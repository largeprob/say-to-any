use std::{
    fs,
    path::{Path, PathBuf},
    sync::{Arc, Mutex},
};

use arboard::Clipboard;
use base64::{engine::general_purpose, Engine as _};
use chrono::{DateTime, Duration, Utc};
use cpal::{
    traits::{DeviceTrait, HostTrait, StreamTrait},
    SampleFormat, Stream,
};
use directories::ProjectDirs;
use serde::{Deserialize, Serialize};
use serde_json::{json, Value};
use uuid::Uuid;

const DEFAULT_BASE_URL: &str = "https://api.openai.com/v1";
const DEFAULT_CLEANUP_PROMPT: &str = r#"你是一个专业的语音转文字内容清洗助手。你的任务是对用户提供的语音识别原始文本进行智能清洗，输出清晰、准确、保留原意的书面内容。

## 清洗基本原则
1. 去除无意义填充词。
2. 删除完全重复的词语、短语或整句。
3. 保留所有有意义的信息，不随意改变原意。
4. 修正明显错别字。
5. 使用标准中文标点并合理断句。

## 多语言处理规则
对于中文以外的任何语言，必须原样保留，不得翻译成中文，不得删除。

## 特殊处理：任务型内容 → To-Do List
如果原始内容包含一系列行动项、步骤、指令或待办事项，请整理为有序编号列表。

## 输出格式
仅输出清洗后的完整文本，不要添加任何解释、说明、前缀或后缀。"#;

#[derive(Clone, Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct AppSettings {
    #[serde(default)]
    pub lm_base_url: String,
    #[serde(default)]
    pub lm_api_key: String,
    #[serde(default)]
    pub lm_model: String,
    #[serde(default)]
    pub lm_temperature: f64,
    #[serde(default)]
    pub asr_base_url: String,
    #[serde(default)]
    pub asr_api_key: String,
    #[serde(default)]
    pub asr_model: String,
    #[serde(default)]
    pub base_url: String,
    #[serde(default)]
    pub api_key: String,
    #[serde(default)]
    pub llm_model: String,
    #[serde(default)]
    pub temperature: f64,
    #[serde(default)]
    pub language: String,
    #[serde(default)]
    pub app_language: String,
    #[serde(default)]
    pub timeout_seconds: u64,
    #[serde(default)]
    pub enable_text_cleanup: bool,
    #[serde(default)]
    pub asr_enable_itn: bool,
    #[serde(default = "automatic_microphone")]
    pub microphone_device_number: i32,
    #[serde(default)]
    pub hotkey: String,
    #[serde(default)]
    pub auto_paste_after_dictation: bool,
    #[serde(default)]
    pub history_retention: String,
    #[serde(default)]
    pub max_recording_seconds: u64,
}

impl Default for AppSettings {
    fn default() -> Self {
        normalize_settings(AppSettings {
            lm_base_url: String::new(),
            lm_api_key: String::new(),
            lm_model: "gpt-4o-mini".to_string(),
            lm_temperature: 0.2,
            asr_base_url: String::new(),
            asr_api_key: String::new(),
            asr_model: "qwen3-asr-flash".to_string(),
            base_url: DEFAULT_BASE_URL.to_string(),
            api_key: String::new(),
            llm_model: "gpt-4o-mini".to_string(),
            temperature: 0.2,
            language: "auto".to_string(),
            app_language: "简体中文".to_string(),
            timeout_seconds: 60,
            enable_text_cleanup: true,
            asr_enable_itn: false,
            microphone_device_number: automatic_microphone(),
            hotkey: "双击 Alt".to_string(),
            auto_paste_after_dictation: true,
            history_retention: "Forever".to_string(),
            max_recording_seconds: 120,
        })
    }
}

#[derive(Clone, Debug, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct HistoryItem {
    #[serde(default = "new_id")]
    pub id: String,
    pub created_at: String,
    #[serde(default)]
    pub raw_text: String,
    #[serde(default)]
    pub final_text: String,
    #[serde(default)]
    pub audio_file_path: String,
}

#[derive(Clone, Debug, Serialize, Deserialize, Default)]
#[serde(rename_all = "camelCase")]
pub struct ApplicationDataFile {
    #[serde(default)]
    pub settings: AppSettings,
    #[serde(default)]
    pub history: Vec<HistoryItem>,
}

