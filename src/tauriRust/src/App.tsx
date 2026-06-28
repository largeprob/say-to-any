import { useEffect, useMemo, useRef, useState } from 'react';
import {
  Check,
  Clipboard,
  History,
  Home,
  Info,
  Keyboard,
  Languages,
  Mic,
  RefreshCw,
  Settings,
  User,
  X,
} from 'lucide-react';
import { Button, Card, Input, Label, Modal, TextArea, TextField } from '@heroui/react';
import { Outlet, useLocation, useNavigate } from 'react-router';
import { Icon } from './components/Icon';
import { RecordingDockControl } from './components/RecordingDockControl';
import { WindowControls, toggleWindowMaximize } from './components/WindowControls';
import { WaveBars } from './components/WaveBars';
import {
  cancelRecording,
  copyText,
  deleteHistoryItem,
  initializeAuthSession,
  listMicrophones,
  loadAppState,
  logout,
  pasteText,
  refreshAuthSession,
  saveSettings,
  startRecording,
  stopAndProcess,
  testConnection,
} from './lib/api';
import { useAuthStore } from './lib/auth-store';
import { defaultSettings } from './lib/defaults';
import {
  hideRecordingDockWindow,
  listenRecordingDockRequests,
  prepareRecordingDockWindow,
  showRecordingDockWindow,
} from './lib/recording-dock-window';
import type { HistoryGroup } from './routes/HistoryRoute';
import type { AppRouteContext } from './routes/route-context';
import type {
  AppSettings,
  AudioDeviceInfo,
  HistoryItem,
  MainSection,
  PlatformStatus,
  PreferencesSection,
} from './lib/types';

const historyRetentionOptions = [
  { value: 'Never', label: '从不' },
  { value: '24Hours', label: '24小时' },
  { value: 'OneWeek', label: '一周' },
  { value: 'OneMonth', label: '一个月' },
  { value: 'Forever', label: '永远' },
];

const appLanguages = ['简体中文', 'English'];

const mainSectionRoutes: Record<MainSection, string> = {
  home: '/home',
  history: '/history',
};

function getMainSectionFromPathname(pathname: string): MainSection {
  return pathname.startsWith('/history') ? 'history' : 'home';
}

