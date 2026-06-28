import { useEffect, useMemo, useState, type ReactNode } from "react";
import { SiteFooter, SiteHeader } from "../components/site";
import type { Route } from "./+types/pricing";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "定价 - Say-To-Any" },
    {
      name: "description",
      content: "Say-To-Any 目前处于限时免费阶段，所有功能开放使用。",
    },
  ];
}

export default function Pricing() {
  return (
    <main className="min-h-svh overflow-x-hidden bg-[#fbfaf8] pt-[76px] text-[#06080d]">
      <SiteHeader />
      <PricingHero />
      <FreeFeatures />
      <Faq />
      <SiteFooter />
    </main>
  );
}

function PricingHero() {
  return (
    <section className="mx-auto grid min-h-[500px] w-full max-w-[1120px] gap-14 px-6 pb-12 pt-24 md:grid-cols-[0.9fr_1fr] md:items-center md:pb-14 md:pt-28">
      <div className="hero-copy">
        <h1 className="text-[42px] font-semibold leading-[1.32] text-[#06080d] sm:text-[56px]">
          限时免费使用
          <br />
          专注打磨产品体验
        </h1>
        <p className="mt-8 max-w-[450px] text-lg leading-8 text-[#6f737b]">
          Say-To-Any 目前处于限时免费阶段，所有功能开放使用，未来将推出更多智能能力。
        </p>
        <div className="mt-10 flex flex-wrap items-center gap-5">
          <a
            id="download"
            href="/download"
            className="inline-flex h-[52px] items-center justify-center rounded-[14px] bg-[#06080d] px-8 text-base font-semibold text-white shadow-[0_16px_36px_rgba(6,8,13,0.18)] transition hover:-translate-y-0.5"
          >
            免费下载
          </a>
          <a
            href="#features"
            className="inline-flex h-[52px] items-center justify-center rounded-[14px] border border-[#dfded9] bg-white/72 px-8 text-base font-semibold text-[#06080d] shadow-[0_12px_28px_rgba(20,24,32,0.04)] transition hover:-translate-y-0.5"
          >
            了解更多
          </a>
        </div>
        <p className="mt-9 text-sm text-[#777b82]">支持 macOS 10.14+ / Windows 10+</p>
      </div>

      <div className="hero-panel mx-auto grid h-[340px] w-full max-w-[500px] place-items-center rounded-[24px] border border-[#e4e2dc] bg-white/56 p-8 shadow-[0_28px_90px_rgba(30,34,42,0.08)] backdrop-blur md:h-[360px]">
        <div className="text-center">
          <span className="inline-flex h-9 items-center rounded-full border border-[#e4e2dc] bg-white/84 px-5 text-sm font-semibold text-[#06080d] shadow-[0_10px_28px_rgba(20,24,32,0.06)]">
            当前状态
          </span>
          <h2 className="mt-8 text-[46px] font-semibold leading-tight text-[#06080d] sm:text-[60px]">限时免费</h2>
          <p className="mt-5 text-lg text-[#72767f]">所有功能开放使用，无需付费</p>
          <div className="mt-14 flex justify-center">
            <MiniWaveform barCount={17} />
          </div>
        </div>
      </div>
    </section>
  );
}

function FreeFeatures() {
  const features = [
    {
      title: "语音转文字",
      text: "高效准确的语音识别，将你的话转为文本",
      icon: <KeyboardIcon />,
    },
    {
      title: "自动粘贴",
      text: "识别完成后自动粘贴到当前光标位置",
      icon: <ClipboardIcon />,
    },
    {
      title: "全局快捷键",
      text: "任何应用中按快捷键即可开始说话",
      icon: <CommandIcon />,
    },
    {
      title: "隐私安全",
      text: "本地处理，保护你的数据隐私",
      icon: <LockIcon />,
    },
    {
      title: "持续更新",
      text: "我们会持续优化体验，并带来更多功能",
      icon: <RefreshIcon />,
    },
  ];

  return (
    <section id="features" className="mx-auto w-full max-w-[1120px] px-6 pb-16 pt-10">
      <h2 className="text-center text-[30px] font-semibold leading-tight text-[#06080d]">你现在可以免费使用</h2>

      <div className="mt-14 grid gap-y-10 sm:grid-cols-2 lg:grid-cols-5">
        {features.map((feature, index) => (
          <FeatureItem key={feature.title} index={index} {...feature} />
        ))}
      </div>

      <div className="mt-16 flex justify-center">
        <div className="inline-flex max-w-full items-center gap-3 rounded-full border border-[#e2e0da] bg-white/70 px-6 py-3 text-sm text-[#747882] shadow-[0_12px_38px_rgba(20,24,32,0.04)]">
          <ClockIcon />
          <span>限时免费活动时间将另行通知，敬请关注后续公告。</span>
        </div>
      </div>
    </section>
  );
}

function FeatureItem({
  index,
  icon,
  title,
  text,
}: {
  index: number;
  icon: ReactNode;
  title: string;
  text: string;
}) {
  return (
    <article className={`px-7 text-center ${index > 0 ? "lg:border-l lg:border-[#e5e3de]" : ""}`}>
      <div className="mx-auto grid size-16 place-items-center rounded-full border border-[#e2e0da] bg-white/68 text-[#06080d] shadow-[0_12px_34px_rgba(20,24,32,0.04)]">
        {icon}
      </div>
      <h3 className="mt-7 text-base font-semibold text-[#06080d]">{title}</h3>
      <p className="mx-auto mt-4 max-w-[150px] text-sm leading-7 text-[#777b82]">{text}</p>
    </article>
  );
}