#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AudioDeviceInfo {
    pub device_number: i32,
    pub name: String,
    pub default_device_name: Option<String>,
    pub display_name: String,
    pub description: String,
}

#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PlatformStatus {
    pub os: String,
    pub supports_recording: bool,
    pub supports_auto_paste: bool,
    pub supports_global_hotkey: bool,
}

#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct AppStateResponse {
    pub settings: AppSettings,
    pub history: Vec<HistoryItem>,
    pub microphones: Vec<AudioDeviceInfo>,
    pub platform: PlatformStatus,
    pub app_data_path: String,
}

#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct DictationResult {
    pub raw_text: String,
    pub final_text: String,
    pub audio_file_path: String,
    pub pasted: bool,
    pub history_item: HistoryItem,
}

#[derive(Clone, Debug, Serialize)]
#[serde(rename_all = "camelCase")]
pub struct PasteResult {
    pub pasted: bool,
    pub message: String,
}

pub struct AppRuntime {
    data_path: PathBuf,
    audio_dir: PathBuf,
    data: Mutex<ApplicationDataFile>,
    recording: Mutex<Option<RecordingSession>>,
}

struct RecordingSession {
    stream: Stream,
    samples: Arc<Mutex<Vec<i16>>>,
    sample_rate: u32,
    channels: u16,
}

impl AppRuntime {
    fn new() -> Self {
        let data_path = data_file_path();
        let audio_dir = data_path
            .parent()
            .unwrap_or_else(|| Path::new("."))
            .join("audio");
        let data = load_data_from_path(&data_path).unwrap_or_default();

        Self {
            data_path,
            audio_dir,
            data: Mutex::new(normalize_data(data)),
            recording: Mutex::new(None),
        }
    }
}

#[tauri::command]
fn load_app_state(state: tauri::State<'_, AppRuntime>) -> Result<AppStateResponse, String> {
    let data = state
        .data
        .lock()
        .map_err(|_| "无法读取应用状态。".to_string())?
        .clone();

    Ok(AppStateResponse {
        settings: data.settings,
        history: data.history,
        microphones: list_microphones_inner(),
        platform: platform_status(),
        app_data_path: state.data_path.display().to_string(),
    })
}

#[tauri::command]
fn list_microphones() -> Vec<AudioDeviceInfo> {
    list_microphones_inner()
}

#[tauri::command]
fn save_settings(
    settings: AppSettings,
    state: tauri::State<'_, AppRuntime>,
) -> Result<AppStateResponse, String> {
    {
        let mut data = state
            .data
            .lock()
            .map_err(|_| "无法保存设置。".to_string())?;
        data.settings = normalize_settings(settings);
        apply_history_retention(&mut data);
        save_data_to_path(&state.data_path, &data)?;
    }

    load_app_state(state)
}

#[tauri::command]
fn start_recording(device_number: i32, state: tauri::State<'_, AppRuntime>) -> Result<(), String> {
    let mut slot = state
        .recording
        .lock()
        .map_err(|_| "无法访问录音状态。".to_string())?;

    if slot.is_some() {
        return Ok(());
    }

    fs::create_dir_all(&state.audio_dir).map_err(|error| error.to_string())?;
    let session = RecordingSession::start(device_number)?;
    *slot = Some(session);
    Ok(())
}

#[tauri::command]
async fn cancel_recording(state: tauri::State<'_, AppRuntime>) -> Result<(), String> {
    let _ = stop_recording_to_file(&state)?;
    Ok(())
}

#[tauri::command]
async fn stop_and_process(
    settings: AppSettings,
    state: tauri::State<'_, AppRuntime>,
) -> Result<DictationResult, String> {
    let settings = normalize_settings(settings);
    let audio_file_path = stop_recording_to_file(&state)?;
    let raw_text = transcribe_audio(&audio_file_path, &settings).await?;

    if raw_text.trim().is_empty() {
        return Err("未识别到文本。".to_string());
    }

    let final_text = cleanup_text(&raw_text, &settings).await?;
    let mut pasted = false;

    if settings.auto_paste_after_dictation {
        copy_text_inner(&final_text)?;
        pasted = false;
    }

    let item = HistoryItem {
        id: new_id(),
        created_at: Utc::now().to_rfc3339(),
        raw_text: raw_text.clone(),
        final_text: final_text.clone(),
        audio_file_path: audio_file_path.clone(),
    };

    {
        let mut data = state
            .data
            .lock()
            .map_err(|_| "无法保存历史记录。".to_string())?;
        data.settings = settings;

        if data.settings.history_retention == "Never" {
            let _ = fs::remove_file(&audio_file_path);
        } else {
            data.history.insert(0, item.clone());
            apply_history_retention(&mut data);
        }

        save_data_to_path(&state.data_path, &data)?;
    }

    Ok(DictationResult {
        raw_text,
        final_text,
        audio_file_path,
        pasted,
        history_item: item,
    })
}

