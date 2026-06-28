import { useEffect, useMemo, useState, type ReactNode } from "react";
import { SiteFooter, SiteHeader } from "../components/site";
import type { Route } from "./+types/home";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "Say-To-Any" },
    {
      name: "description",
      content: "说完即输入的语音转文字工具。",
    },
  ];
}

export default function Home() {
  return (
    <main className="min-h-svh overflow-x-hidden bg-[#fbfaf8] pt-[76px] text-[#06080d]">
      <SiteHeader />
      <Hero />
      <HowItWorks />
      <Benefits />
      <Future />
      <SiteFooter />
    </main>
  );
}

function Hero() {
  return (
    <section id="home" className="mx-auto grid w-full max-w-[1120px] gap-12 px-6 pb-24 pt-16 md:grid-cols-[0.82fr_1fr] md:items-center md:pb-28 md:pt-24">
      <div className="hero-copy">
        <h1 className="text-[56px] font-semibold leading-[1.18] text-[#06080d] sm:text-[72px]">
          说完，
          <br />
          即输入。
        </h1>
        <p className="mt-8 max-w-[330px] text-lg leading-8 text-[#5e626b]">
          按下快捷键，说出你的想法，Say-To-Any 会自动转成文字并粘贴到当前光标位置。
        </p>
        <div className="mt-8 flex flex-wrap items-center gap-5">
          <a
            id="download"
            href="/download"
            className="inline-flex h-[52px] items-center justify-center rounded-[14px] bg-[#06080d] px-8 text-base font-semibold text-white shadow-[0_16px_36px_rgba(6,8,13,0.18)] transition hover:-translate-y-0.5"
          >
            免费下载
          </a>
          <a
            href="#how"
            className="inline-flex h-[52px] items-center justify-center rounded-[14px] border border-[#dfded9] bg-white/70 px-8 text-base font-semibold text-[#06080d] shadow-[0_12px_28px_rgba(20,24,32,0.04)] transition hover:-translate-y-0.5"
          >
            了解更多
          </a>
        </div>
        <p className="mt-9 text-sm text-[#777b82]">支持 macOS 10.14+ / Windows 10+</p>
      </div>

      <ProductPreview />
    </section>
  );
}

function ProductPreview() {
  return (
    <div className="hero-panel relative mx-auto w-full max-w-full rounded-[24px] border border-[#dfded9] bg-white/58 p-11 shadow-[0_26px_80px_rgba(22,26,34,0.08)] backdrop-blur sm:max-w-[560px]">
      <div className="text-center text-sm text-[#70747b]">
        按下 <kbd className="mx-1 rounded-md bg-white px-2 py-1 font-semibold text-[#06080d] shadow-[0_2px_8px_rgba(20,24,32,0.08)]">⌘ 空格</kbd> 开始说话
      </div>
      <div className="mx-auto mt-6 grid h-36 w-full max-w-[420px] place-items-center rounded-[18px] border border-[#ecebe7] bg-white/70 shadow-[0_14px_40px_rgba(20,24,32,0.04)]">
        <ListeningWaveform variant="dark" barCount={29} />
        <p className="mt-2 text-center text-base text-[#8a8e95]">正在监听...</p>
      </div>
      <p className="mt-10 max-w-[360px] text-[22px] leading-9 text-[#06080d]">
        设计的本质是解决问题，
        <br />
        而不是追求形式的完美。
        <span className="ml-2 inline-block h-6 w-px translate-y-1 bg-[#06080d]" />
      </p>
    </div>
  );
}

