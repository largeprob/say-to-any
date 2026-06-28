import { useEffect, useState, type FormEvent } from 'react';
import {
  Apple,
  Bot,
  EyeOff,
  LockKeyhole,
  Mic,
  ShieldCheck,
  SquareCheckBig,
  Terminal,
  User,
  Zap,
} from 'lucide-react';
import { useNavigate } from 'react-router';
import { Icon } from '../components/Icon';
import { WindowControls } from '../components/WindowControls';
import {
  initializeAuthSession,
  loginWithPassword,
  registerWithEmail,
  sendRegisterEmailCode,
} from '../lib/api';
import { useAuthStore } from '../lib/auth-store';

const voiceBars = [18, 28, 24, 36, 46, 30, 54, 40, 26, 34, 22, 28, 18];
type AuthMode = 'login' | 'register';
type LoginMessage = { type: 'error' | 'success'; text: string } | null;

export function LoginRoute() {
  const navigate = useNavigate();
  const authStatus = useAuthStore((state) => state.status);
  const [mode, setMode] = useState<AuthMode>('login');
  const [loginAccount, setLoginAccount] = useState('');
  const [loginPassword, setLoginPassword] = useState('');
  const [registerEmail, setRegisterEmail] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [registerPassword, setRegisterPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isSendingCode, setIsSendingCode] = useState(false);
  const [codeCountdown, setCodeCountdown] = useState(0);
  const [message, setMessage] = useState<LoginMessage>(null);

  useEffect(() => {
    if (authStatus === 'idle') {
      void initializeAuthSession();
    }

    if (authStatus === 'authenticated') {
      void navigate('/home', { replace: true });
    }
  }, [authStatus, navigate]);

  useEffect(() => {
    if (codeCountdown <= 0) {
      return;
    }

    const timer = window.setInterval(() => {
      setCodeCountdown((value) => Math.max(0, value - 1));
    }, 1000);

    return () => window.clearInterval(timer);
  }, [codeCountdown]);

  function switchMode(nextMode: AuthMode) {
    setMode(nextMode);
    setMessage(null);
  }

  async function submitLogin(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);

    if (!loginAccount.trim() || !loginPassword) {
      setMessage({ type: 'error', text: '请输入邮箱账号和密码' });
      return;
    }

    try {
      setIsSubmitting(true);
      await loginWithPassword(loginAccount.trim(), loginPassword);
      void navigate('/home', { replace: true });
    } catch (error) {
      setMessage({ type: 'error', text: getErrorMessage(error) });
    } finally {
      setIsSubmitting(false);
    }
  }

  async function sendCode() {
    setMessage(null);
    if (!registerEmail.trim()) {
      setMessage({ type: 'error', text: '请输入邮箱账号' });
      return;
    }

    try {
      setIsSendingCode(true);
      await sendRegisterEmailCode(registerEmail.trim());
      setCodeCountdown(60);
      setMessage({ type: 'success', text: '验证码已发送，请查看邮箱' });
    } catch (error) {
      setMessage({ type: 'error', text: getErrorMessage(error) });
    } finally {
      setIsSendingCode(false);
    }
  }

  async function submitRegister(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setMessage(null);

    if (!registerEmail.trim() || !verificationCode.trim() || !registerPassword) {
      setMessage({ type: 'error', text: '请输入邮箱、验证码和密码' });
      return;
    }

    if (registerPassword.length < 8) {
      setMessage({ type: 'error', text: '密码长度至少为8位' });
      return;
    }

    try {
      setIsSubmitting(true);
      await registerWithEmail(registerEmail.trim(), verificationCode.trim(), registerPassword);
      void navigate('/home', { replace: true });
    } catch (error) {
      setMessage({ type: 'error', text: getErrorMessage(error) });
    } finally {
      setIsSubmitting(false);
    }
  }

  const isLoginMode = mode === 'login';

  return (
    <main className="login-page">
      <div className="login-frame">
        <div className="login-titlebar" data-tauri-drag-region>
          <WindowControls />
        </div>

        <div className="login-aura login-aura-left" />
        <div className="login-aura login-aura-right" />
        <div className="login-diagonal-glow" />
        <div className="login-bottom-wave" />

        <div className="login-content">
          <section className="login-showcase" aria-label="Say To Any 产品介绍">
            <div className="login-brand">
              <LoginLogo />
              <div>
                <strong>Say To Any</strong>
                <span>语音驱动生产力</span>
              </div>
            </div>

            <div className="login-copy">
              <h1>
                说出想法，
                <span>AI</span>
                帮你完成
              </h1>
              <p>
                将语音转为文字，自动粘贴到任意位置
                <br />
                未来，AI Agent 将帮你操作应用，完成更多工作
              </p>
            </div>

            <VoiceHeroGraphic />

            <div className="login-features" aria-label="核心能力">
              <FeatureItem icon={Zap} title="高效语音输入" description="精准识别，快速转写" />
              <FeatureItem icon={Bot} title="AI Agent 赋能" description="未来将支持应用操作和自动化任务" />
              <FeatureItem icon={ShieldCheck} title="安全与隐私" description="数据加密存储保护你的信息安全" />
            </div>
          </section>

          <section className="login-panel" aria-label="登录表单">
            <div className="login-card">
              <div className="login-card-heading">
                <h2>{isLoginMode ? '欢迎回来' : '创建账户'}</h2>
                <p>{isLoginMode ? '登录你的 Say To Any 账户' : '使用邮箱验证码注册 Say To Any'}</p>
              </div>

              <div className="login-tabs" role="tablist" aria-label="登录方式">
                <button
                  aria-selected={isLoginMode}
                  className={isLoginMode ? 'active' : ''}
                  onClick={() => switchMode('login')}
                  role="tab"
                  type="button"
                >
                  账号登录
                </button>
                <button
                  aria-selected={!isLoginMode}
                  className={!isLoginMode ? 'active' : ''}
                  onClick={() => switchMode('register')}
                  role="tab"
                  type="button"
                >
                  注册账号
                </button>
              </div>

              {isLoginMode ? (
                <form className="login-form" aria-label="账号登录" onSubmit={(event) => void submitLogin(event)}>
                  <label className="login-field">
                    <Icon icon={User} size={21} />
                    <input
                      aria-label="邮箱账号"
                      autoComplete="username"
                      onChange={(event) => setLoginAccount(event.target.value)}
                      placeholder="邮箱账号"
                      type="email"
                      value={loginAccount}
                    />
                  </label>

                  <label className="login-field">
                    <Icon icon={LockKeyhole} size={20} />
                    <input
                      aria-label="密码"
                      autoComplete="current-password"
                      onChange={(event) => setLoginPassword(event.target.value)}
                      placeholder="密码"
                      type={showPassword ? 'text' : 'password'}
                      value={loginPassword}
                    />
                    <button
                      aria-label="显示或隐藏密码"
                      className="login-field-icon"
                      onClick={() => setShowPassword((value) => !value)}
                      type="button"
                    >
                      <Icon icon={EyeOff} size={20} />
                    </button>
                  </label>

                  <div className="login-options">
                    <label className="login-remember">
                      <input aria-label="记住我" type="checkbox" />
                      <span />
                      记住我
                    </label>
                    <button type="button">忘记密码?</button>
                  </div>

                  {message ? <p className={`login-message ${message.type}`}>{message.text}</p> : null}

                  <button className="login-submit" disabled={isSubmitting || authStatus === 'checking'} type="submit">
                    {authStatus === 'checking' ? '正在恢复登录...' : isSubmitting ? '登录中...' : '登录'}
                  </button>
                </form>
              ) : (
                <form className="login-form" aria-label="注册账号" onSubmit={(event) => void submitRegister(event)}>
                  <label className="login-field">
                    <Icon icon={User} size={21} />
                    <input
                      aria-label="邮箱账号"
                      autoComplete="email"
                      onChange={(event) => setRegisterEmail(event.target.value)}
                      placeholder="邮箱账号"
                      type="email"
                      value={registerEmail}
                    />
                  </label>

                  <label className="login-field verification-field">
                    <Icon icon={ShieldCheck} size={20} />
                    <input
                      aria-label="邮箱验证码"
                      autoComplete="one-time-code"
                      inputMode="numeric"
                      maxLength={6}
                      onChange={(event) => setVerificationCode(event.target.value)}
                      placeholder="邮箱验证码"
                      value={verificationCode}
                    />
                    <button
                      className="send-code-button"
                      disabled={isSendingCode || codeCountdown > 0}
                      onClick={() => void sendCode()}
                      type="button"
                    >
                      {codeCountdown > 0 ? `${codeCountdown}s` : isSendingCode ? '发送中' : '发送验证码'}
                    </button>
                  </label>

                  <label className="login-field">
                    <Icon icon={LockKeyhole} size={20} />
                    <input
                      aria-label="设置密码"
                      autoComplete="new-password"
                      onChange={(event) => setRegisterPassword(event.target.value)}
                      placeholder="设置密码，至少8位"
                      type={showPassword ? 'text' : 'password'}
                      value={registerPassword}
                    />
                    <button
                      aria-label="显示或隐藏密码"
                      className="login-field-icon"
                      onClick={() => setShowPassword((value) => !value)}
                      type="button"
                    >
                      <Icon icon={EyeOff} size={20} />
                    </button>
                  </label>

                  {message ? <p className={`login-message ${message.type}`}>{message.text}</p> : null}

                  <button className="login-submit" disabled={isSubmitting || authStatus === 'checking'} type="submit">
                    {authStatus === 'checking' ? '正在恢复登录...' : isSubmitting ? '注册中...' : '注册并登录'}
                  </button>
                </form>
              )}

              {isLoginMode ? (
                <>
                  <div className="login-divider">
                    <span />
                    <em>其他登录方式</em>
                    <span />
                  </div>

                  <div className="login-socials" aria-label="第三方登录">
                    <button aria-label="Google 登录" type="button">
                      <span className="google-mark">G</span>
                    </button>
                    <button aria-label="Microsoft 登录" type="button">
                      <span className="microsoft-mark">
                        <i />
                        <i />
                        <i />
                        <i />
                      </span>
                    </button>
                    <button aria-label="Apple 登录" type="button">
                      <Icon icon={Apple} size={29} />
                    </button>
                  </div>
                </>
              ) : null}

              <p className="login-register">
                {isLoginMode ? '还没有账户?' : '已有账户?'}
                <button onClick={() => switchMode(isLoginMode ? 'register' : 'login')} type="button">
                  {isLoginMode ? '立即注册' : '返回登录'}
                </button>
              </p>
            </div>

            <p className="login-legal">
              <Icon icon={ShieldCheck} size={17} />
              登录即表示你同意
              <button type="button">《用户协议》</button>
              和
              <button type="button">《隐私政策》</button>
            </p>
          </section>
        </div>
      </div>
    </main>
  );
}