function Faq() {
  const questions = [
    "免费期间会有功能限制吗？",
    "我的数据会被保存或上传吗？",
    "未来会收费吗？",
    "如果未来收费，现在用户会受影响吗？",
  ];

  return (
    <section className="mx-auto w-full max-w-[1000px] px-6 pb-24 pt-4">
      <h2 className="text-center text-[30px] font-semibold text-[#06080d]">常见问题</h2>
      <div className="mt-8 overflow-hidden rounded-[20px] border border-[#e1dfd9] bg-white/54 shadow-[0_20px_70px_rgba(20,24,32,0.04)]">
        {questions.map((question, index) => (
          <button
            key={question}
            className={`flex min-h-20 w-full items-center justify-between px-10 text-left text-lg font-semibold text-[#06080d] transition hover:bg-white/72 ${index > 0 ? "border-t border-[#e6e4df]" : ""}`}
            type="button"
          >
            <span>{question}</span>
            <span className="text-2xl font-semibold">+</span>
          </button>
        ))}
      </div>
    </section>
  );
}

function MiniWaveform({ barCount }: { barCount: number }) {
  const baseHeights = useMemo(
    () => Array.from({ length: barCount }, (_, index) => 8 + ((index * 11) % 18)),
    [barCount],
  );
  const [heights, setHeights] = useState(baseHeights);

  useEffect(() => {
    const timer = window.setInterval(() => {
      setHeights((current) =>
        current.map((height, index) => {
          const center = Math.abs(index - (barCount - 1) / 2) / (barCount / 2);
          const next = 8 + Math.random() * 28;
          return Math.max(6, next * (1 - center * 0.35) + height * 0.2);
        }),
      );
    }, 190);

    return () => window.clearInterval(timer);
  }, [barCount]);

  return (
    <div className="flex h-10 items-center justify-center gap-1" aria-hidden="true">
      {heights.map((height, index) => (
        <span
          key={index}
          className="rounded-full bg-[#bfc3ca] transition-[height,opacity] duration-200 ease-out"
          style={{
            width: 4,
            height,
            opacity: index < 2 || index > barCount - 3 ? 0.42 : 0.82,
          }}
        />
      ))}
    </div>
  );
}

function KeyboardIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-8" fill="none" aria-hidden="true">
      <rect x="4" y="8" width="20" height="12" rx="2.5" stroke="currentColor" strokeWidth="1.8" />
      {Array.from({ length: 9 }).map((_, index) => (
        <rect key={index} x={8 + (index % 3) * 4} y={11 + Math.floor(index / 3) * 3} width="1.8" height="1.4" rx="0.7" fill="currentColor" />
      ))}
    </svg>
  );
}

function ClipboardIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-8" fill="none" aria-hidden="true">
      <path d="M11 8H9.5A2.5 2.5 0 0 0 7 10.5v12A2.5 2.5 0 0 0 9.5 25h10a2.5 2.5 0 0 0 2.5-2.5v-12A2.5 2.5 0 0 0 19.5 8H18" stroke="currentColor" strokeWidth="1.8" />
      <rect x="11" y="5" width="7" height="6" rx="1.8" stroke="currentColor" strokeWidth="1.8" />
      <path d="M11 16h7M11 20h5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function CommandIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-8" fill="none" aria-hidden="true">
      <path d="M10 10h8v8h-8z" stroke="currentColor" strokeWidth="1.8" />
      <path d="M10 10H7.8A3.3 3.3 0 1 1 10 7.8V10ZM18 10V7.8A3.3 3.3 0 1 1 20.2 10H18ZM18 18h2.2A3.3 3.3 0 1 1 18 20.2V18ZM10 18v2.2A3.3 3.3 0 1 1 7.8 18H10Z" stroke="currentColor" strokeWidth="1.8" strokeLinejoin="round" />
    </svg>
  );
}

function LockIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-8" fill="none" aria-hidden="true">
      <rect x="7" y="12" width="14" height="11" rx="2.4" stroke="currentColor" strokeWidth="1.8" />
      <path d="M10 12V9a4 4 0 0 1 8 0v3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M14 16.5v2.3" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
    </svg>
  );
}

function RefreshIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-8" fill="none" aria-hidden="true">
      <path d="M21.5 9.5A8.2 8.2 0 0 0 7.1 8.1L5.5 10.2" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M5.2 5.5v4.9h4.9" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
      <path d="M6.5 18.5a8.2 8.2 0 0 0 14.4 1.4l1.6-2.1" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" />
      <path d="M22.8 22.5v-4.9h-4.9" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}

function ClockIcon() {
  return (
    <svg viewBox="0 0 20 20" className="size-5 shrink-0 text-[#06080d]" fill="none" aria-hidden="true">
      <circle cx="10" cy="10" r="6.5" stroke="currentColor" strokeWidth="1.5" />
      <path d="M10 6.8v3.4l2.4 1.4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  );
}
