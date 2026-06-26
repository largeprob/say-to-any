import { SiteFooter, SiteHeader } from "../components/site";
import type { Route } from "./+types/about";

export function meta({}: Route.MetaArgs) {
  return [
    { title: "关于我们 - Say-To-Any" },
    {
      name: "description",
      content: "了解 Say-To-Any 的产品方向。",
    },
  ];
}

export default function About() {
  return (
    <main className="min-h-svh overflow-x-hidden bg-[#fbfaf8] pt-[76px] text-[#06080d]">
      <SiteHeader />
      <section className="mx-auto grid min-h-[620px] w-full max-w-[1120px] gap-14 px-6 py-24 md:grid-cols-[0.9fr_1fr] md:items-center">
        <div className="hero-copy">
          <p className="text-sm font-semibold text-[#777b82]">About Say-To-Any</p>
          <h1 className="mt-5 text-[44px] font-semibold leading-[1.22] text-[#06080d] sm:text-[64px]">
            让输入重新回到表达本身
          </h1>
          <p className="mt-8 max-w-[470px] text-lg leading-8 text-[#6f737b]">
            Say-To-Any 希望把语音变成任何应用里的自然输入方式。你只需要说出想法，剩下的识别、整理和粘贴都交给工具完成。
          </p>
        </div>

        <div className="hero-panel rounded-[24px] border border-[#e4e2dc] bg-white/56 p-9 shadow-[0_28px_90px_rgba(30,34,42,0.08)] backdrop-blur md:p-12">
          <h2 className="text-[30px] font-semibold leading-tight">我们正在构建什么</h2>
          <div className="mt-9 grid gap-7">
            {[
              "更快的语音转文字体验",
              "跨应用的自动粘贴能力",
              "面向智能体的下一步执行能力",
            ].map((item, index) => (
              <div key={item} className="flex items-center gap-5">
                <span className="grid size-8 shrink-0 place-items-center rounded-full bg-[#06080d] text-sm font-semibold text-white">
                  {index + 1}
                </span>
                <span className="text-lg font-semibold text-[#06080d]">{item}</span>
              </div>
            ))}
          </div>
        </div>
      </section>
      <SiteFooter />
    </main>
  );
}