function getErrorMessage(error: unknown) {
  return error instanceof Error ? error.message : String(error);
}

function LoginLogo() {
  return (
    <span className="login-logo" aria-hidden="true">
      <Icon icon={Mic} size={39} />
    </span>
  );
}

function VoiceHeroGraphic() {
  return (
    <div className="voice-graphic" aria-hidden="true">
      <div className="voice-floor" />
      <div className="voice-orbit orbit-one" />
      <div className="voice-orbit orbit-two" />
      <div className="voice-orbit orbit-three" />

      <div className="voice-floating-card waveform-card">
        <span className="waveform">
          {voiceBars.map((height, index) => (
            <i key={index} style={{ height }} />
          ))}
        </span>
      </div>
      <div className="voice-floating-card terminal-card">
        <Icon icon={Terminal} size={35} />
      </div>
      <div className="voice-floating-card ai-card">A</div>
      <div className="voice-floating-card task-card">
        <Icon icon={SquareCheckBig} size={34} />
      </div>

      <span className="sparkle sparkle-one" />
      <span className="sparkle sparkle-two" />
      <span className="sparkle sparkle-three" />

      <div className="voice-core-halo">
        <div className="voice-core">
          <Icon icon={Mic} size={74} />
        </div>
      </div>
    </div>
  );
}

function FeatureItem({
  description,
  icon,
  title,
}: {
  description: string;
  icon: typeof Zap;
  title: string;
}) {
  return (
    <article className="login-feature">
      <Icon icon={icon} size={26} />
      <div>
        <strong>{title}</strong>
        <p>{description}</p>
      </div>
    </article>
  );
}
