use reqwest::{Method, Url, header::HeaderValue};
use serde::Deserialize;
use yaml_rust2::{Yaml, YamlEmitter};

const OUT_FILE: &str = "tts_voices.yml";

fn main() {
    let _ = dotenvy::dotenv();

    let api_url = std::env::var("API_URL").expect("API_URL was not set");
    let api_token = std::env::var("API_TOKEN").expect("API_TOKEN was not set");

    let mut request = reqwest::blocking::Request::new(Method::GET, Url::parse(&api_url).unwrap());

    request.headers_mut().append(
        "Authorization",
        HeaderValue::from_str(&format!("Bearer {}", api_token))
            .expect("Couldn't convert api_token string into a header value"),
    );

    let response: GetSpeakersResponse = reqwest::blocking::Client::new()
        .execute(request)
        .expect("api request failed")
        .json()
        .expect("json parsing failed");

    let yaml = {
        let mut vec = Vec::new();

        fn write_speaker(vec: &mut Vec<Yaml>, speaker: &Speaker) {
            /*
                      - type: ttsVoice
            id: Kseniya
            name: tts-voice-name-kseniya
            sex: Female
            speaker: kseniya
            roundStart: false
                       */

            let mut map = yaml_rust2::yaml::Hash::new();
            map.insert(
                Yaml::String("type".to_owned()),
                Yaml::String("ttsVoice".to_owned()),
            );
            map.insert(
                Yaml::String("id".to_owned()),
                Yaml::String(speaker.speakers[0].clone()),
            );
            map.insert(
                Yaml::String("name".to_owned()),
                Yaml::String(speaker.name.clone()),
            );
            map.insert(
                Yaml::String("sex".to_owned()),
                Yaml::String(
                    match speaker.gender {
                        Gender::Male => "male",
                        Gender::Female => "female",
                    }
                    .to_owned(),
                ),
            );
            map.insert(
                Yaml::String("speaker".to_owned()),
                Yaml::String(speaker.speakers[0].clone()),
            );
            map.insert(Yaml::String("roundStart".to_owned()), Yaml::Boolean(false));
            vec.push(Yaml::Hash(map))
        }

        for speaker in response.voices {
            write_speaker(&mut vec, &speaker);
        }

        for speaker in response.custom_voices {
            write_speaker(&mut vec, &speaker);
        }

        Yaml::Array(vec)
    };

    let mut output = String::new();
    YamlEmitter::new(&mut output).dump(&yaml).unwrap();

    std::fs::write(OUT_FILE, &output).expect("Failed to write output to the output file");
}

#[derive(Deserialize)]
pub struct GetSpeakersResponse {
    pub voices: Vec<Speaker>,
    pub custom_voices: Vec<Speaker>,
}

#[derive(Deserialize)]
pub struct Speaker {
    pub name: String,
    pub description: String,
    pub source: String,
    pub gender: Gender,
    pub speakers: Vec<String>,
}

#[derive(Deserialize)]
#[serde(rename_all = "lowercase")] 
pub enum Gender {
    Male,
    Female,
}