#[tauri::command]
fn copy_text(text: String) -> Result<(), String> {
    copy_text_inner(&text)
}

#[tauri::command]
fn paste_text(text: String) -> Result<PasteResult, String> {
    copy_text_inner(&text)?;
    Ok(PasteResult {
        pasted: false,
        message: "已复制到剪贴板。Tauri 版自动按键粘贴将在平台层补齐。".to_string(),
    })
}

#[tauri::command]
async fn test_connection(settings: AppSettings) -> Result<String, String> {
    let settings = normalize_settings(settings);
    let response = post_chat_completion(
        &settings.lm_base_url,
        &settings.lm_api_key,
        json!({
            "model": settings.lm_model,
            "messages": [{"role": "user", "content": "ping"}],
            "temperature": 0.1,
            "max_tokens": 16
        }),
        settings.timeout_seconds,
    )
    .await?;

    let _ = extract_message_content(&response);
    Ok("连接成功".to_string())
}

#[tauri::command]
fn delete_history_item(
    id: String,
    state: tauri::State<'_, AppRuntime>,
) -> Result<Vec<HistoryItem>, String> {
    let mut data = state
        .data
        .lock()
        .map_err(|_| "无法删除历史记录。".to_string())?;

    if let Some(index) = data.history.iter().position(|item| item.id == id) {
        let item = data.history.remove(index);
        if !item.audio_file_path.is_empty() {
            let _ = fs::remove_file(item.audio_file_path);
        }
        save_data_to_path(&state.data_path, &data)?;
    }

    Ok(data.history.clone())
}

impl RecordingSession {
    fn start(device_number: i32) -> Result<Self, String> {
        let host = cpal::default_host();
        let device = if device_number < 0 {
            host.default_input_device()
        } else {
            host.input_devices()
                .map_err(|error| error.to_string())?
                .nth(device_number as usize)
        }
        .ok_or_else(|| "未发现可用麦克风。".to_string())?;

        let config = device
            .default_input_config()
            .map_err(|error| error.to_string())?;
        let sample_format = config.sample_format();
        let stream_config: cpal::StreamConfig = config.into();
        let sample_rate = stream_config.sample_rate.0;
        let channels = stream_config.channels;
        let samples = Arc::new(Mutex::new(Vec::<i16>::new()));
        let err_fn = |error| eprintln!("audio stream error: {error}");

        let stream = match sample_format {
            SampleFormat::F32 => {
                let target = Arc::clone(&samples);
                device.build_input_stream(
                    &stream_config,
                    move |data: &[f32], _| push_f32_samples(data, &target),
                    err_fn,
                    None,
                )
            }
            SampleFormat::I16 => {
                let target = Arc::clone(&samples);
                device.build_input_stream(
                    &stream_config,
                    move |data: &[i16], _| push_i16_samples(data, &target),
                    err_fn,
                    None,
                )
            }
            SampleFormat::U16 => {
                let target = Arc::clone(&samples);
                device.build_input_stream(
                    &stream_config,
                    move |data: &[u16], _| push_u16_samples(data, &target),
                    err_fn,
                    None,
                )
            }
            other => return Err(format!("不支持的麦克风采样格式：{other:?}")),
        }
        .map_err(|error| error.to_string())?;

        stream.play().map_err(|error| error.to_string())?;

        Ok(Self {
            stream,
            samples,
            sample_rate,
            channels,
        })
    }
}

