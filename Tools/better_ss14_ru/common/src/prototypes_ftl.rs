use std::{
    collections::HashMap,
    io::Read,
    path::{Path, PathBuf},
};

use hex::ToHex;
use walkdir::WalkDir;

use crate::change_guard::ChangeGuard;

pub struct FtlStorage {
    root_path: PathBuf,
    pub files: HashMap<PathBuf, FtlPrototypesFile>,
}

impl FtlStorage {
    pub fn load(root_path: PathBuf) -> Result<Self, ReadFtlStorageError> {
        let iter = WalkDir::new(&root_path)
            .into_iter()
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

            let file = FtlPrototypesFile::load(path.clone())
                .map_err(|e| ReadFtlStorageError::Parse(path.to_owned(), e))?;

            let local_path = pathdiff::diff_paths(&path, &root_path).expect(&format!(
                "Failed to find a relative path for {} with base of {}",
                path.display(),
                root_path.display()
            ));
            //dbg!(local_path.display());

            files.insert(local_path, file);
        }

        Ok(Self { root_path, files })
    }

    pub fn save(&self) {
        for (path, file) in &self.files {
            if !file.is_changed() {
                continue;
            }
            let path = self.root_path.join(&path);

            log::info!("File {} has changed, saving...", path.display());

            file.save(&path);
        }
    }

    pub fn get_or_create(&mut self, path: PathBuf) -> &FtlPrototypesFile {
        let local_path = pathdiff::diff_paths(&path, &self.root_path).expect(&format!(
            "Failed to find a relative path for {} with base of {}",
            path.display(),
            self.root_path.display()
        ));
        self.files.entry(local_path).or_insert(FtlPrototypesFile {
            prototypes: Vec::new(),
        })
    }

    pub fn get_or_create_mut(&mut self, path: PathBuf) -> &mut FtlPrototypesFile {
        let local_path = pathdiff::diff_paths(&path, &self.root_path).expect(&format!(
            "Failed to find a relative path for {} with base of {}",
            path.display(),
            self.root_path.display()
        ));
        //dbg!(local_path.display());
        self.files.entry(local_path).or_insert(FtlPrototypesFile {
            prototypes: Vec::new(),
        })
    }
}

pub struct FtlPrototypesFile {
    pub prototypes: Vec<FtlPrototype>,
}

