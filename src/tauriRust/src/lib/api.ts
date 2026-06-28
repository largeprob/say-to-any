import { invoke } from '@tauri-apps/api/core';
import { getAuthStoreState } from './auth-store';
import { fallbackState } from './defaults';
import type {
  AppSettings,
  AppStateResponse,
  AuthSession,
  AudioDeviceInfo,
  DictationResult,
  HistoryItem,
  PasteResult,
} from './types';

const isTauri = () => '__TAURI_INTERNALS__' in window;

const delay = (ms: number) => new Promise((resolve) => window.setTimeout(resolve, ms));

let authBootstrapPromise: Promise<AuthSession | null> | null = null;

function getApiUrl(path: string) {
  return `/api${path.startsWith('/') ? path : `/${path}`}`;
}

type RequestApiOptions = {
  auth?: boolean;
  retryOnUnauthorized?: boolean;
};

async function requestApi<T>(
  path: string,
  init: RequestInit,
  options: RequestApiOptions = {},
): Promise<T> {
  const { auth = true, retryOnUnauthorized = true } = options;
  const session = getAuthSession();
  const response = await fetch(getApiUrl(path), {
    credentials: 'include',
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(auth && session ? { Authorization: `Bearer ${session.accessToken}` } : {}),
      ...init.headers,
    },
  });

  if (response.status === 401 && auth && retryOnUnauthorized) {
    const refreshedSession = await refreshAuthSession();
    if (refreshedSession) {
      return requestApi<T>(path, init, { auth, retryOnUnauthorized: false });
    }
  }

  if (!response.ok) {
    const text = await response.text();
    let message = `请求失败（${response.status}）`;

    if (text) {
      try {
        const body = JSON.parse(text) as { detail?: string; message?: string; title?: string };
        message = body.detail ?? body.message ?? body.title ?? message;
      } catch {
        message = text;
      }
    }

    throw new Error(message);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function getAuthSession(): AuthSession | null {
  const session = getAuthStoreState().session;
  if (!session) {
    return null;
  }

  if (Date.parse(session.accessTokenExpiresAt) <= Date.now()) {
    return null;
  }

  return session;
}

export async function initializeAuthSession(): Promise<AuthSession | null> {
  const state = getAuthStoreState();
  if (state.status === 'authenticated' && getAuthSession()) {
    return state.session;
  }

  if (authBootstrapPromise) {
    return authBootstrapPromise;
  }

  state.setChecking();
  authBootstrapPromise = refreshAuthSession().finally(() => {
    authBootstrapPromise = null;
  });

  return authBootstrapPromise;
}

export async function refreshAuthSession(): Promise<AuthSession | null> {
  const response = await fetch(getApiUrl('/Auth/refresh'), {
    method: 'POST',
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
    },
  });

  if (!response.ok) {
    getAuthStoreState().clearSession();
    return null;
  }

  const session = (await response.json()) as AuthSession;
  getAuthStoreState().setSession(session);
  return session;
}

export async function loginWithPassword(loginAccount: string, password: string): Promise<AuthSession> {
  const session = await requestApi<AuthSession>('/Auth/login', {
    method: 'POST',
    body: JSON.stringify({ loginAccount, password }),
  }, { auth: false });
  getAuthStoreState().setSession(session);
  return session;
}

export async function sendRegisterEmailCode(email: string): Promise<void> {
  await requestApi<void>('/Auth/register/send-email-code', {
    method: 'POST',
    body: JSON.stringify({ email }),
  }, { auth: false });
}

export async function registerWithEmail(
  email: string,
  verificationCode: string,
  password: string,
): Promise<AuthSession> {
  const session = await requestApi<AuthSession>('/Auth/register', {
    method: 'POST',
    body: JSON.stringify({ email, verificationCode, password }),
  }, { auth: false });
  getAuthStoreState().setSession(session);
  return session;
}

export async function logout(): Promise<void> {
  try {
    await requestApi<void>('/Auth/logout', {
      method: 'POST',
    }, { retryOnUnauthorized: false });
  } finally {
    getAuthStoreState().clearSession();
  }
}

export async function loadAppState(): Promise<AppStateResponse> {
  if (!isTauri()) {
    return fallbackState;
  }

  return invoke<AppStateResponse>('load_app_state');
}

export async function listMicrophones(): Promise<AudioDeviceInfo[]> {
  if (!isTauri()) {
    return fallbackState.microphones;
  }

  return invoke<AudioDeviceInfo[]>('list_microphones');
}

export async function saveSettings(settings: AppSettings): Promise<AppStateResponse> {
  if (!isTauri()) {
    return { ...fallbackState, settings };
  }

  return invoke<AppStateResponse>('save_settings', { settings });
}

export async function startRecording(deviceNumber: number): Promise<void> {
  if (!isTauri()) {
    await delay(240);
    return;
  }

  return invoke<void>('start_recording', { deviceNumber });
}

export async function cancelRecording(): Promise<void> {
  if (!isTauri()) {
    return;
  }

  return invoke<void>('cancel_recording');
}

export async function stopAndProcess(settings: AppSettings): Promise<DictationResult> {
  if (!isTauri()) {
    await delay(900);
    const finalText = '这是一段 Tauri 浏览器预览中的模拟识别文本。';
    return {
      rawText: finalText,
      finalText,
      audioFilePath: '',
      pasted: false,
      historyItem: {
        id: crypto.randomUUID(),
        createdAt: new Date().toISOString(),
        rawText: finalText,
        finalText,
        audioFilePath: '',
      },
    };
  }

  return invoke<DictationResult>('stop_and_process', { settings });
}

export async function copyText(text: string): Promise<void> {
  if (!isTauri()) {
    await navigator.clipboard?.writeText(text);
    return;
  }

  return invoke<void>('copy_text', { text });
}

export async function pasteText(text: string): Promise<PasteResult> {
  if (!isTauri()) {
    await navigator.clipboard?.writeText(text);
    return {
      pasted: false,
      message: '已复制到剪贴板。',
    };
  }

  return invoke<PasteResult>('paste_text', { text });
}

export async function testConnection(settings: AppSettings): Promise<string> {
  if (!isTauri()) {
    await delay(500);
    return settings.lmApiKey.trim() ? '连接成功' : '浏览器预览：请在 Tauri 中测试真实连接';
  }

  return invoke<string>('test_connection', { settings });
}

export async function deleteHistoryItem(id: string): Promise<HistoryItem[]> {
  if (!isTauri()) {
    return fallbackState.history.filter((item) => item.id !== id);
  }

  return invoke<HistoryItem[]>('delete_history_item', { id });
}