function HowItWorks() {
  const steps = [
    {
      number: "1",
      title: "按下快捷键",
      text: "随时唤醒 Say-To-Any",
    },
    {
      number: "2",
      title: "开始说话",
      text: "自然表达，无需学习指令",
      wave: true,
    },
    {
      number: "3",
      title: "自动输入",
      text: "识别完成后自动粘贴到当前光标位置",
    },
  ];

  return (
    <section id="how" className="mx-auto w-full max-w-[1120px] px-6 pb-20">
      <div className="flex items-end justify-between gap-6">
        <h2 className="text-[36px] font-semibold leading-tight text-[#06080d]">工作方式</h2>
        <p className="hidden text-base text-[#777b82] md:block">简单三步，立即开始</p>
      </div>

      <div className="mt-12 grid gap-8 md:grid-cols-[1fr_auto_1fr_auto_1fr] md:items-start">
        {steps.map((step, index) => (
          <div key={step.number} className="contents">
            <article className="min-h-40">
              <div className="flex items-center gap-4">
                <span className="grid size-7 place-items-center rounded-full bg-[#06080d] text-sm font-semibold text-white">
                  {step.number}
                </span>
                <h3 className="text-lg font-semibold text-[#06080d]">{step.title}</h3>
              </div>
              <p className="ml-11 mt-4 max-w-[190px] text-sm leading-6 text-[#73777f]">{step.text}</p>
              {step.wave ? (
                <div className="ml-4 mt-20">
                  <ListeningWaveform variant="muted" barCount={25} small />
                </div>
              ) : null}
              {index === 2 ? (
                <div className="ml-11 mt-10 w-full max-w-[250px] rounded-[12px] border border-[#e2e1dc] bg-white/76 px-7 py-5 text-center text-base text-[#06080d] shadow-[0_18px_46px_rgba(20,24,32,0.06)]">
                  说完即输入，效率倍增
                </div>
              ) : null}
            </article>
            {index < steps.length - 1 ? (
              <div className="hidden pt-20 text-3xl text-[#06080d] md:block">→</div>
            ) : null}
          </div>
        ))}
      </div>
    </section>
  );
}

function Benefits() {
  const benefits = [
    { title: "全局快捷键", text: "在任何应用中\n随时唤醒", icon: <KeyboardIcon /> },
    { title: "自动粘贴", text: "文字自动输入到\n光标位置", icon: <ClipboardIcon /> },
    { title: "高效准确", text: "快速识别，\n更高准确率", icon: <BoltIcon /> },
    { title: "隐私安全", text: "本地处理，\n保护你的数据", icon: <LockIcon /> },
    { title: "极简设计", text: "轻量优雅，\n专注体验", icon: <CircleIcon /> },
  ];

  return (
    <section className="mx-auto w-full max-w-[1120px] border-t border-[#e0ded8] px-6 py-12">
      <div className="grid gap-8 md:grid-cols-[1fr_310px]">
        <h2 className="text-[36px] font-semibold leading-tight text-[#06080d]">为高效而生</h2>
        <p className="text-base leading-7 text-[#777b82]">
          专注于语音转文字，帮助你在任何场景下更快地表达与记录。
        </p>
      </div>
      <div className="mt-12 grid grid-cols-2 gap-8 sm:grid-cols-3 lg:grid-cols-5">
        {benefits.map((item) => (
          <article key={item.title} className="min-h-36">
            <div className="mb-6 text-[#06080d]">{item.icon}</div>
            <h3 className="text-base font-semibold text-[#06080d]">{item.title}</h3>
            <p className="mt-3 whitespace-pre-line text-sm leading-7 text-[#777b82]">{item.text}</p>
          </article>
        ))}
      </div>
    </section>
  );
}

function Future() {
  return (
    <section id="about" className="mx-auto w-full max-w-[1120px] px-6 py-8">
      <div className="grid min-h-[300px] gap-10 rounded-[18px] border border-[#dfded9] bg-white/56 p-10 shadow-[0_24px_70px_rgba(20,24,32,0.06)] md:grid-cols-[0.8fr_1.2fr] md:items-center md:p-12">
        <div>
          <h2 className="text-[30px] font-semibold leading-tight text-[#06080d]">未来：从输入到执行</h2>
          <p className="mt-6 max-w-[360px] text-base leading-8 text-[#6d7179]">
            我们正在构建智能体能力，让 Say-To-Any 不仅能帮你输入，还能理解你的意图，主动打开应用，完成任务。
          </p>
          <a
            href="#"
            className="mt-9 inline-flex h-11 items-center rounded-[12px] border border-[#dfded9] bg-white/72 px-6 text-sm font-semibold text-[#06080d] shadow-[0_12px_28px_rgba(20,24,32,0.04)] transition hover:-translate-y-0.5"
          >
            了解未来规划
          </a>
        </div>
        <div className="relative grid min-h-[210px] place-items-center">
          <span className="absolute size-40 rounded-full border border-[#ecebe7]" />
          <span className="absolute size-28 rounded-full bg-[#f3f2ee]" />
          <span className="relative grid size-20 place-items-center rounded-full bg-[#e9e8e4] text-[#06080d]">
            <BrandMark className="size-8" />
          </span>
          <FloatingAction className="left-[8%] top-[12%]">打开微信</FloatingAction>
          <FloatingAction className="bottom-[18%] left-[8%]">创建任务</FloatingAction>
          <FloatingAction className="right-[4%] top-[20%]">发送会议纪要</FloatingAction>
          <FloatingAction className="bottom-[18%] right-[2%]">整理这段录音</FloatingAction>
        </div>
      </div>
    </section>
  );
}

