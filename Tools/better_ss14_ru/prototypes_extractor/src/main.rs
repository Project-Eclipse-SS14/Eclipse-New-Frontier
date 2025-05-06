use std::{collections::HashMap, path::PathBuf};

use common::{
    SS14Project,
    prototypes_ftl::{FtlStorage, StringOrOtherId},
};
use sha2::{Digest, Sha256};
use simple_logger::SimpleLogger;
use yaml_rust2::Yaml;

struct TranslatableAttributes {
    pub name: Option<StringOrOtherId>,
    pub description: Option<StringOrOtherId>,
    pub suffix: Option<StringOrOtherId>,
}

impl TranslatableAttributes {
    pub fn needs_resolving(&self) -> bool {
        self.name.is_none() || self.description.is_none()
    }

    pub fn try_resolve_from_parent(&mut self, parent_id: &str, parent_attrs: &Self) {
        self.name = self.name.take().or_else(|| {
            if parent_attrs.name.is_some() {
                Some(StringOrOtherId::Id(parent_id.to_owned()))
            } else {
                None
            }
        });
        self.description = self.description.take().or_else(|| {
            if parent_attrs.description.is_some() {
                Some(StringOrOtherId::Id(parent_id.to_owned()))
            } else {
                None
            }
        });
    }

    pub fn resolve_using_parent_id(&mut self, parent_id: &str) {
        self.name = self
            .name
            .take()
            .or_else(|| Some(StringOrOtherId::Id(parent_id.to_owned())));
        self.description = self
            .description
            .take()
            .or_else(|| Some(StringOrOtherId::Id(parent_id.to_owned())));
    }
}

const SAVE_LOCALE_PATH: &str = "../../Resources/Locale/ru-RU/ss14-ru-better/";

fn main() {
    SimpleLogger::new()
        .with_level(log::LevelFilter::Info)
        .init()
        .unwrap();

    let project = SS14Project::new("../../".into());

    let save_locale_path = &PathBuf::from(SAVE_LOCALE_PATH);

    let mut ftl_project = FtlStorage::load(save_locale_path.clone()).unwrap();

    //Id lookup maps to check which parent should an entity inherit name and description from
    let mut parents_map = HashMap::new();
    let mut id_map = HashMap::new();

    let mut prototypes = Vec::new();

    for v in project.get_prototypes() {
        let (path, document) = match v {
            Ok(v) => {
                //log::info!("Reading prototypes from {}...", v.0.display());
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

            let name = obj
                .get(&Yaml::String("name".to_owned()))
                .map(|v| StringOrOtherId::String(v.as_str().unwrap().to_owned()));

            let description = obj
                .get(&Yaml::String("description".to_owned()))
                .filter(|v| !v.is_null())
                .map(|v| v.as_str().unwrap().to_owned())
                .filter(|v| !v.is_empty())
                .map(|v| StringOrOtherId::String(v));

            let suffix = obj
                .get(&Yaml::String("suffix".to_owned()))
                .filter(|v| !v.is_null())
                .map(|v| match v {
                    Yaml::String(v) => v.clone(),
                    Yaml::Integer(v) => v.to_string(),
                    _ => panic!("Encountered unsupported suffix type: {v:?}. Object id: {id}"),
                })
                .filter(|v| !v.is_empty())
                .map(|v| StringOrOtherId::String(v));

            let parents = obj
                .get(&Yaml::String("parent".to_owned()))
                .map(|v| match v {
                    Yaml::String(v) => vec![v.clone()],
                    Yaml::Array(v) => v.iter().map(|v| v.as_str().unwrap().to_string()).collect(),
                    _ => panic!("Encountered unsupported parent type: {v:?}. Object id: {id}"),
                });

            if let Some(parents) = parents {
                parents_map.insert(id.clone(), parents);
            }

            id_map.insert(
                id.clone(),
                TranslatableAttributes {
                    name: name.clone(),
                    description: description.clone(),
                    suffix: suffix.clone(),
                },
            );

            prototypes.push((
                id.clone(),
                path.clone(),
                TranslatableAttributes {
                    name,
                    description,
                    suffix,
                },
            ));
        }
    }

    for (id, path, mut attributes) in prototypes {
        let locale_path = save_locale_path
            .join(pathdiff::diff_paths(path, project.get_resources_path()).unwrap())
            .with_extension("ftl");

        if attributes.needs_resolving() {
            if let Some(v) = parents_map.get(&id) {
                if v.len() == 1 {
                    attributes.resolve_using_parent_id(&v[0]);
                } else {
                    for parent in v {
                        resolve_using_parents(&id_map, &parents_map, parent, &mut attributes);
                    }
                }
            }
        }

        // If parent search fails, fallback
        attributes.name = attributes
            .name
            .take()
            .or_else(|| Some(StringOrOtherId::Empty));
        attributes.description = attributes
            .description
            .take()
            .or_else(|| Some(StringOrOtherId::Empty));

        let hash: [u8; 32] = {
            let mut hasher = Sha256::new();

            if let Some(v) = &attributes.name {
                match v {
                    StringOrOtherId::Empty => (),
                    StringOrOtherId::String(v) => hasher.update(v),
                    StringOrOtherId::Id(v) => hasher.update(v),
                };
            }
            if let Some(v) = &attributes.description {
                match v {
                    StringOrOtherId::Empty => (),
                    StringOrOtherId::String(v) => hasher.update(v),
                    StringOrOtherId::Id(v) => hasher.update(v),
                };
            }
            if let Some(v) = &attributes.suffix {
                match v {
                    StringOrOtherId::Empty => (),
                    StringOrOtherId::String(v) => hasher.update(v),
                    StringOrOtherId::Id(v) => hasher.update(v),
                };
            }

            hasher.finalize().into()
        };

        let file = ftl_project.get_or_create_mut(locale_path);

        match file.get_prototype(&id) {
            Some(prototype) => {
                let mut prototype = prototype.borrow_mut();
                if prototype.hash != hash {
                    log::info!("Prototype {id} has a hash mismatch.");
                    *prototype.name_mut() = attributes.name.clone();
                    *prototype.description_mut() = attributes.description.clone();
                    *prototype.suffix_mut() = attributes.suffix.clone();
                    prototype.hash = hash;
                }
            }
            None => {
                let prototype = file.create_prototype(id.clone()).unwrap();
                let mut prototype = prototype.borrow_mut();
                *prototype.name_mut() = attributes.name.clone();
                *prototype.description_mut() = attributes.description.clone();
                *prototype.suffix_mut() = attributes.suffix.clone();
                prototype.hash = hash;
                log::info!("Prototype {id} did not have an ftl entry, created.");
            }
        }
    }

    ftl_project.save();
}

fn resolve_using_parents(
    id_map: &HashMap<String, TranslatableAttributes>,
    parents_map: &HashMap<String, Vec<String>>,
    parent_id: &str,
    attributes: &mut TranslatableAttributes,
) {
    if !attributes.needs_resolving() {
        return;
    }
    let Some(parent_attrs) = id_map.get(parent_id) else {
        panic!("A parent id was set to an object that was not found! This is a yaml mistake");
    };
    attributes.try_resolve_from_parent(parent_id, parent_attrs);
    if !attributes.needs_resolving() {
        return;
    }
    if let Some(v) = parents_map.get(parent_id) {
        for parent in v {
            resolve_using_parents(id_map, parents_map, parent, attributes);
        }
    }
}