function App() {
  const location = useLocation();
  const navigate = useNavigate();
  const authStatus = useAuthStore((state) => state.status);
  const authSession = useAuthStore((state) => state.session);
  const selectedSection = getMainSectionFromPathname(location.pathname);
  const [preferencesSection, setPreferencesSection] = useState<PreferencesSection>('settings');
  const [microphoneOpen, setMicrophoneOpen] = useState(false);
  const [settings, setSettings] = useState<AppSettings>(defaultSettings);
  const [history, setHistory] = useState<HistoryItem[]>([]);
  const [microphones, setMicrophones] = useState<AudioDeviceInfo[]>([]);
  const [platform, setPlatform] = useState<PlatformStatus | null>(null);
  const [appDataPath, setAppDataPath] = useState('');
  const [statusMessage, setStatusMessage] = useState('待机');
  const [rawTranscript, setRawTranscript] = useState('');
  const [finalText, setFinalText] = useState('');
  const [dictationResultText, setDictationResultText] = useState('');
  const [isRecording, setIsRecording] = useState(false);
  const [useInlineRecordingDock, setUseInlineRecordingDock] = useState(false);
  const [isBusy, setIsBusy] = useState(false);
  const [processingProgress, setProcessingProgress] = useState(0);
  const [waveBars, setWaveBars] = useState([10, 10, 10, 10, 10]);
  const recordingDockHandlersRef = useRef({
    cancel: () => {},
    finish: () => {},
  });

  useEffect(() => {
    if (authStatus === 'idle') {
      void initializeAuthSession();
    }
  }, [authStatus]);

  useEffect(() => {
    if (authStatus === 'anonymous') {
      void navigate('/login', { replace: true });
    }
  }, [authStatus, navigate]);

  useEffect(() => {
    if (authStatus !== 'authenticated' || !authSession) {
      return;
    }

    const expiresAt = Date.parse(authSession.accessTokenExpiresAt);
    if (!Number.isFinite(expiresAt)) {
      void refreshAuthSession();
      return;
    }

    const refreshLeadTimeMs = 60_000;
    const refreshDelayMs = Math.max(0, expiresAt - Date.now() - refreshLeadTimeMs);
    const timer = window.setTimeout(() => {
      void refreshAuthSession();
    }, refreshDelayMs);

    return () => window.clearTimeout(timer);
  }, [authSession, authStatus]);

  useEffect(() => {
    let disposed = false;
    let cleanup = () => {};

    void listenRecordingDockRequests({
      onCancel: () => recordingDockHandlersRef.current.cancel(),
      onFinish: () => recordingDockHandlersRef.current.finish(),
    }).then((unlisten) => {
      if (disposed) {
        unlisten();
        return;
      }

      cleanup = unlisten;
    });

    return () => {
      disposed = true;
      cleanup();
      void hideRecordingDockWindow();
    };
  }, []);

  useEffect(() => {
    if (authStatus === 'authenticated') {
      void prepareRecordingDockWindow();
    } else {
      void hideRecordingDockWindow();
    }
  }, [authStatus]);

  const selectedMicrophone = useMemo(
    () =>
      microphones.find((device) => device.deviceNumber === settings.microphoneDeviceNumber) ??
      microphones[0],
    [microphones, settings.microphoneDeviceNumber],
  );

  const wordUsage = useMemo(() => {
    const count = history.reduce((total, item) => total + countWords(item.finalText), 0);
    return new Intl.NumberFormat('zh-CN').format(count);
  }, [history]);

  const historyGroups = useMemo(() => groupHistory(history), [history]);

  useEffect(() => {
    if (authStatus !== 'authenticated') {
      return;
    }

    void loadAppState()
      .then((state) => {
        setSettings(state.settings);
        setHistory(state.history);
        setMicrophones(state.microphones);
        setPlatform(state.platform);
        setAppDataPath(state.appDataPath);
        setStatusMessage('待机');
      })
      .catch((error: unknown) => setStatusMessage(formatError('加载失败', error)));
  }, [authStatus]);

  useEffect(() => {
    if (!isRecording) {
      setWaveBars([10, 10, 10, 10, 10]);
      return;
    }

    let frame = 0;
    const timer = window.setInterval(() => {
      frame += 1;
      setWaveBars(
        Array.from({ length: 5 }, (_, index) => {
          const motion = (Math.sin(frame * 0.7 + index * 1.2) + 1) / 2;
          return Math.round(10 + motion * 28);
        }),
      );
    }, 90);

    return () => window.clearInterval(timer);
  }, [isRecording]);

  useEffect(() => {
    if (!isBusy) {
      setProcessingProgress(0);
      return;
    }

    const timer = window.setInterval(() => {
      setProcessingProgress((value) => Math.min(92, value + Math.max(1.2, (92 - value) * 0.045)));
    }, 90);

    return () => window.clearInterval(timer);
  }, [isBusy]);

  function patchSettings<K extends keyof AppSettings>(key: K, value: AppSettings[K]) {
    setSettings((current) => ({
      ...current,
      [key]: value,
    }));
  }

  async function refreshMicrophones() {
    try {
      const devices = await listMicrophones();
      setMicrophones(devices);
      setStatusMessage(devices.length > 1 ? '麦克风列表已刷新' : '未发现麦克风');
    } catch (error) {
      setStatusMessage(formatError('刷新失败', error));
    }
  }

  async function startDictation() {
    if (isBusy || isRecording) {
      return;
    }

    try {
      setUseInlineRecordingDock(false);
      setDictationResultText('');
      setRawTranscript('');
      setFinalText('');
      setStatusMessage('录音中...');
      await startRecording(settings.microphoneDeviceNumber);
      const isExternalDockVisible = await showRecordingDockWindow();
      setUseInlineRecordingDock(!isExternalDockVisible);
      setIsRecording(true);
    } catch (error) {
      setStatusMessage(formatError('录音失败', error));
      setUseInlineRecordingDock(false);
      void hideRecordingDockWindow();
      setIsRecording(false);
    }
  }

  async function finishDictation() {
    if (isBusy || !isRecording) {
      return;
    }

    try {
      setIsBusy(true);
      setIsRecording(false);
      setUseInlineRecordingDock(false);
      void hideRecordingDockWindow();
      setProcessingProgress(18);
      setStatusMessage('语音识别中...');
      const result = await stopAndProcess(settings);
      setRawTranscript(result.rawText);
      setFinalText(result.finalText);
      setProcessingProgress(100);
      setHistory((current) =>
        result.historyItem.id ? [result.historyItem, ...current] : current,
      );

      if (settings.autoPasteAfterDictation && !result.pasted) {
        setDictationResultText(result.finalText);
        setStatusMessage('已转换，可复制');
      } else {
        setStatusMessage(result.pasted ? '已粘贴' : '完成');
      }
    } catch (error) {
      setStatusMessage(formatError('处理失败', error));
    } finally {
      window.setTimeout(() => {
        setIsBusy(false);
        setProcessingProgress(0);
      }, 180);
    }
  }

  async function cancelDictation() {
    if (!isRecording) {
      return;
    }

    try {
      setIsBusy(true);
      await cancelRecording();
      setStatusMessage('录音已取消');
    } catch (error) {
      setStatusMessage(formatError('取消失败', error));
    } finally {
      await hideRecordingDockWindow();
      setIsRecording(false);
      setUseInlineRecordingDock(false);
      setIsBusy(false);
    }
  }

  async function copyFinalText() {
    if (!finalText.trim()) {
      setStatusMessage('没有可复制的文本');
      return;
    }

    try {
      await copyText(finalText);
      setStatusMessage('已复制');
    } catch (error) {
      setStatusMessage(formatError('复制失败', error));
    }
  }

  async function pasteFinalText() {
    if (!finalText.trim()) {
      setStatusMessage('没有可粘贴的文本');
      return;
    }

    try {
      const result = await pasteText(finalText);
      setStatusMessage(result.pasted ? '已粘贴' : result.message);
    } catch (error) {
      setStatusMessage(formatError('粘贴失败', error));
    }
  }

  async function copyHistoryText(item: HistoryItem) {
    try {
      await copyText(item.finalText);
      setStatusMessage('已复制历史文本');
    } catch (error) {
      setStatusMessage(formatError('复制失败', error));
    }
  }

  async function removeHistoryItem(item: HistoryItem) {
    try {
      const nextHistory = await deleteHistoryItem(item.id);
      setHistory(nextHistory);
      setStatusMessage('记录已删除');
    } catch (error) {
      setStatusMessage(formatError('删除失败', error));
    }
  }

  async function saveCurrentSettings() {
    try {
      const state = await saveSettings(settings);
      setSettings(state.settings);
      setHistory(state.history);
      setMicrophones(state.microphones);
      setStatusMessage('设置已保存');
    } catch (error) {
      setStatusMessage(formatError('保存失败', error));
    }
  }

  async function testModelConnection() {
    try {
      setIsBusy(true);
      setStatusMessage('测试连接中...');
      const message = await testConnection(settings);
      setStatusMessage(message);
    } catch (error) {
      setStatusMessage(formatError('连接失败', error));
    } finally {
      setIsBusy(false);
    }
  }

  async function signOut() {
    await logout();
    void navigate('/login', { replace: true });
  }

  function navigateMainSection(section: MainSection) {
    const route = mainSectionRoutes[section];

    if (location.pathname === route) {
      return;
    }

    void navigate(route);
  }

  recordingDockHandlersRef.current = {
    cancel: () => void cancelDictation(),
    finish: () => void finishDictation(),
  };

  const routeContext: AppRouteContext = {
    cancelDictation,
    copyFinalText,
    copyHistoryText,
    finalText,
    finishDictation,
    groups: historyGroups,
    isBusy,
    isRecording,
    pasteFinalText,
    processingProgress,
    rawTranscript,
    removeHistoryItem,
    setFinalText,
    startDictation,
    statusMessage,
    waveBars,
  };

  if (authStatus !== 'authenticated') {
    return (
      <div className="app-root auth-loading">
        <div className="content-header-spacer">
          <div
            className="window-drag-region"
            data-tauri-drag-region
            onDoubleClick={() => void toggleWindowMaximize()}
          />
          <WindowControls />
        </div>
        <div className="auth-loading-panel">正在恢复登录状态...</div>
      </div>
    );
  }

  return (
    <div className="app-root">
      <aside className="sidebar">
        <div>
          <div className="brand-row">
            <img alt="Say To Any" className="brand-logo" src="/logo.png" />
            <div className="brand-name">Say To Any</div>
            <div className="plan-chip">Free</div>
          </div>

          <nav className="nav-list" aria-label="主菜单">
            <NavButton
              active={selectedSection === 'home'}
              icon={Home}
              label="首页"
              onPress={() => navigateMainSection('home')}
            />
            <NavButton
              active={selectedSection === 'history'}
              icon={History}
              label="历史记录"
              onPress={() => navigateMainSection('history')}
            />
          </nav>
        </div>

        <div className="sidebar-bottom">
          <Card className="usage-card" variant="default">
            <Card.Header className="gap-1">
              <Card.Title className="text-[13px] text-[#0e2246]">本地听写</Card.Title>
              <Card.Description className="text-[13px] font-semibold text-[#0e2246]">
                {wordUsage} 字
              </Card.Description>
            </Card.Header>
            <Card.Content className="gap-2">
              <div className="usage-track">
                <div className="usage-fill" />
              </div>
              <p className="sidebar-note">双击 Alt 即可开始或停止录音</p>
            </Card.Content>
            <Card.Footer>
              {isRecording ? (
                <Button
                  className="recording-button"
                  fullWidth
                  onPress={() => void finishDictation()}
                  variant="danger-soft"
                >
                  <span className="stop-dot">
                    <span />
                  </span>
                  <WaveBars bars={waveBars} />
                </Button>
              ) : (
                <Button
                  className="primary-button"
                  fullWidth
                  isDisabled={isBusy}
                  onPress={() => void startDictation()}
                >
                  开始听写
                </Button>
              )}
            </Card.Footer>
          </Card>

          <div className="sidebar-separator" />

          <div className="sidebar-actions">
            <PreferencesDialog
              appDataPath={appDataPath}
              initialSection="account"
              microphones={microphones}
              onLogout={signOut}
              onOpenMicrophones={() => setMicrophoneOpen(true)}
              onPatchSettings={patchSettings}
              onRefreshMicrophones={refreshMicrophones}
              onSaveSettings={saveCurrentSettings}
              onSectionChange={setPreferencesSection}
              onTestConnection={testModelConnection}
              platform={platform}
              selectedMicrophone={selectedMicrophone}
              section={preferencesSection}
              settings={settings}
              statusMessage={statusMessage}
              triggerIcon={User}
              triggerLabel="用户"
            />
            <PreferencesDialog
              appDataPath={appDataPath}
              initialSection="settings"
              microphones={microphones}
              onLogout={signOut}
              onOpenMicrophones={() => setMicrophoneOpen(true)}
              onPatchSettings={patchSettings}
              onRefreshMicrophones={refreshMicrophones}
              onSaveSettings={saveCurrentSettings}
              onSectionChange={setPreferencesSection}
              onTestConnection={testModelConnection}
              platform={platform}
              selectedMicrophone={selectedMicrophone}
              section={preferencesSection}
              settings={settings}
              statusMessage={statusMessage}
              triggerIcon={Settings}
              triggerLabel="设置"
            />
          </div>
        </div>
      </aside>

      <main className="content-pane">
        <div className="content-header-spacer">
          <div
            className="window-drag-region"
            data-tauri-drag-region
            onDoubleClick={() => void toggleWindowMaximize()}
          />
          <WindowControls />
        </div>
        <div className="content-body">
          <Outlet context={routeContext} />
        </div>
      </main>

      {isRecording && useInlineRecordingDock ? (
        <div className="recording-dock-layer" aria-live="polite">
          <RecordingDockControl
            bars={waveBars}
            isBusy={isBusy}
            onCancel={() => void cancelDictation()}
            onFinish={() => void finishDictation()}
          />
        </div>
      ) : null}

      {microphoneOpen ? (
        <MicrophoneDialog
          microphones={microphones}
          onClose={() => setMicrophoneOpen(false)}
          onRefresh={refreshMicrophones}
          onSelect={(deviceNumber) => patchSettings('microphoneDeviceNumber', deviceNumber)}
          selectedDeviceNumber={settings.microphoneDeviceNumber}
        />
      ) : null}

      {dictationResultText ? (
        <ResultOverlay
          onClose={() => setDictationResultText('')}
          onCopy={() => {
            void copyText(dictationResultText).then(() => {
              setDictationResultText('');
              setStatusMessage('已复制');
            });
          }}
          text={dictationResultText}
        />
      ) : null}
    </div>
  );
}