fn stop_recording_to_file(state: &AppRuntime) -> Result<String, String> {
    let session = {
        let mut slot = state
            .recording
            .lock()
            .map_err(|_| "无法访问录音状态。".to_string())?;
        slot.take()
    };

    let Some(session) = session else {
        return Err("当前没有正在进行的录音。".to_string());
    };

    drop(session.stream);
    fs::create_dir_all(&state.audio_dir).map_err(|error| error.to_string())?;
    let path = state.audio_dir.join(format!(
        "dictation-{}.wav",
        Utc::now().format("%Y%m%d-%H%M%S")
    ));
    let samples = session
        .samples
        .lock()
        .map_err(|_| "无法读取录音数据。".to_string())?;

    let spec = hound::WavSpec {
        channels: session.channels,
        sample_rate: session.sample_rate,
        bits_per_sample: 16,
        sample_format: hound::SampleFormat::Int,
    };
    let mut writer = hound::WavWriter::create(&path, spec).map_err(|error| error.to_string())?;
    for sample in samples.iter() {
        writer
            .write_sample(*sample)
            .map_err(|error| error.to_string())?;
    }
    writer.finalize().map_err(|error| error.to_string())?;

    Ok(path.display().to_string())
}

async fn transcribe_audio(audio_file_path: &str, settings: &AppSettings) -> Result<String, String> {
    if settings.asr_api_key.trim().is_empty() {
        return Err("请先填写 ASR API Key。".to_string());
    }

    let bytes = tokio::fs::read(audio_file_path)
        .await
        .map_err(|error| error.to_string())?;
    let data_uri = format!(
        "data:{};base64,{}",
        audio_mime_type(audio_file_path),
        general_purpose::STANDARD.encode(bytes)
    );

    let mut asr_options = json!({
        "enable_itn": settings.asr_enable_itn
    });
    if !settings.language.trim().is_empty() && !settings.language.eq_ignore_ascii_case("auto") {
        asr_options["language"] = json!(settings.language.trim());
    }

    let response = post_chat_completion(
        &settings.asr_base_url,
        &settings.asr_api_key,
        json!({
            "model": settings.asr_model,
            "messages": [{
                "role": "user",
                "content": [{
                    "type": "input_audio",
                    "input_audio": {
                        "data": data_uri
                    }
                }]
            }],
            "stream": false,
            "asr_options": asr_options
        }),
        settings.timeout_seconds,
    )
    .await?;

    Ok(extract_message_content(&response).trim().to_string())
}

async fn cleanup_text(raw_text: &str, settings: &AppSettings) -> Result<String, String> {
    if !settings.enable_text_cleanup || raw_text.trim().is_empty() {
        return Ok(raw_text.to_string());
    }

    if settings.lm_api_key.trim().is_empty() {
        return Ok(raw_text.to_string());
    }

    let response = post_chat_completion(
        &settings.lm_base_url,
        &settings.lm_api_key,
        json!({
            "model": settings.lm_model,
            "temperature": settings.lm_temperature,
            "messages": [
                {"role": "system", "content": DEFAULT_CLEANUP_PROMPT},
                {"role": "user", "content": format!("原始语音识别文本：\n{raw_text}")}
            ]
        }),
        settings.timeout_seconds,
    )
    .await?;

    let content = extract_message_content(&response).trim().to_string();
    Ok(if content.is_empty() {
        raw_text.to_string()
    } else {
        content
    })
}

async fn post_chat_completion(
    base_url: &str,
    api_key: &str,
    payload: Value,
    timeout_seconds: u64,
) -> Result<Value, String> {
    if api_key.trim().is_empty() {
        return Err("请先填写 API Key。".to_string());
    }

    let url = format!("{}/chat/completions", normalize_base_url(base_url));
    let client = reqwest::Client::builder()
        .timeout(std::time::Duration::from_secs(
            timeout_seconds.clamp(5, 300),
        ))
        .build()
        .map_err(|error| error.to_string())?;

    let response = client
        .post(url)
        .bearer_auth(api_key.trim())
        .json(&payload)
        .send()
        .await
        .map_err(|error| format!("OpenAI 兼容请求失败：{error}"))?;
    let status = response.status();
    let body = response
        .text()
        .await
        .map_err(|error| format!("读取响应失败：{error}"))?;

    if !status.is_success() {
        return Err(format!("OpenAI 兼容请求失败 {status}: {body}"));
    }

    serde_json::from_str(&body).map_err(|error| format!("解析响应失败：{error}"))
}

fn extract_message_content(body: &Value) -> String {
    let Some(content) = body
        .get("choices")
        .and_then(Value::as_array)
        .and_then(|choices| choices.first())
        .and_then(|choice| choice.get("message"))
        .and_then(|message| message.get("content"))
    else {
        return String::new();
    };

    match content {
        Value::String(value) => value.clone(),
        Value::Array(parts) => parts
            .iter()
            .filter_map(|part| {
                if let Some(text) = part.as_str() {
                    Some(text.to_string())
                } else {
                    part.get("text").and_then(Value::as_str).map(str::to_string)
                }
            })
            .collect(),
        other => other.to_string(),
    }
}

