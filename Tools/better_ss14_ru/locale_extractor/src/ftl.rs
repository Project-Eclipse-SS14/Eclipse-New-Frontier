use std::{
    collections::HashMap,
    io::{Read, Seek, SeekFrom},
    path::{Path, PathBuf},
};

use common::change_guard::ChangeGuard;
use sha2::{Digest, Sha256};
use unicode_bom::Bom;
use walkdir::WalkDir;

struct AbstractFtlStorage {
    root_path: PathBuf,
    pub files: HashMap<PathBuf, FtlFile>,
}

impl AbstractFtlStorage {
    pub fn load(root_path: PathBuf, folder_filter: &[&str]) -> Result<Self, ReadFtlStorageError> {
        let iter = WalkDir::new(&root_path)
            .into_iter()
            .filter_entry(|p| {
                !p.file_type().is_dir()
                    || !folder_filter.contains(&&*p.file_name().to_string_lossy())
            })
            .filter_map(|p| match p {
                Ok(p)
                    if p.file_type().is_file()
                        && p.path().extension().is_some_and(|v| v == "ftl") =>
                {
                    Some(p)
                }
                Ok(_) => None,
                Err(e) => {
                    log::error!("Failed to read ftl file: {}", e);
                    None
                }
            });

        let mut files = HashMap::new();

        for entry in iter {
            let path = entry.into_path();

            let file = FtlFile::load(path.clone())
                .map_err(|e| ReadFtlStorageError::Parse(path.to_owned(), e))?;

            let local_path = pathdiff::diff_paths(&path, &root_path).expect(&format!(
                "Failed to find a relative path for {} with base of {}",
                path.display(),
                root_path.display()
            ));

            files.insert(local_path, file);
        }

        Ok(Self { root_path, files })
    }

    pub fn iter_messages(&self) -> impl Iterator<Item = (&PathBuf, &MessageWrapper)> {
        self.files
            .iter()
            .flat_map(|v| std::iter::repeat(v.0).zip(&v.1.messages))
    }

    pub fn iter_messages_mut(&mut self) -> impl Iterator<Item = &mut MessageWrapper> {
        self.files.iter_mut().flat_map(|v| &mut v.1.messages)
    }
}

pub struct FtlFile {
    messages: Vec<MessageWrapper>,
    entries_were_removed: bool,
}

impl FtlFile {
    fn load(path: PathBuf) -> Result<Self, ParseFtlFileError> {
        let mut file = std::fs::File::open(&path).map_err(|e| ParseFtlFileError::Io(e))?;

        let bom = Bom::from(&mut file);

        file.seek(SeekFrom::Start(bom.len() as u64))
            .map_err(|e| ParseFtlFileError::Io(e))?;

        let mut s = String::new();
        file.read_to_string(&mut s)
            .map_err(|e| ParseFtlFileError::Io(e))?;

        let body =
            fluent_syntax::parser::parse(s).map_err(|(_, e)| ParseFtlFileError::FtlParse(e))?;

        let mut messages = Vec::new();

        for entry in body.body {
            match entry {
                fluent_syntax::ast::Entry::Message(message) => {
                    messages.push(MessageWrapper {
                        inner: message,
                        changed: false,
                    });
                }
                _ => {
                    log::error!(
                        "Encountered unused fluent ast entry while parsing file body. Path: {}, Message: {:?}",
                        path.display(),
                        entry
                    );
                    continue;
                }
            }
        }

        Ok(Self {
            messages,
            entries_were_removed: false,
        })
    }

    pub fn is_changed(&self) -> bool {
        self.entries_were_removed || self.messages.iter().any(|v| v.changed)
    }

    pub fn contains_id(&self, id: &fluent_syntax::ast::Identifier<String>) -> bool {
        self.messages.iter().any(|v| v.id() == id)
    }

    pub fn add_message(&mut self, message: fluent_syntax::ast::Message<String>) {
        self.messages.push(MessageWrapper {
            inner: message,
            changed: true,
        });
    }

    pub fn remove_entry(&mut self, id: &fluent_syntax::ast::Identifier<String>) {
        self.messages.retain(|v| v.id() != id);
        self.entries_were_removed = true;
    }

    fn save(&self, path: &Path) {
        let resource = fluent_syntax::ast::Resource {
            body: self
                .messages
                .iter()
                .map(|v| fluent_syntax::ast::Entry::Message(v.inner.clone()))
                .collect(),
        };
        let serialized_resource = fluent_syntax::serializer::serialize(&resource);
        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, serialized_resource).unwrap();
    }
}

