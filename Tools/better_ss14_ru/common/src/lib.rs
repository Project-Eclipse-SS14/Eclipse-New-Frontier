pub mod change_guard;
pub mod prototypes_ftl;

use std::{
    io::{Read, Seek, SeekFrom},
    path::{Path, PathBuf},
};

use unicode_bom::Bom;
use walkdir::WalkDir;
use yaml_rust2::YamlLoader;

pub struct SS14Project(PathBuf);

impl SS14Project {
    pub fn new(path: PathBuf) -> Self {
        //TODO: validate that path is correct
        Self(path)
    }

    pub fn get_project_path(&self) -> &Path {
        &self.0
    }

    pub fn get_locale_path(&self, locale: &str) -> PathBuf {
        self.0.join(format!("Resources/Locale/{locale}"))
    }

    pub fn get_resources_path(&self) -> PathBuf {
        self.0.join("Resources")
    }

    fn get_prototypes_path(&self) -> PathBuf {
        self.0.join("Resources/Prototypes")
    }

    fn get_prototype_file_paths(&self) -> impl Iterator<Item = walkdir::DirEntry> {
        WalkDir::new(self.get_prototypes_path())
            .into_iter()
            .filter_map(|p| match p {
                Ok(p)
                    if p.file_type().is_file()
                        && p.path()
                            .extension()
                            .is_some_and(|v| v == "yaml" || v == "yml") =>
                {
                    Some(p)
                }
                Ok(_) => None,
                Err(e) => {
                    log::error!("Failed to read prototype file: {}", e);
                    None
                }
            })
    }

    pub fn get_prototypes(
        &self,
    ) -> impl Iterator<Item = Result<(PathBuf, yaml_rust2::Yaml), (PathBuf, ReadPrototypeError)>>
    {
        self.get_prototype_file_paths().map(|p| {
            let mut file = std::fs::File::open(p.path()).map_err(|e| (p.path().to_owned(), ReadPrototypeError::Io(e)))?;

            let bom = Bom::from(&mut file);

            file.seek(SeekFrom::Start(bom.len() as u64)).map_err(|e| (p.path().to_owned(), ReadPrototypeError::Io(e)))?;

            let mut s = String::new();
            file.read_to_string(&mut s).map_err(|e| (p.path().to_owned(), ReadPrototypeError::Io(e)))?;

            Ok((
                p.path().to_owned(),
                YamlLoader::load_from_str(
                    &s,
                ).map_err(|e| (p.path().to_owned(), ReadPrototypeError::Scan(e)))?
            ))
        }).filter_map(|v| {
            match v {
                Ok(mut v) => {
                    match v.1.len() {
                        0 => None,
                        1 => {
                            Some(Ok((v.0, v.1.swap_remove(0))))
                        }
                        _ => {
                            panic!("Found more than 1 document in a single YAML file (Not handled yet). Path: {}", v.0.display());
                        }
                    }
                }
                Err(e) => Some(Err(e))
            }
        })
    }
}

#[derive(Debug)]
pub enum ReadPrototypeError {
    Io(std::io::Error),
    Scan(yaml_rust2::ScanError),
}