type NavButtonProps = {
  active: boolean;
  icon: typeof Home;
  label: string;
  onPress: () => void;
};

function menuButtonClassName(active: boolean, widthClassName: string) {
  return [
    'menu-button h-[34px] min-h-[34px] justify-start rounded-[10px] border-0 px-2.5 text-[13px] font-medium',
    widthClassName,
    active
      ? 'bg-[var(--primary)] font-[650] text-white hover:bg-[var(--primary)] hover:text-white data-[hovered=true]:bg-[var(--primary)] data-[hovered=true]:text-white'
      : 'bg-transparent text-[var(--muted)] hover:bg-[var(--surface-hover)] hover:text-[var(--primary-strong)] data-[hovered=true]:bg-[var(--surface-hover)] data-[hovered=true]:text-[var(--primary-strong)]',
  ].join(' ');
}

function mainNavButtonClassName(active: boolean) {
  return `main-nav-button ${active ? 'active' : ''}`;
}

function NavButton({ active, icon, label, onPress }: NavButtonProps) {
  return (
    <Button
      aria-current={active ? 'page' : undefined}
      className={mainNavButtonClassName(active)}
      fullWidth
      onPress={onPress}
      size="sm"
      variant="ghost"
    >
      <Icon icon={icon} size={16} />
      <span>{label}</span>
    </Button>
  );
}

