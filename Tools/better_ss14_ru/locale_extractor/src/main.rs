mod ftl;

use std::collections::HashSet;

use common::SS14Project;
use ftl::{SourceFtlStorage, TargetFtlStorage};
use simple_logger::SimpleLogger;

const SKIPPED_FOLDER_NAMES: &[&str] = &["ss14-ru", "ss14-ru-better"];

const SOURCE_LOCALE: &str = "en-US";
const TARGET_LOCALE: &str = "ru-RU";

const CREATE_HASH_IF_EMPTY: bool = true;

fn main() {
    SimpleLogger::new()
        .with_level(log::LevelFilter::Info)
        .init()
        .unwrap();

    let project = SS14Project::new("../../".into());

    let source_storage =
        SourceFtlStorage::load(project.get_locale_path(SOURCE_LOCALE), SKIPPED_FOLDER_NAMES)
            .unwrap();
    let mut target_storage =
        TargetFtlStorage::load(project.get_locale_path(TARGET_LOCALE), SKIPPED_FOLDER_NAMES)
            .unwrap();

    // check all target ftls for changes using hash
    for message in target_storage.iter_messages_mut() {
        let id = message.id();
        let Some(source_message) = source_storage.find(id) else {
            continue; // TODO: was removed
        };

        let source_hash = source_message.calculate_hash();
        let target_hash = if CREATE_HASH_IF_EMPTY {
            match message.get_hash() {
                Ok(Some(v)) => v,
                Ok(None) => {
                    message.inner_mut().comment = Some(fluent_syntax::ast::Comment {
                        content: vec![format!("HASH: {}", hex::encode(source_hash))],
                    });
                    source_hash
                }
                Err(ftl::GetHashError::HashCommentNoPrefix) => {
                    message
                        .inner_mut()
                        .comment
                        .as_mut()
                        .unwrap()
                        .content
                        .insert(0, format!("HASH: {}", hex::encode(source_hash)));
                    source_hash
                }
                Err(_) => panic!(),
            }
        } else {
            message.get_hash().unwrap().unwrap()
        };

        if source_hash != target_hash {
            let source_inner = source_message.inner();
            let mut target_inner = message.inner_mut();
            target_inner.attributes = source_inner.attributes.clone();
            target_inner.value = source_inner.value.clone();
            target_inner.comment = Some(fluent_syntax::ast::Comment {
                content: vec![format!("HASH: {}", hex::encode(source_hash))],
            });
        }
    }

    // add missing ftls from source to target
    for (source_path, message) in source_storage.iter_messages() {
        let file = if let Some(v) = target_storage.get_file_mut(&source_path) {
            v
        } else {
            target_storage.add_file(source_path.to_path_buf());
            target_storage.get_file_mut(&source_path).unwrap()
        };

        if !file.contains_id(message.id()) {
            let mut message_inner = message.inner().clone();
            message_inner.comment = Some(fluent_syntax::ast::Comment {
                content: vec![format!("HASH: {}", hex::encode(message.calculate_hash()))],
            });
            file.add_message(message_inner);
        }
    }

    // remove extra ftls from target that are not present in source
    let mut files_to_remove = HashSet::new();
    let mut ftls_to_remove = Vec::new();
    for (target_path, message) in target_storage.iter_messages() {
        let Some(file) = source_storage.get_file(&target_path) else {
            files_to_remove.insert(target_path.clone());
            continue;
        };
        if !file.contains_id(message.id()) {
            ftls_to_remove.push((target_path.clone(), message.id().clone()));
        }
    }

    for target_path in files_to_remove {
        target_storage.remove_file(&target_path);
    }

    for (target_path, target_id) in ftls_to_remove {
        let file = target_storage.get_file_mut(&target_path).unwrap();
        file.remove_entry(&target_id);
    }

    target_storage.save();
}