#[derive(Debug)]
pub enum ParseFtlFileError {
    Io(std::io::Error),
    FtlParse(Vec<fluent_syntax::parser::ParserError>),
}

#[derive(PartialEq)]
pub struct MessageWrapper {
    inner: fluent_syntax::ast::Message<String>,

    changed: bool,
}

impl MessageWrapper {
    pub fn id(&self) -> &fluent_syntax::ast::Identifier<String> {
        &self.inner.id
    }

    pub fn get_hash(&self) -> Result<Option<[u8; 32]>, GetHashError> {
        let Some(hash_comment) = &self.inner.comment else {
            return Ok(None);
        };
        if hash_comment.content.is_empty() {
            return Ok(None);
        }
        let hash_string = &hash_comment.content[0];
        let Some(hash_string) = hash_string.strip_prefix("HASH: ") else {
            return Err(GetHashError::HashCommentNoPrefix);
        };
        let mut out = [0; 32];
        hex::decode_to_slice(hash_string, &mut out)
            .map_err(|e| GetHashError::HashCommentHexDecodeError(e))?;
        Ok(Some(out))
    }

    pub fn calculate_hash(&self) -> [u8; 32] {
        let mut hasher = Sha256::new();
        if let Some(v) = &self.inner.value {
            hasher.update(serde_json::to_string(&v).unwrap());
        }
        for attribute in &self.inner.attributes {
            hasher.update(serde_json::to_string(&attribute).unwrap());
        }

        hasher.finalize().into()
    }

    pub fn inner(&self) -> &fluent_syntax::ast::Message<String> {
        &self.inner
    }

    pub fn inner_mut(&mut self) -> ChangeGuard<fluent_syntax::ast::Message<String>> {
        ChangeGuard::new(&mut self.inner, &mut self.changed)
    }
}

#[derive(Debug)]
pub enum GetHashError {
    HashCommentNoPrefix,
    HashCommentHexDecodeError(hex::FromHexError),
}

pub struct SourceFtlStorage {
    inner: AbstractFtlStorage,
}

impl SourceFtlStorage {
    pub fn load(root_path: PathBuf, folder_filter: &[&str]) -> Result<Self, ReadFtlStorageError> {
        Ok(Self {
            inner: AbstractFtlStorage::load(root_path, folder_filter)?,
        })
    }

    pub fn find(&self, id: &fluent_syntax::ast::Identifier<String>) -> Option<&MessageWrapper> {
        self.inner
            .iter_messages()
            .map(|v| v.1)
            .find(|v| v.id() == id)
    }

    pub fn iter_messages(&self) -> impl Iterator<Item = (&PathBuf, &MessageWrapper)> {
        self.inner.iter_messages()
    }

    pub fn get_file(&self, path: &Path) -> Option<&FtlFile> {
        self.inner.files.get(path)
    }
}

pub struct TargetFtlStorage {
    inner: AbstractFtlStorage,
}

impl TargetFtlStorage {
    pub fn load(root_path: PathBuf, folder_filter: &[&str]) -> Result<Self, ReadFtlStorageError> {
        Ok(Self {
            inner: AbstractFtlStorage::load(root_path, folder_filter)?,
        })
    }

    pub fn iter_messages(&self) -> impl Iterator<Item = (&PathBuf, &MessageWrapper)> {
        self.inner.iter_messages()
    }

    pub fn iter_messages_mut(&mut self) -> impl Iterator<Item = &mut MessageWrapper> {
        self.inner.iter_messages_mut()
    }

    pub fn save(&self) {
        for (path, file) in &self.inner.files {
            if !file.is_changed() {
                continue;
            }
            let path = self.inner.root_path.join(&path);

            log::info!("File {} has changed, saving...", path.display());

            file.save(&path);
        }
    }

    pub fn get_file(&self, path: &Path) -> Option<&FtlFile> {
        self.inner.files.get(path)
    }

    pub fn get_file_mut(&mut self, path: &Path) -> Option<&mut FtlFile> {
        self.inner.files.get_mut(path)
    }

    pub fn add_file(&mut self, path: PathBuf) {
        self.inner.files.insert(
            path,
            FtlFile {
                messages: vec![],
                entries_were_removed: false,
            },
        );
    }

    pub fn remove_file(&mut self, path: &Path) {
        self.inner.files.remove(path);
        let full_path = self.inner.root_path.join(&path);
        std::fs::remove_file(full_path).unwrap();
        log::info!("File {} has been removed", path.display());
    }
}

#[derive(Debug)]
pub enum ReadFtlStorageError {
    Io(PathBuf, std::io::Error),
    Parse(PathBuf, ParseFtlFileError),
}
