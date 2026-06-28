import type { AppSettings, AppStateResponse, HistoryItem } from './types';

export const defaultSettings: AppSettings = {
  lmBaseUrl: 'https://api.openai.com/v1',
  lmApiKey: '',
  lmModel: 'gpt-4o-mini',
  lmTemperature: 0.2,
  asrBaseUrl: 'https://api.openai.com/v1',
  asrApiKey: '',
  asrModel: 'qwen3-asr-flash',
  baseUrl: 'https://api.openai.com/v1',
  apiKey: '',
  llmModel: 'gpt-4o-mini',
  temperature: 0.2,
  language: 'auto',
  appLanguage: '简体中文',
  timeoutSeconds: 60,
  enableTextCleanup: true,
  asrEnableItn: false,
  microphoneDeviceNumber: -1,
  hotkey: '双击 Alt',
  autoPasteAfterDictation: true,
  historyRetention: 'Forever',
  maxRecordingSeconds: 120,
};

export const demoHistory: HistoryItem[] = [
  {
    id: 'demo-1',
    createdAt: new Date().toISOString(),
    rawText: '这是一条用于浏览器预览的听写记录。',
    finalText: '这是一条用于浏览器预览的听写记录。',
    audioFilePath: '',
  },
];

export const fallbackState: AppStateResponse = {
  settings: defaultSettings,
  history: demoHistory,
  microphones: [
    {
      deviceNumber: -1,
      name: '自动检测',
      displayName: '自动检测',
      description: '使用系统当前默认输入设备。',
    },
  ],
  platform: {
    os: 'browser-preview',
    supportsRecording: false,
    supportsAutoPaste: false,
    supportsGlobalHotkey: false,
  },
  appDataPath: '',
};
