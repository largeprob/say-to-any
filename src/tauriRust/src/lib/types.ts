export type MainSection = 'home' | 'history';
export type PreferencesSection = 'account' | 'settings' | 'about';

export type AppSettings = {
  lmBaseUrl: string;
  lmApiKey: string;
  lmModel: string;
  lmTemperature: number;
  asrBaseUrl: string;
  asrApiKey: string;
  asrModel: string;
  baseUrl: string;
  apiKey: string;
  llmModel: string;
  temperature: number;
  language: string;
  appLanguage: string;
  timeoutSeconds: number;
  enableTextCleanup: boolean;
  asrEnableItn: boolean;
  microphoneDeviceNumber: number;
  hotkey: string;
  autoPasteAfterDictation: boolean;
  historyRetention: string;
  maxRecordingSeconds: number;
};

export type HistoryItem = {
  id: string;
  createdAt: string;
  rawText: string;
  finalText: string;
  audioFilePath: string;
};

export type AudioDeviceInfo = {
  deviceNumber: number;
  name: string;
  defaultDeviceName?: string | null;
  displayName: string;
  description: string;
};

export type PlatformStatus = {
  os: string;
  supportsRecording: boolean;
  supportsAutoPaste: boolean;
  supportsGlobalHotkey: boolean;
};

export type AppStateResponse = {
  settings: AppSettings;
  history: HistoryItem[];
  microphones: AudioDeviceInfo[];
  platform: PlatformStatus;
  appDataPath: string;
};

export type DictationResult = {
  rawText: string;
  finalText: string;
  audioFilePath: string;
  pasted: boolean;
  historyItem: HistoryItem;
};

export type PasteResult = {
  pasted: boolean;
  message: string;
};

export type AuthSession = {
  accessToken: string;
  accessTokenExpiresAt: string;
  userId: number;
  userName: string;
  isAdmin: boolean;
};
