export function SiteHeader() {
  return (
    <header className="fixed inset-x-0 top-0 z-50 bg-[#fbfaf8]/88 backdrop-blur-xl">
      <div className="mx-auto flex h-[76px] w-full max-w-[1120px] items-center justify-between px-6">
        <a href="/" className="flex items-center gap-3 font-semibold text-[#06080d]" aria-label="Say-To-Any 首页">
          <BrandMark className="size-7" />
          <span className="text-xl">Say-To-Any</span>
        </a>

        <nav className="hidden items-center gap-20 text-[15px] font-semibold text-[#06080d] md:flex" aria-label="主导航">
          <a href="/" className="transition hover:text-[#535761]">首页</a>
          <a href="/pricing" className="transition hover:text-[#535761]">定价</a>
          <a href="/about" className="transition hover:text-[#535761]">关于我们</a>
        </nav>

        <div className="flex shrink-0 items-center gap-4">
          <a
            href="/pricing#download"
            className="inline-flex h-11 items-center rounded-[13px] bg-[#06080d] px-4 text-[15px] font-semibold text-white shadow-[0_14px_34px_rgba(6,8,13,0.16)] transition hover:-translate-y-0.5 md:px-5"
          >
            下载<span className="hidden md:inline">&nbsp;Say-To-Any</span>
          </a>
          <a
            href="/"
            className="hidden h-11 w-[58px] items-center justify-center transition hover:-translate-y-0.5 md:flex"
            aria-label="Say-To-Any logo"
          >
            <img
              src="/say-to-any-logo.png"
              alt=""
              className="h-9 w-auto object-contain"
              decoding="async"
            />
          </a>
        </div>
      </div>
    </header>
  );
}

export function SiteFooter() {
  return (
    <footer className="mx-auto flex w-full max-w-[1120px] flex-col gap-8 border-t border-[#e0ded8] px-6 py-8 text-sm text-[#777b82] md:flex-row md:items-center md:justify-between">
      <div>
        <div className="flex items-center gap-2 font-semibold text-[#06080d]">
          <BrandMark className="size-5" />
          <span>Say-To-Any</span>
        </div>
        <p className="mt-4">© 2024 Say-To-Any. All rights reserved.</p>
      </div>
      <div className="flex gap-12">
        <a href="#" className="transition hover:text-[#06080d]">隐私政策</a>
        <a href="#" className="transition hover:text-[#06080d]">用户协议</a>
      </div>
    </footer>
  );
}

export function BrandMark({ className = "" }: { className?: string }) {
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
