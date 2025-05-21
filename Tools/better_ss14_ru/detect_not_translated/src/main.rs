use std::path::PathBuf;

use common::prototypes_ftl::{FtlStorage, StringOrOtherId};
use simple_logger::SimpleLogger;

const SAVE_LOCALE_PATH: &str = "../../Resources/Locale/ru-RU/ss14-ru-better/";

fn main() {
    SimpleLogger::new()
        .with_level(log::LevelFilter::Info)
        .init()
        .unwrap();

    let save_locale_path = &PathBuf::from(SAVE_LOCALE_PATH);

    let ftl_project = FtlStorage::load(save_locale_path.clone()).unwrap();

    for (_, file) in &ftl_project.files {
        for prototype in &file.prototypes {
            let prototype = prototype.borrow();
            let mut is_translated = true;
            if let Some(StringOrOtherId::String(v)) = prototype.name() {
                is_translated = is_translated && v.contains(|c: char| !c.is_ascii());
            }
            if let Some(StringOrOtherId::String(v)) = prototype.description() {
                is_translated = is_translated
                    && (v.contains(|c: char| !c.is_ascii())
                        || v.chars().all(|v| v.is_ascii_digit()));
            }
            if let Some(StringOrOtherId::String(v)) = prototype.suffix() {
                is_translated = is_translated
                    && (v.contains(|c: char| !c.is_ascii())
                        || v.chars().all(|v| v.is_ascii_digit()));
            }
            if !is_translated {
                log::info!("Prototype id {{ ent-{} }} is not translated!", prototype.id);
            }
        }
    }
}