fn push_f32_samples(input: &[f32], target: &Arc<Mutex<Vec<i16>>>) {
    if let Ok(mut samples) = target.lock() {
        samples.extend(input.iter().map(|sample| {
            let clamped = sample.clamp(-1.0, 1.0);
            (clamped * i16::MAX as f32) as i16
        }));
    }
}

fn push_i16_samples(input: &[i16], target: &Arc<Mutex<Vec<i16>>>) {
    if let Ok(mut samples) = target.lock() {
        samples.extend_from_slice(input);
    }
}

fn push_u16_samples(input: &[u16], target: &Arc<Mutex<Vec<i16>>>) {
    if let Ok(mut samples) = target.lock() {
        samples.extend(input.iter().map(|sample| (*sample as i32 - 32768) as i16));
    }
}

fn list_microphones_inner() -> Vec<AudioDeviceInfo> {
    let host = cpal::default_host();
    let default_device_name = host
        .default_input_device()
        .and_then(|device| device.name().ok());
    let mut devices = vec![AudioDeviceInfo::automatic(default_device_name.clone())];

    if let Ok(input_devices) = host.input_devices() {
        for (index, device) in input_devices.enumerate() {
            let name = device
                .name()
                .unwrap_or_else(|_| format!("输入设备 {}", index + 1));
            devices.push(AudioDeviceInfo {
                device_number: index as i32,
                display_name: name.clone(),
                description: "手动指定此输入设备。".to_string(),
                name,
                default_device_name: None,
            });
        }
    }

    devices
}

impl AudioDeviceInfo {
    fn automatic(default_device_name: Option<String>) -> Self {
        let display_name = default_device_name
            .as_ref()
            .filter(|value| !value.trim().is_empty())
            .map(|value| format!("自动检测（当前默认：{value}）"))
            .unwrap_or_else(|| "自动检测".to_string());

        Self {
            device_number: automatic_microphone(),
            name: "自动检测".to_string(),
            default_device_name,
            display_name,
            description: "使用系统当前默认输入设备。".to_string(),
        }
    }
}

fn platform_status() -> PlatformStatus {
    PlatformStatus {
        os: std::env::consts::OS.to_string(),
        supports_recording: true,
        supports_auto_paste: false,
        supports_global_hotkey: false,
    }
}

fn copy_text_inner(text: &str) -> Result<(), String> {
    let mut clipboard = Clipboard::new().map_err(|error| error.to_string())?;
    clipboard
        .set_text(text.to_string())
        .map_err(|error| error.to_string())
}

fn load_data_from_path(path: &Path) -> Result<ApplicationDataFile, String> {
    if !path.exists() {
        return Ok(ApplicationDataFile::default());
    }

    let json = fs::read_to_string(path).map_err(|error| error.to_string())?;
    serde_json::from_str(&json).map_err(|error| error.to_string())
}

fn save_data_to_path(path: &Path, data: &ApplicationDataFile) -> Result<(), String> {
    if let Some(parent) = path.parent() {
        fs::create_dir_all(parent).map_err(|error| error.to_string())?;
    }

    let json = serde_json::to_string_pretty(data).map_err(|error| error.to_string())?;
    fs::write(path, json).map_err(|error| error.to_string())
}

fn normalize_data(mut data: ApplicationDataFile) -> ApplicationDataFile {
    data.settings = normalize_settings(data.settings);
    for item in &mut data.history {
        if item.id.trim().is_empty() {
            item.id = new_id();
        }
    }
    apply_history_retention(&mut data);
    data
}

