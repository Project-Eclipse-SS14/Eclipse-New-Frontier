use std::{fs::File, path::PathBuf};

use common::prototypes_ftl::{FtlStorage, StringOrOtherId};
use fluent::{FluentBundle, FluentResource};
use simple_logger::SimpleLogger;
use walkdir::WalkDir;

const SAVE_LOCALE_PATH: &str = "../../Resources/Locale/ru-RU/ss14-ru-better/";
const LOAD_LOCALE_PATH: &str = "../../Resources/Locale/ru-RU/ss14-ru/";

fn main() {
    SimpleLogger::new()
        .with_level(log::LevelFilter::Info)
        .init()
        .unwrap();

    let save_locale_path = &PathBuf::from(SAVE_LOCALE_PATH);
    let load_locale_path = &PathBuf::from(LOAD_LOCALE_PATH);

    let mut bundle = FluentBundle::default();

    let iter = WalkDir::new(&load_locale_path)
        .into_iter()
        .filter_map(|p| match p {
            Ok(p)
                if p.file_type().is_file() && p.path().extension().is_some_and(|v| v == "ftl") =>
            {
                Some(p)
            }
            Ok(_) => None,
            Err(e) => {
                log::error!("Failed to read ftl file: {}", e);
                None
            }
        });
    for entry in iter {
        let path = entry.into_path();

        bundle
            .add_resource(
                FluentResource::try_new(
                    std::io::read_to_string(File::open(path).unwrap()).unwrap(),
                )
                .unwrap(),
            )
            .unwrap();
    }

    let ftl_project = FtlStorage::load(save_locale_path.clone()).unwrap();

    for (_, file) in &ftl_project.files {
        for prototype in &file.prototypes {
            if let Some(message) = bundle.get_message(&format!("ent-{}", prototype.borrow().id)) {
                let name =
                    StringOrOtherId::parse_pattern_str("name", message.value().unwrap()).unwrap();

                let description = message
                    .get_attribute("desc")
                    .map(|v| StringOrOtherId::parse_pattern_str("desc", v.value()).unwrap())
                    .unwrap_or_else(|| StringOrOtherId::Empty);
                let suffix = message
                    .get_attribute("suffix")
                    .map(|s| StringOrOtherId::parse_pattern_str("suffix", s.value()).unwrap());

                if let StringOrOtherId::Id(id) = &name {
                    // if result is Err -> leave unchanged, ss14_ru has a parent that does not exist
                    if let Ok(is_empty) =
                        check_if_proto_has_empty_string(&ftl_project, id, MatchingProperty::Name)
                    {
                        if !is_empty && prototype.borrow().name() != Some(&name) {
                            log::info!("Prototype '{}' changed name from '{:?}' to '{}'", prototype.borrow().id, prototype.borrow().name(), id);
                            *prototype.borrow_mut().name_mut() = Some(name);
                        }
                    }
                } else {
                    *prototype.borrow_mut().name_mut() = Some(name);
                }

                if let StringOrOtherId::Id(id) = &description {
                    // if result is Err -> leave unchanged, ss14_ru has a parent that does not exist
                    if let Ok(is_empty) = check_if_proto_has_empty_string(
                        &ftl_project,
                        id,
                        MatchingProperty::Description,
                    ) {
                        if !is_empty && prototype.borrow().description() != Some(&description) {
                            log::info!("Prototype '{}' changed desc from '{:?}' to '{}'", prototype.borrow().id, prototype.borrow().description(), id);
                            *prototype.borrow_mut().description_mut() = Some(description);
                        }
                    }
                } else {
                    *prototype.borrow_mut().description_mut() = Some(description);
                }

                if let Some(suffix) = suffix {
                    if let StringOrOtherId::Id(id) = &suffix {
                        // if result is Err -> leave unchanged, ss14_ru has a parent that does not exist
                        if let Ok(is_empty) = check_if_proto_has_empty_string(
                            &ftl_project,
                            id,
                            MatchingProperty::Suffix,
                        ) {
                            if !is_empty && prototype.borrow().suffix() != Some(&suffix) {
                                log::info!("Prototype '{}' changed suffix from '{:?}' to '{}'", prototype.borrow().id, prototype.borrow().suffix(), id);
                                *prototype.borrow_mut().suffix_mut() = Some(suffix);
                            }
                        }
                    } else {
                        *prototype.borrow_mut().suffix_mut() = Some(suffix);
                    }
                }
            }
        }
    }

    ftl_project.save();
}

enum MatchingProperty {
    Name,
    Description,
    Suffix,
}

struct ProtoNotFound;

fn check_if_proto_has_empty_string(
    ftl_project: &FtlStorage,
    id: &str,
    property: MatchingProperty,
) -> Result<bool, ProtoNotFound> {
    let Some(prototype) = ftl_project.find_prototype(id) else {
        return Err(ProtoNotFound);
    };

    let prototype = prototype.borrow();

    let s = match property {
        MatchingProperty::Name => &prototype.name(),
        MatchingProperty::Description => &prototype.description(),
        MatchingProperty::Suffix => &prototype.suffix(),
    };

    match s {
        None => Ok(true),
        Some(StringOrOtherId::Empty) => Ok(true),
        Some(StringOrOtherId::String(s)) => Ok(s.is_empty()),
        Some(StringOrOtherId::Id(id)) => check_if_proto_has_empty_string(ftl_project, id, property),
    }
}
