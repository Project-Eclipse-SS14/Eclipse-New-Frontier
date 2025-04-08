use std::{collections::HashMap, path::PathBuf};

use common::SS14Project;
use hex::ToHex;
use sha2::{Digest, Sha256};
use simple_logger::SimpleLogger;
use yaml_rust2::Yaml;

struct TranslatableAttributes {
    pub name: Option<String>,
    pub description: Option<String>,
    pub suffix: Option<String>,
}

const SAVE_LOCALE_PATH: &str = "../../Resources/Locale/ru-RU/ss14-ru-better/";

fn main() {
    SimpleLogger::new()
        .with_level(log::LevelFilter::Info)
        .init()
        .unwrap();

    let project = SS14Project::new("../../".into());

    let save_locale_path = &PathBuf::from(SAVE_LOCALE_PATH);

    //Id lookup map to check which parent should an entity inherit name and description from
    let mut id_map = HashMap::new();

    for v in project.get_prototypes() {
        let (path, document) = match v {
            Ok(v) => {
                log::info!("Reading prototypes from {}...", v.0.display());
                v
            }
            Err(e) => {
                log::error!("Failed to read prototype: {:?}", e);
                continue;
            }
        };

        let Yaml::Array(seq) = document else {
            log::warn!(
                "Non sequence root node in a yaml file. Path: {}",
                path.display()
            );
            continue;
        };

        for obj in seq {
            let Yaml::Hash(obj) = obj else {
                log::warn!(
                    "Non object child node in a yaml file. Path: {}",
                    path.display()
                );
                continue;
            };

            let obj_type = match obj.get(&Yaml::String("type".to_owned())).unwrap() {
                Yaml::String(v) => v,
                k => {
                    log::info!("Unsupported type key: {k:?}, skipping");
                    continue;
                }
            };

            if obj_type != "entity" {
                log::debug!("Skipping unsupported object with type: {obj_type}");
                continue;
            }

            let id = match obj.get(&Yaml::String("id".to_owned())).unwrap() {
                Yaml::String(v) => v,
                k => {
                    log::info!("Unsupported id key: {k:?}, skipping");
                    continue;
                }
            };

            id_map.insert(
                id.clone(),
                (
                    path.clone(),
                    TranslatableAttributes {
                        name: obj
                            .get(&Yaml::String("name".to_owned()))
                            .map(|v| v.as_str().unwrap().to_owned()),
                        description: obj
                            .get(&Yaml::String("description".to_owned()))
                            .filter(|v| !v.is_null())
                            .map(|v| v.as_str().unwrap().to_owned())
                            .filter(|v| !v.is_empty()),
                        suffix: obj
                            .get(&Yaml::String("suffix".to_owned()))
                            .filter(|v| !v.is_null())
                            .map(|v| match v {
                                Yaml::String(v) => v.clone(),
                                Yaml::Integer(v) => v.to_string(),
                                _ => panic!(
                                    "Encountered unsupported suffix type: {v:?}. Object id: {id}"
                                ),
                            })
                            .filter(|v| !v.is_empty()),
                    },
                ),
            );
        }
    }

    let mut file_map: HashMap<PathBuf, fluent_syntax::ast::Resource<String>> = HashMap::new();

    for (id, (path, attributes)) in &id_map {
        let locale_path = save_locale_path
            .join(pathdiff::diff_paths(path, project.get_resources_path()).unwrap())
            .with_extension("ftl");

        if locale_path.exists() {
            //TODO: check for updates using hash
            continue;
        }

        let mut can_be_skipped = true;

        let mut hasher = Sha256::new();

        let fl_attributes = {
            let mut vec = vec![];

            if let Some(v) = &attributes.description {
                vec.push(fluent_syntax::ast::Attribute {
                    id: fluent_syntax::ast::Identifier {
                        name: "desc".to_owned(),
                    },
                    value: fluent_syntax::ast::Pattern {
                        elements: vec![fluent_syntax::ast::PatternElement::TextElement {
                            value: v.replace("\n", "\n        "),
                        }],
                    },
                });
                hasher.update(v);
                can_be_skipped = false;
            }

            if let Some(v) = &attributes.suffix {
                vec.push(fluent_syntax::ast::Attribute {
                    id: fluent_syntax::ast::Identifier {
                        name: "suffix".to_owned(),
                    },
                    value: fluent_syntax::ast::Pattern {
                        elements: vec![fluent_syntax::ast::PatternElement::TextElement {
                            value: v.clone(),
                        }],
                    },
                });
                hasher.update(v);
                can_be_skipped = false;
            }

            vec
        };

        if let Some(v) = &attributes.name {
            hasher.update(v);
            can_be_skipped = false;
        }

        if can_be_skipped {
            continue;
        }

        let hash: String = hasher.finalize().encode_hex();

        let resource = file_map
            .entry(locale_path)
            .or_insert(fluent_syntax::ast::Resource { body: vec![] });
        resource.body.push(fluent_syntax::ast::Entry::Message(
            fluent_syntax::ast::Message {
                id: fluent_syntax::ast::Identifier {
                    name: format!("ent-{id}"),
                },
                value: attributes
                    .name
                    .clone()
                    .map(|v| fluent_syntax::ast::Pattern {
                        elements: vec![fluent_syntax::ast::PatternElement::TextElement {
                            value: v,
                        }],
                    }),
                attributes: fl_attributes,
                comment: Some(fluent_syntax::ast::Comment {
                    content: vec![format!("HASH: {}", hash)],
                }),
            },
        ));
    }

    for (path, resource) in file_map {
        let serialized_resource = fluent_syntax::serializer::serialize(&resource);

        std::fs::create_dir_all(path.parent().unwrap()).unwrap();
        std::fs::write(path, serialized_resource).unwrap();
        //println!("PATH: {}\n{:#?}\n\n", path.display(), serialized_resource);
    }

    //println!("{:#?}", file_map);
}
