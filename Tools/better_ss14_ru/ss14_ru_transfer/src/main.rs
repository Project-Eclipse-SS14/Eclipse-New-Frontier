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

    let mut ftl_project = FtlStorage::load(save_locale_path.clone()).unwrap();

    for (_, file) in &mut ftl_project.files {
        for prototype in &mut file.prototypes {
            if let Some(message) = bundle.get_message(&format!("ent-{}", prototype.id)) {
                let name =
                    StringOrOtherId::parse_pattern_str("name", message.value().unwrap()).unwrap();

                let description = message
                    .get_attribute("desc")
                    .map(|v| StringOrOtherId::parse_pattern_str("desc", v.value()).unwrap())
                    .unwrap_or_else(|| StringOrOtherId::Empty);
                let suffix = message
                    .get_attribute("suffix")
                    .map(|s| StringOrOtherId::parse_pattern_str("suffix", s.value()).unwrap());

                *prototype.name_mut() = Some(name);
                *prototype.description_mut() = Some(description);
                *prototype.suffix_mut() = suffix;
            }
        }
    }

    ftl_project.save();
}