function ListeningWaveform({
  variant,
  barCount,
  small = false,
}: {
  variant: "dark" | "muted";
  barCount: number;
  small?: boolean;
}) {
  const baseHeights = useMemo(
    () => Array.from({ length: barCount }, (_, index) => 10 + ((index * 17) % 34)),
    [barCount],
  );
  const [heights, setHeights] = useState(baseHeights);

  useEffect(() => {
    const timer = window.setInterval(() => {
      setHeights((current) =>
        current.map((height, index) => {
          const next = 8 + Math.random() * (small ? 28 : 46);
          const edgeFade = Math.abs(index - (barCount - 1) / 2) / (barCount / 2);
          return Math.max(6, next * (1 - edgeFade * 0.45) + height * 0.18);
        }),
      );
    }, 180);

    return () => window.clearInterval(timer);
  }, [barCount, small]);

  const color = variant === "dark" ? "bg-[#06080d]" : "bg-[#b8bcc3]";

  return (
    <div className={`flex items-center justify-center gap-1 ${small ? "h-10" : "h-12"}`} aria-hidden="true">
      {heights.map((height, index) => (
        <span
          key={index}
          className={`${color} rounded-full transition-[height,opacity] duration-200 ease-out`}
          style={{
            width: small ? 3 : 4,
            height,
            opacity: index < 3 || index > barCount - 4 ? 0.24 : 0.86,
          }}
        />
      ))}
    </div>
  );
}

function FloatingAction({ children, className }: { children: ReactNode; className: string }) {
  return (
    <span className={`absolute rounded-full bg-white/78 px-6 py-3 text-sm font-semibold text-[#555b64] shadow-[0_14px_36px_rgba(20,24,32,0.07)] ${className}`}>
      {children}
    </span>
  );
}

function BrandMark({ className = "" }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" className={className} fill="none" aria-hidden="true">
      <path d="M6 14v4" stroke="currentColor" strokeWidth="2.7" strokeLinecap="round" />
      <path d="M11 9v14" stroke="currentColor" strokeWidth="2.7" strokeLinecap="round" />
      <path d="M16 5v22" stroke="currentColor" strokeWidth="2.7" strokeLinecap="round" />
      <path d="M21 10v12" stroke="currentColor" strokeWidth="2.7" strokeLinecap="round" />
      <path d="M26 14v4" stroke="currentColor" strokeWidth="2.7" strokeLinecap="round" />
    </svg>
  );
}

function KeyboardIcon() {
  return (
    <svg viewBox="0 0 32 32" className="size-9" fill="none" aria-hidden="true">
      <rect x="3" y="8" width="26" height="16" rx="4" stroke="currentColor" strokeWidth="1.8" />
      {Array.from({ length: 9 }).map((_, index) => (
        <rect key={index} x={8 + (index % 3) * 5} y={12 + Math.floor(index / 3) * 4} width="2.4" height="1.8" rx="0.9" fill="currentColor" />
      ))}
    </svg>
  );
}

function ClipboardIcon() {
  return (
    <svg viewBox="0 0 32 32" className="size-9" fill="none" aria-hidden="true">
      <path d="M11 8H9a3 3 0 0 0-3 3v14a3 3 0 0 0 3 3h14a3 3 0 0 0 3-3V11a3 3 0 0 0-3-3h-2" stroke="currentColor" strokeWidth="1.8" />
      <rect x="11" y="5" width="10" height="6" rx="2" stroke="currentColor" strokeWidth="1.8" />
      <path d="M11 17h10M11 22h7" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function BoltIcon() {
  return (
    <svg viewBox="0 0 32 32" className="size-9" fill="none" aria-hidden="true">
      <path d="M18 3 8 18h8l-2 11 10-16h-8l2-10Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
    </svg>
  );
}

function LockIcon() {
  return (
    <svg viewBox="0 0 32 32" className="size-9" fill="none" aria-hidden="true">
      <rect x="7" y="14" width="18" height="13" rx="3" stroke="currentColor" strokeWidth="1.8" />
      <path d="M11 14v-3a5 5 0 0 1 10 0v3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M16 19v3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function CircleIcon() {
  return (
    <svg viewBox="0 0 32 32" className="size-9" fill="none" aria-hidden="true">
      <circle cx="16" cy="16" r="11" stroke="currentColor" strokeWidth="1.8" />
    </svg>
  );
}