impl FtlPrototypesFile {
    fn load(path: PathBuf) -> Result<Self, ParseFtlPrototypesFileError> {
        let mut file =
            std::fs::File::open(&path).map_err(|e| ParseFtlPrototypesFileError::Io(e))?;

        let mut s = String::new();
        file.read_to_string(&mut s)
            .map_err(|e| ParseFtlPrototypesFileError::Io(e))?;

        let body = fluent_syntax::parser::parse(s)
            .map_err(|(_, e)| ParseFtlPrototypesFileError::FtlParse(e))?;

        let mut prototypes = Vec::new();

        for entry in body.body {
            match entry {
                fluent_syntax::ast::Entry::Message(message) => {
                    prototypes.push(FtlPrototype::parse(message)?)
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

        Ok(Self { prototypes })
    }

    pub fn is_changed(&self) -> bool {
        self.prototypes.iter().any(|v| v.changed)
    }

    fn save(&self, path: &Path) {
        let mut resource = fluent_syntax::ast::Resource { body: Vec::new() };

        for prototype in &self.prototypes {
            let fl_attributes = {
                let mut vec = vec![];

                if let Some(v) = &prototype.description {
                    vec.push(fluent_syntax::ast::Attribute {
                        id: fluent_syntax::ast::Identifier {
                            name: "desc".to_owned(),
                        },
                        value: fluent_syntax::ast::Pattern {
                            elements: match v {
                                StringOrOtherId::Empty => {
                                    vec![fluent_syntax::ast::PatternElement::Placeable { expression: fluent_syntax::ast::Expression::Inline(fluent_syntax::ast::InlineExpression::StringLiteral { value: "".to_owned() }) }]
                                }
                                StringOrOtherId::String(v) => {
                                    vec![fluent_syntax::ast::PatternElement::TextElement {
                                        value: v.replace("\n", "\n        "),
                                    }]
                                }
                                StringOrOtherId::Id(v) => {
                                    vec![fluent_syntax::ast::PatternElement::Placeable { expression: fluent_syntax::ast::Expression::Inline(fluent_syntax::ast::InlineExpression::MessageReference { id: fluent_syntax::ast::Identifier { name: format!("ent-{v}") }, attribute: Some(fluent_syntax::ast::Identifier { name: "desc".to_owned() }) }) }]
                                },
                            },
                        },
                    });
                }

                if let Some(v) = &prototype.suffix {
                    vec.push(fluent_syntax::ast::Attribute {
                        id: fluent_syntax::ast::Identifier {
                            name: "suffix".to_owned(),
                        },
                        value: fluent_syntax::ast::Pattern {
                            elements: match v {
                                StringOrOtherId::Empty => {
                                    vec![fluent_syntax::ast::PatternElement::Placeable { expression: fluent_syntax::ast::Expression::Inline(fluent_syntax::ast::InlineExpression::StringLiteral { value: "".to_owned() }) }]
                                }
                                StringOrOtherId::String(v) => {
                                    vec![fluent_syntax::ast::PatternElement::TextElement {
                                        value: v.clone(),
                                    }]
                                }
                                StringOrOtherId::Id(v) => {
                                    vec![fluent_syntax::ast::PatternElement::Placeable { expression: fluent_syntax::ast::Expression::Inline(fluent_syntax::ast::InlineExpression::MessageReference { id: fluent_syntax::ast::Identifier { name: format!("ent-{v}") }, attribute: Some(fluent_syntax::ast::Identifier { name: "suffix".to_owned() }) }) }]
                                },
                            },
                        },
                    });
                }

                vec
            };

            resource.body.push(fluent_syntax::ast::Entry::Message(
                fluent_syntax::ast::Message {
                    id: fluent_syntax::ast::Identifier {
                        name: format!("ent-{}", prototype.id),
                    },
                    value: prototype.name.clone().map(|v| fluent_syntax::ast::Pattern {
                        elements: match v {
                            StringOrOtherId::Empty => {
                                vec![fluent_syntax::ast::PatternElement::Placeable {
                                    expression: fluent_syntax::ast::Expression::Inline(
                                        fluent_syntax::ast::InlineExpression::StringLiteral {
                                            value: "".to_owned(),
                                        },
                                    ),
                                }]
                            }
                            StringOrOtherId::String(v) => {
                                vec![fluent_syntax::ast::PatternElement::TextElement { value: v }]
                            }
                            StringOrOtherId::Id(v) => {
                                vec![fluent_syntax::ast::PatternElement::Placeable {
                                    expression: fluent_syntax::ast::Expression::Inline(
                                        fluent_syntax::ast::InlineExpression::MessageReference {
                                            id: fluent_syntax::ast::Identifier {
                                                name: format!("ent-{v}"),
                                            },
                                            attribute: None,
                                        },
                                    ),
                                }]
                            }
                        },
                    }),
                    attributes: fl_attributes,
                    comment: Some(fluent_syntax::ast::Comment {
                        content: vec![format!("HASH: {}", prototype.hash.encode_hex::<String>())],
                    }),
                },
            ));
        }

        let serialized_resource = fluent_syntax::serializer::serialize(&resource);

        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, serialized_resource).unwrap();
    }

    pub fn get_prototype_ref(&self, id: &str) -> Option<&FtlPrototype> {
        for prototype in &self.prototypes {
            if prototype.id == id {
                return Some(prototype);
            }
        }
        None
    }

    pub fn get_prototype_mut(&mut self, id: &str) -> Option<&mut FtlPrototype> {
        for prototype in &mut self.prototypes {
            //dbg!(&prototype.id);
            if prototype.id == id {
                return Some(prototype);
            }
        }
        None
    }

    pub fn create_prototype(&mut self, id: String) -> Result<&mut FtlPrototype, PrototypeIdExists> {
        for prototype in &mut self.prototypes {
            if prototype.id == id {
                return Err(PrototypeIdExists);
            }
        }

        self.prototypes.push(FtlPrototype {
            hash: [0; 32],
            id,
            name: None,
            description: None,
            suffix: None,
            changed: true,
        });
        //SAFETY: it was literally just added above
        Ok(unsafe { self.prototypes.last_mut().unwrap_unchecked() })
    }
}

#[derive(Debug)]
pub struct PrototypeIdExists;

pub struct FtlPrototype {
    pub hash: [u8; 32],

    pub id: String,

    name: Option<StringOrOtherId>,
    description: Option<StringOrOtherId>,
    suffix: Option<StringOrOtherId>,

    changed: bool,
}

impl FtlPrototype {
    fn parse(
        message: fluent_syntax::ast::Message<String>,
    ) -> Result<Self, ParseFtlPrototypesFileError> {
        let hash = {
            let Some(hash_comment) = message.comment else {
                return Err(ParseFtlPrototypesFileError::NoHashComment);
            };
            if hash_comment.content.is_empty() {
                return Err(ParseFtlPrototypesFileError::HashCommentIsEmpty);
            }
            if hash_comment.content.len() != 1 {
                return Err(ParseFtlPrototypesFileError::HashCommentContainsVariables);
            }
            let hash_string = &hash_comment.content[0];
            let Some(hash_string) = hash_string.strip_prefix("HASH: ") else {
                return Err(ParseFtlPrototypesFileError::HashCommentNoPrefix);
            };
            let mut out = [0; 32];
            hex::decode_to_slice(hash_string, &mut out)
                .map_err(|e| ParseFtlPrototypesFileError::HashCommentHexDecodeError(e))?;
            out
        };

        let name = match message
            .value
            .as_ref()
            .map(|v| StringOrOtherId::parse_pattern("name", &v))
        {
            Some(Ok(v)) => Some(v),
            Some(Err(e)) => return Err(e),
            None => None,
        };

        let mut description = None;
        let mut suffix = None;

        for attribute in &message.attributes {
            let id = &attribute.id.name;
            let target = match id.as_str() {
                "desc" => &mut description,
                "suffix" => &mut suffix,
                v => {
                    return Err(ParseFtlPrototypesFileError::UnknownAttribute(v.to_string()));
                }
            };
            if target.is_some() {
                return Err(ParseFtlPrototypesFileError::AttributeDefinedTwice(
                    id.clone(),
                ));
            }
            *target = Some(StringOrOtherId::parse_pattern(&id, &attribute.value)?);
        }

        Ok(Self {
            hash,

            id: message
                .id
                .name
                .strip_prefix("ent-")
                .ok_or(ParseFtlPrototypesFileError::IdDidNotStartWithEnt(
                    message.id.name.clone(),
                ))?
                .to_owned(),

            name,
            description,
            suffix,

            changed: false,
        })
    }

    pub fn name(&self) -> Option<&StringOrOtherId> {
        self.name.as_ref()
    }
    pub fn name_mut(&mut self) -> ChangeGuard<Option<StringOrOtherId>> {
        ChangeGuard::new(&mut self.name, &mut self.changed)
    }

    pub fn description(&self) -> Option<&StringOrOtherId> {
        self.description.as_ref()
    }
    pub fn description_mut(&mut self) -> ChangeGuard<Option<StringOrOtherId>> {
        ChangeGuard::new(&mut self.description, &mut self.changed)
    }

    pub fn suffix(&self) -> Option<&StringOrOtherId> {
        self.suffix.as_ref()
    }
    pub fn suffix_mut(&mut self) -> ChangeGuard<Option<StringOrOtherId>> {
        ChangeGuard::new(&mut self.suffix, &mut self.changed)
    }
}

#[derive(Debug)]
pub enum ReadFtlStorageError {
    Io(PathBuf, std::io::Error),
    Parse(PathBuf, ParseFtlPrototypesFileError),
}

#[derive(Debug)]
pub enum ParseFtlPrototypesFileError {
    Io(std::io::Error),
    FtlParse(Vec<fluent_syntax::parser::ParserError>),

    NoHashComment,
    HashCommentIsEmpty,
    HashCommentContainsVariables,
    HashCommentNoPrefix,
    HashCommentHexDecodeError(hex::FromHexError),

    IdDidNotStartWithEnt(String),

    AttributeIsEmpty(String),
    AttributeContainsVariables(String),
    UnknownAttribute(String),
    AttributeDefinedTwice(String),
    AttributeVariablesMixedWithText(String),
    AttributeInvalidVariable(String),
}

#[derive(Clone, PartialEq, Debug)]
pub enum StringOrOtherId {
    Empty,
    String(String),
    Id(String),
}

impl StringOrOtherId {
    pub fn parse_pattern(
        name: &str,
        pat: &fluent_syntax::ast::Pattern<String>,
    ) -> Result<Self, ParseFtlPrototypesFileError> {
        if pat.elements.is_empty() {
            return Err(ParseFtlPrototypesFileError::AttributeIsEmpty(
                name.to_owned(),
            ));
        }

        let mut other_id = None;
        let mut vec = Vec::new();
        for v in &pat.elements {
            match v {
                fluent_syntax::ast::PatternElement::TextElement { value } => {
                    if other_id.is_some() {
                        return Err(
                            ParseFtlPrototypesFileError::AttributeVariablesMixedWithText(
                                name.to_owned(),
                            ),
                        );
                    }
                    vec.push(value.clone());
                }
                fluent_syntax::ast::PatternElement::Placeable { expression } => {
                    if !vec.is_empty() {
                        return Err(
                            ParseFtlPrototypesFileError::AttributeVariablesMixedWithText(
                                name.to_owned(),
                            ),
                        );
                    }
                    let fluent_syntax::ast::Expression::Inline(v) = expression else {
                        return Err(ParseFtlPrototypesFileError::AttributeInvalidVariable(
                            name.to_owned(),
                        ));
                    };
                    let fluent_syntax::ast::InlineExpression::MessageReference { id, .. } = v
                    else {
                        return Err(ParseFtlPrototypesFileError::AttributeInvalidVariable(
                            name.to_owned(),
                        ));
                    };
                    other_id = Some(id.name.strip_prefix("ent-").ok_or_else(|| {
                        ParseFtlPrototypesFileError::AttributeInvalidVariable(name.to_owned())
                    })?);
                }
            }
        }

        match other_id {
            Some(v) => Ok(StringOrOtherId::Id(v.to_owned())),
            None => Ok(StringOrOtherId::String(vec.join("\n"))),
        }
    }

    pub fn parse_pattern_str(
        name: &str,
        pat: &fluent_syntax::ast::Pattern<&str>,
    ) -> Result<Self, ParseFtlPrototypesFileError> {
        if pat.elements.is_empty() {
            return Err(ParseFtlPrototypesFileError::AttributeIsEmpty(
                name.to_owned(),
            ));
        }

        let mut other_id = None;
        let mut vec = Vec::new();
        for v in &pat.elements {
            match v {
                fluent_syntax::ast::PatternElement::TextElement { value } => {
                    if other_id.is_some() {
                        return Err(
                            ParseFtlPrototypesFileError::AttributeVariablesMixedWithText(
                                name.to_owned(),
                            ),
                        );
                    }
                    vec.push(*value);
                }
                fluent_syntax::ast::PatternElement::Placeable { expression } => {
                    if !vec.is_empty() {
                        return Err(
                            ParseFtlPrototypesFileError::AttributeVariablesMixedWithText(
                                name.to_owned(),
                            ),
                        );
                    }
                    let fluent_syntax::ast::Expression::Inline(v) = expression else {
                        return Err(ParseFtlPrototypesFileError::AttributeInvalidVariable(
                            name.to_owned(),
                        ));
                    };
                    match v {
                        fluent_syntax::ast::InlineExpression::MessageReference { id, .. } => {
                            other_id = Some(id.name.strip_prefix("ent-").ok_or_else(|| {
                                ParseFtlPrototypesFileError::AttributeInvalidVariable(
                                    name.to_owned(),
                                )
                            })?);
                        }
                        // Compatibility with ss14_ru
                        fluent_syntax::ast::InlineExpression::StringLiteral { value } => {
                            if *value == "" {
                                return Ok(StringOrOtherId::Empty);
                            } else {
                                return Err(ParseFtlPrototypesFileError::AttributeInvalidVariable(
                                    name.to_owned(),
                                ));
                            }
                        }
                        _ => {
                            return Err(ParseFtlPrototypesFileError::AttributeInvalidVariable(
                                name.to_owned(),
                            ));
                        }
                    }
                }
            }
        }

        match other_id {
            Some(v) => Ok(StringOrOtherId::Id(v.to_owned())),
            None => Ok(StringOrOtherId::String(vec.join("\n"))),
        }
    }
}