fn normalize_settings(mut settings: AppSettings) -> AppSettings {
    if settings.base_url.trim().is_empty() {
        settings.base_url = DEFAULT_BASE_URL.to_string();
    }

    if settings.lm_base_url.trim().is_empty() {
        settings.lm_base_url = settings.base_url.clone();
    }

    if settings.asr_base_url.trim().is_empty() {
        settings.asr_base_url = settings.base_url.clone();
    }

    if settings.lm_api_key.trim().is_empty() && !settings.api_key.trim().is_empty() {
        settings.lm_api_key = settings.api_key.clone();
    }

    if settings.asr_api_key.trim().is_empty() && !settings.api_key.trim().is_empty() {
        settings.asr_api_key = settings.api_key.clone();
    }

    if settings.lm_model.trim().is_empty() {
        settings.lm_model = if settings.llm_model.trim().is_empty() {
            "gpt-4o-mini".to_string()
        } else {
            settings.llm_model.clone()
        };
    }

    if settings.asr_model.trim().is_empty() {
        settings.asr_model = "qwen3-asr-flash".to_string();
    }

    if settings.lm_temperature.is_nan() {
        settings.lm_temperature = settings.temperature;
    }
    settings.lm_temperature = settings.lm_temperature.clamp(0.0, 2.0);
    settings.llm_model = settings.lm_model.clone();
    settings.temperature = settings.lm_temperature;

    if settings.language.trim().is_empty() {
        settings.language = "auto".to_string();
    }
    if settings.app_language.trim().is_empty() {
        settings.app_language = "简体中文".to_string();
    }
    if settings.timeout_seconds < 5 {
        settings.timeout_seconds = 60;
    }
    if settings.max_recording_seconds < 5 {
        settings.max_recording_seconds = 120;
    }
    if settings.microphone_device_number < automatic_microphone() {
        settings.microphone_device_number = automatic_microphone();
    }
    if settings.hotkey.trim().is_empty() {
        settings.hotkey = "双击 Alt".to_string();
    }
    if !matches!(
        settings.history_retention.as_str(),
        "Never" | "24Hours" | "OneWeek" | "OneMonth" | "Forever"
    ) {
        settings.history_retention = "Forever".to_string();
    }

    settings
}

fn apply_history_retention(data: &mut ApplicationDataFile) {
    let now = Utc::now();
    let retention = data.settings.history_retention.as_str();

    if retention == "Forever" {
        return;
    }

    let mut removed = Vec::new();
    data.history.retain(|item| {
        let keep = match retention {
            "Never" => false,
            "24Hours" => item_age(item, now).is_none_or(|age| age <= Duration::hours(24)),
            "OneWeek" => item_age(item, now).is_none_or(|age| age <= Duration::days(7)),
            "OneMonth" => item_age(item, now).is_none_or(|age| age <= Duration::days(31)),
            _ => true,
        };

        if !keep && !item.audio_file_path.is_empty() {
            removed.push(item.audio_file_path.clone());
        }

        keep
    });

    for file in removed {
        let _ = fs::remove_file(file);
    }
}

fn item_age(item: &HistoryItem, now: DateTime<Utc>) -> Option<Duration> {
    DateTime::parse_from_rfc3339(&item.created_at)
        .ok()
        .map(|created| now - created.with_timezone(&Utc))
}

fn data_file_path() -> PathBuf {
    ProjectDirs::from("com", "Say To Any", "Say To Any")
        .map(|dirs| dirs.data_dir().join("settings.json"))
        .unwrap_or_else(|| PathBuf::from("settings.json"))
}

fn normalize_base_url(base_url: &str) -> String {
    let trimmed = base_url.trim();
    if trimmed.is_empty() {
        DEFAULT_BASE_URL.to_string()
    } else {
        trimmed.trim_end_matches('/').to_string()
    }
}

fn audio_mime_type(path: &str) -> &'static str {
    match Path::new(path)
        .extension()
        .and_then(|extension| extension.to_str())
        .unwrap_or_default()
        .to_ascii_lowercase()
        .as_str()
    {
        "mp3" => "audio/mpeg",
        "m4a" => "audio/mp4",
        "ogg" => "audio/ogg",
        "flac" => "audio/flac",
        "webm" => "audio/webm",
        "wav" => "audio/wav",
        _ => "application/octet-stream",
    }
}

fn automatic_microphone() -> i32 {
    -1
}

fn new_id() -> String {
    Uuid::new_v4().to_string()
}

pub fn run() {
    tauri::Builder::default()
        .manage(AppRuntime::new())
        .invoke_handler(tauri::generate_handler![
            load_app_state,
            list_microphones,
            save_settings,
            start_recording,
            cancel_recording,
            stop_and_process,
            copy_text,
            paste_text,
            test_connection,
            delete_history_item
        ])
        .run(tauri::generate_context!())
        .expect("error while running Say To Any Tauri app");
}