function PreferenceMenuButton({ active, icon, label, onPress }: NavButtonProps) {
  return (
    <Button
      className={menuButtonClassName(active, 'w-40 min-w-40')}
      onPress={onPress}
      size="sm"
      variant="ghost"
    >
      <Icon icon={icon} size={16} />
      <span>{label}</span>
    </Button>
  );
}

type PreferencesDialogProps = {
  appDataPath: string;
  initialSection: PreferencesSection;
  microphones: AudioDeviceInfo[];
  onLogout: () => Promise<void>;
  onOpenMicrophones: () => void;
  onPatchSettings: <K extends keyof AppSettings>(key: K, value: AppSettings[K]) => void;
  onRefreshMicrophones: () => Promise<void>;
  onSaveSettings: () => Promise<void>;
  onSectionChange: (section: PreferencesSection) => void;
  onTestConnection: () => Promise<void>;
  platform: PlatformStatus | null;
  selectedMicrophone?: AudioDeviceInfo;
  section: PreferencesSection;
  settings: AppSettings;
  statusMessage: string;
  triggerIcon: typeof Settings;
  triggerLabel: string;
};

function PreferencesDialog({
  appDataPath,
  initialSection,
  microphones,
  onLogout,
  onOpenMicrophones,
  onPatchSettings,
  onRefreshMicrophones,
  onSaveSettings,
  onSectionChange,
  onTestConnection,
  platform,
  selectedMicrophone,
  section,
  settings,
  statusMessage,
  triggerIcon,
  triggerLabel,
}: PreferencesDialogProps) {
  return (
    <Modal>
      <Button
        aria-label={triggerLabel}
        className="icon-button"
        isIconOnly
        onPress={() => onSectionChange(initialSection)}
        variant="ghost"
      >
        <Icon icon={triggerIcon} />
      </Button>
      <Modal.Backdrop variant="blur">
        <Modal.Container scroll="inside" size="cover">
          <Modal.Dialog
            aria-label="偏好设置"
            className="bg-[var(--background)] shadow-[0_24px_70px_rgba(15,31,58,0.22)]"
          >
            <Modal.Header>
              <Modal.Heading>
                <div className='h-5'></div>
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body className="grid grid-cols-1 gap-5 text-[var(--text)] sm:grid-cols-[188px_minmax(0,1fr)]">
              <aside className="sticky top-0 flex flex-col items-start gap-1 self-start bg-[var(--background)]">
                <PreferenceMenuButton
                  active={section === 'account'}
                  icon={User}
                  label="账户"
                  onPress={() => onSectionChange('account')}
                />
                <PreferenceMenuButton
                  active={section === 'settings'}
                  icon={Settings}
                  label="设置"
                  onPress={() => onSectionChange('settings')}
                />
                <PreferenceMenuButton
                  active={section === 'about'}
                  icon={Info}
                  label="关于"
                  onPress={() => onSectionChange('about')}
                />
              </aside>

              <div className="min-h-full min-w-0 rounded-t-2xl bg-white px-6 pb-6 pt-5">
                {section === 'account' ? (
                  <AccountSettings onLogout={onLogout} />
                ) : section === 'settings' ? (
                  <SettingsContent
                    appDataPath={appDataPath}
                    microphones={microphones}
                    onOpenMicrophones={onOpenMicrophones}
                    onPatchSettings={onPatchSettings}
                    onRefreshMicrophones={onRefreshMicrophones}
                    onSaveSettings={onSaveSettings}
                    onTestConnection={onTestConnection}
                    platform={platform}
                    selectedMicrophone={selectedMicrophone}
                    settings={settings}
                    statusMessage={statusMessage}
                  />
                ) : (
                  <AboutContent platform={platform} />
                )}
              </div>
            </Modal.Body>
            <Modal.CloseTrigger />
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

function AccountSettings({ onLogout }: { onLogout: () => Promise<void> }) {
  const session = useAuthStore((state) => state.session);

  return (
    <section className="grid gap-[18px]">
      <div>
        <h2 className="m-0 text-2xl font-bold leading-tight text-[var(--text)]">账户</h2>
      </div>
      <Card className="settings-card">
        <Card.Header>
          <Card.Title>{session?.userName ?? 'Say To Any 用户'}</Card.Title>
          <Card.Description>当前账户会在桌面端保持长期登录。</Card.Description>
        </Card.Header>
        <Card.Content>
          <p className="muted-line">用户 ID：{session?.userId ?? '-'}</p>
        </Card.Content>
        <Card.Footer>
          <Button onPress={() => void onLogout()} variant="secondary">
            退出登录
          </Button>
        </Card.Footer>
      </Card>
    </section>
  );
}

type SettingsContentProps = {
  appDataPath: string;
  microphones: AudioDeviceInfo[];
  onOpenMicrophones: () => void;
  onPatchSettings: <K extends keyof AppSettings>(key: K, value: AppSettings[K]) => void;
  onRefreshMicrophones: () => Promise<void>;
  onSaveSettings: () => Promise<void>;
  onTestConnection: () => Promise<void>;
  platform: PlatformStatus | null;
  selectedMicrophone?: AudioDeviceInfo;
  settings: AppSettings;
  statusMessage: string;
};

function SettingsContent({
  appDataPath,
  onOpenMicrophones,
  onPatchSettings,
  onRefreshMicrophones,
  onSaveSettings,
  onTestConnection,
  platform,
  selectedMicrophone,
  settings,
  statusMessage,
}: SettingsContentProps) {
  return (
    <section className="grid gap-[18px]">
      <div className="flex items-start justify-between gap-[18px]">
        <div>
          <h2 className="m-0 text-2xl font-bold leading-tight text-[var(--text)]">设置</h2>
          <p className="mt-2 text-sm leading-normal text-[var(--low)]">
            模型、快捷键、麦克风和界面语言。
          </p>
        </div>
        <Button className="primary-button" onPress={() => void onSaveSettings()}>
          保存设置
        </Button>
      </div>

      <div className="grid gap-3.5">
        <Card className="settings-card">
          <Card.Header>
            <Card.Title>模型配置</Card.Title>
          </Card.Header>
          <Card.Content className="settings-stack">
            <h3>LLM Model</h3>
            <SettingsField
              label="Base URL"
              onChange={(value) => onPatchSettings('lmBaseUrl', value)}
              value={settings.lmBaseUrl}
            />
            <SettingsField
              label="API Key"
              onChange={(value) => onPatchSettings('lmApiKey', value)}
              type="password"
              value={settings.lmApiKey}
            />
            <SettingsField
              label="Model"
              onChange={(value) => onPatchSettings('lmModel', value)}
              value={settings.lmModel}
            />
            <div className="setting-row">
              <span>温度</span>
              <div className="range-row">
                <input
                  max={2}
                  min={0}
                  onChange={(event) => onPatchSettings('lmTemperature', Number(event.target.value))}
                  step={0.1}
                  type="range"
                  value={settings.lmTemperature}
                />
                <strong>{settings.lmTemperature.toFixed(1)}</strong>
              </div>
            </div>

            <div className="soft-divider" />

            <h3>ASR Model</h3>
            <SettingsField
              label="Base URL"
              onChange={(value) => onPatchSettings('asrBaseUrl', value)}
              value={settings.asrBaseUrl}
            />
            <SettingsField
              label="API Key"
              onChange={(value) => onPatchSettings('asrApiKey', value)}
              type="password"
              value={settings.asrApiKey}
            />
            <SettingsField
              label="Model"
              onChange={(value) => onPatchSettings('asrModel', value)}
              value={settings.asrModel}
            />
            <SettingsField
              label="识别语言"
              onChange={(value) => onPatchSettings('language', value)}
              value={settings.language}
            />
            <ToggleRow
              checked={settings.asrEnableItn}
              label="ASR enable_itn"
              onChange={(value) => onPatchSettings('asrEnableItn', value)}
            />
          </Card.Content>
        </Card>

        <Card className="settings-card">
          <Card.Header>
            <Card.Title>录音快捷键</Card.Title>
          </Card.Header>
          <Card.Content>
            <ReadOnlyRow icon={Keyboard} label="启动录音" value="双击 Alt" />
          </Card.Content>
        </Card>

        <Card className="settings-card">
          <Card.Header>
            <Card.Title>翻译快捷键</Card.Title>
          </Card.Header>
          <Card.Content>
            <ReadOnlyRow icon={Languages} label="翻译文本" value="未设置" />
          </Card.Content>
        </Card>

        <Card className="settings-card">
          <Card.Header>
            <Card.Title>麦克风设备</Card.Title>
          </Card.Header>
          <Card.Content className="settings-stack">
            <div className="setting-row">
              <span>输入设备</span>
              <div className="microphone-picker">
                <Button className="device-button" onPress={onOpenMicrophones} variant="secondary">
                  <Icon icon={Mic} size={16} />
                  <span>{selectedMicrophone?.displayName ?? '未发现麦克风'}</span>
                </Button>
                <Button onPress={() => void onRefreshMicrophones()} variant="tertiary">
                  <Icon icon={RefreshCw} size={15} />
                  刷新
                </Button>
              </div>
            </div>
            <SettingsField
              label="超时秒数"
              onChange={(value) => onPatchSettings('timeoutSeconds', Number(value))}
              type="number"
              value={String(settings.timeoutSeconds)}
            />
            <SettingsField
              label="最长录音秒数"
              onChange={(value) => onPatchSettings('maxRecordingSeconds', Number(value))}
              type="number"
              value={String(settings.maxRecordingSeconds)}
            />
            <ToggleRow
              checked={settings.enableTextCleanup}
              label="启用文本整理"
              onChange={(value) => onPatchSettings('enableTextCleanup', value)}
            />
            <ToggleRow
              checked={settings.autoPasteAfterDictation}
              label="识别后自动粘贴"
              onChange={(value) => onPatchSettings('autoPasteAfterDictation', value)}
            />
            <div className="setting-row">
              <span>历史保存</span>
              <select
                className="select-shell"
                onChange={(event) => onPatchSettings('historyRetention', event.target.value)}
                value={settings.historyRetention}
              >
                {historyRetentionOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            <Button className="w-fit" onPress={() => void onTestConnection()} variant="secondary">
              测试连接
            </Button>
            <p className="muted-line">{statusMessage}</p>
          </Card.Content>
        </Card>

        <Card className="settings-card">
          <Card.Header>
            <Card.Title>程序页面语言</Card.Title>
          </Card.Header>
          <Card.Content>
            <div className="setting-row">
              <span>界面语言</span>
              <select
                className="select-shell"
                onChange={(event) => onPatchSettings('appLanguage', event.target.value)}
                value={settings.appLanguage}
              >
                {appLanguages.map((language) => (
                  <option key={language} value={language}>
                    {language}
                  </option>
                ))}
              </select>
            </div>
          </Card.Content>
        </Card>

        <Card className="settings-card">
          <Card.Header>
            <Card.Title>平台兼容</Card.Title>
            <Card.Description>{platform?.os ?? 'unknown'}</Card.Description>
          </Card.Header>
          <Card.Content className="compat-list">
            <CompatItem enabled={platform?.supportsRecording ?? false} label="录音并转为文字" />
            <CompatItem enabled={platform?.supportsAutoPaste ?? false} label="自动粘贴" />
            <CompatItem enabled={platform?.supportsGlobalHotkey ?? false} label="全局快捷键" />
            {appDataPath ? <p className="muted-line">配置文件：{appDataPath}</p> : null}
          </Card.Content>
        </Card>
      </div>
    </section>
  );
}

function AboutContent({ platform }: { platform: PlatformStatus | null }) {
  return (
    <section className="grid gap-[18px]">
      <div>
        <h2 className="m-0 text-2xl font-bold leading-tight text-[var(--text)]">关于</h2>
        <p className="mt-2 text-sm leading-normal text-[var(--low)]">Say To Any</p>
      </div>
      <Card className="settings-card">
        <Card.Header>
          <img alt="Say To Any" className="about-logo" src="/logo.png" />
          <Card.Title>Say To Any</Card.Title>
          <Card.Description>Tauri + React + Tailwind CSS + HeroUI</Card.Description>
        </Card.Header>
        <Card.Content>
          <p className="muted-line">当前平台：{platform?.os ?? 'unknown'}</p>
        </Card.Content>
      </Card>
    </section>
  );
}

type SettingsFieldProps = {
  label: string;
  onChange: (value: string) => void;
  type?: string;
  value: string;
};

function SettingsField({ label, onChange, type = 'text', value }: SettingsFieldProps) {
  return (
    <div className="setting-row">
      <span>{label}</span>
      <TextField className="field-shell" onChange={onChange} type={type} value={value}>
        <Label className="sr-only">{label}</Label>
        <Input className="input-shell" />
      </TextField>
    </div>
  );
}

function ToggleRow({
  checked,
  label,
  onChange,
}: {
  checked: boolean;
  label: string;
  onChange: (value: boolean) => void;
}) {
  return (
    <label className="toggle-row">
      <span>{label}</span>
      <input checked={checked} onChange={(event) => onChange(event.target.checked)} type="checkbox" />
      <i />
    </label>
  );
}

function ReadOnlyRow({
  icon,
  label,
  value,
}: {
  icon: typeof Keyboard;
  label: string;
  value: string;
}) {
  return (
    <div className="read-only-row">
      <span>
        <Icon icon={icon} size={16} />
        {label}
      </span>
      <strong>{value}</strong>
    </div>
  );
}

function CompatItem({ enabled, label }: { enabled: boolean; label: string }) {
  return (
    <div className={`compat-item ${enabled ? 'enabled' : ''}`}>
      <Icon icon={enabled ? Check : X} size={15} />
      <span>{label}</span>
    </div>
  );
}

type MicrophoneDialogProps = {
  microphones: AudioDeviceInfo[];
  onClose: () => void;
  onRefresh: () => Promise<void>;
  onSelect: (deviceNumber: number) => void;
  selectedDeviceNumber: number;
};

function MicrophoneDialog({
  microphones,
  onClose,
  onRefresh,
  onSelect,
  selectedDeviceNumber,
}: MicrophoneDialogProps) {
  return (
    <div className="modal-layer top-layer">
      <Card className="microphone-dialog">
        <Card.Header className="modal-card-header">
          <div>
            <Card.Title>麦克风</Card.Title>
            <Card.Description>
              选择能捕捉到您声音的麦克风。如果指示条没有移动，请尝试其他麦克风。
            </Card.Description>
          </div>
          <Button aria-label="关闭" className="icon-button" isIconOnly onPress={onClose} variant="ghost">
            <Icon icon={X} size={18} />
          </Button>
        </Card.Header>
        <Card.Content className="microphone-list">
          {microphones.map((device, index) => (
            <Button
              className={`microphone-option ${device.deviceNumber === selectedDeviceNumber ? 'selected' : ''
                }`}
              fullWidth
              key={`${device.deviceNumber}-${device.name}`}
              onPress={() => {
                onSelect(device.deviceNumber);
                onClose();
              }}
              variant="ghost"
            >
              <span className="selection-mark">
                {device.deviceNumber === selectedDeviceNumber ? <Icon icon={Check} size={13} /> : null}
              </span>
              <span className="device-copy">
                <strong>{device.displayName}</strong>
                <small>{device.description}</small>
              </span>
              <FakeMeter seed={index} />
            </Button>
          ))}
        </Card.Content>
        <Card.Footer>
          <Button onPress={() => void onRefresh()} variant="secondary">
            <Icon icon={RefreshCw} size={15} />
            刷新
          </Button>
        </Card.Footer>
      </Card>
    </div>
  );
}

function ResultOverlay({
  onClose,
  onCopy,
  text,
}: {
  onClose: () => void;
  onCopy: () => void;
  text: string;
}) {
  return (
    <Card className="result-overlay">
      <Card.Header className="modal-card-header">
        <Card.Title>已转换，可复制</Card.Title>
        <Button aria-label="关闭" className="icon-button" isIconOnly onPress={onClose} variant="ghost">
          <Icon icon={X} size={16} />
        </Button>
      </Card.Header>
      <Card.Content>
        <TextField value={text}>
          <Label className="sr-only">转换结果</Label>
          <TextArea className="result-textarea" readOnly rows={5} />
        </TextField>
      </Card.Content>
      <Card.Footer>
        <Button className="primary-button" fullWidth onPress={onCopy}>
          <Icon icon={Clipboard} size={16} />
          复制
        </Button>
      </Card.Footer>
    </Card>
  );
}

function FakeMeter({ seed }: { seed: number }) {
  const heights = [14, 20, 28, 22, 30, 16];
  return (
    <span className="fake-meter">
      {heights.map((height, index) => (
        <i
          key={index}
          style={{
            height,
            opacity: 0.35 + (((index + seed) % 4) + 1) * 0.14,
          }}
        />
      ))}
    </span>
  );
}

function groupHistory(items: HistoryItem[]): HistoryGroup[] {
  const today = new Date();
  const yesterday = new Date();
  yesterday.setDate(today.getDate() - 1);

  const groups: HistoryGroup[] = [
    { title: '今天的录音', items: [] },
    { title: '昨天的录音', items: [] },
    { title: '最近的录音', items: [] },
  ];

  for (const item of items) {
    const created = new Date(item.createdAt);
    if (isSameDate(created, today)) {
      groups[0].items.push(item);
    } else if (isSameDate(created, yesterday)) {
      groups[1].items.push(item);
    } else {
      groups[2].items.push(item);
    }
  }

  return groups.filter((group) => group.items.length > 0);
}

function isSameDate(left: Date, right: Date) {
  return (
    left.getFullYear() === right.getFullYear() &&
    left.getMonth() === right.getMonth() &&
    left.getDate() === right.getDate()
  );
}

function countWords(text: string) {
  return text ? Array.from(text).filter((character) => !/\s/.test(character)).length : 0;
}

function formatError(prefix: string, error: unknown) {
  const message = error instanceof Error ? error.message : String(error);
  return `${prefix}：${message}`;
}

export default App;
