import type { ReactNode } from "react";
import { useLoaderData } from "react-router";
import { SiteFooter, SiteHeader } from "../components/site";
import type { Route } from "./+types/download";

const releaseApiUrl = "https://api.github.com/repos/largeprob/say-to-any/releases/latest";

type GitHubReleaseAsset = {
  name: string;
  browser_download_url: string;
  size: number;
};

type GitHubRelease = {
  tag_name: string;
  html_url: string;
  published_at: string | null;
  assets: GitHubReleaseAsset[];
};

type DownloadAsset = {
  name: string;
  url: string;
  size: string;
  label: string;
  detail: string;
};

type DownloadData = {
  tagName: string;
  releaseUrl: string;
  publishedAt: string;
  windows: DownloadAsset[];
  macos: DownloadAsset[];
  error?: string;
};

export async function loader({ request }: Route.LoaderArgs): Promise<DownloadData> {
  return loadReleaseData(request.signal);
}

export async function clientLoader({ request }: Route.ClientLoaderArgs): Promise<DownloadData> {
  return loadReleaseData(request.signal);
}

clientLoader.hydrate = true as const;

async function loadReleaseData(signal?: AbortSignal): Promise<DownloadData> {
  try {
    const response = await fetch(releaseApiUrl, {
      headers: {
        Accept: "application/vnd.github+json",
        "User-Agent": "say-to-any-site",
      },
      signal,
    });

    if (!response.ok) {
      throw new Error(`GitHub Release 请求失败：${response.status}`);
    }

    const release = (await response.json()) as GitHubRelease;
    const assets = release.assets.filter((asset) => isInstallableAsset(asset.name)).map(toDownloadAsset);

    return {
      tagName: release.tag_name,
      releaseUrl: release.html_url,
      publishedAt: formatDate(release.published_at),
      windows: sortAssets(assets.filter((asset) => isWindowsAsset(asset.name)), "windows"),
      macos: sortAssets(assets.filter((asset) => isMacAsset(asset.name)), "macos"),
    };
  } catch (error) {
    return {
      tagName: "暂不可用",
      releaseUrl: "https://github.com/largeprob/say-to-any/releases",
      publishedAt: "请稍后重试",
      windows: [],
      macos: [],
      error: error instanceof Error ? error.message : "无法读取 GitHub Release。",
    };
  }
}

export function meta({}: Route.MetaArgs) {
  return [
    { title: "下载 - Say-To-Any" },
    {
      name: "description",
      content: "下载 Say-To-Any 的 Windows 和 macOS 最新版本。",
    },
  ];
}

export default function Download() {
  const release = useLoaderData<typeof loader>();

  return <DownloadPage release={release} />;
}

export function HydrateFallback({ loaderData }: Route.HydrateFallbackProps) {
  return <DownloadPage release={loaderData ?? createPendingReleaseData()} />;
}

function DownloadPage({ release }: { release: DownloadData }) {
  return (
    <main className="min-h-svh overflow-x-hidden bg-[#fbfaf8] pt-[76px] text-[#06080d]">
      <SiteHeader />
      <section className="mx-auto w-full max-w-[1120px] px-6 pb-24 pt-20 md:pb-28 md:pt-28">
        <div className="hero-copy max-w-[720px]">
          <p className="text-sm font-semibold text-[#777b82]">Download Say-To-Any</p>
          <h1 className="mt-5 text-[44px] font-semibold leading-[1.18] text-[#06080d] sm:text-[64px]">
            选择你的桌面版本
          </h1>
          <p className="mt-7 max-w-[560px] text-lg leading-8 text-[#6f737b]">
            页面会读取 GitHub Release 的最新版本，并只展示可安装的软件包。
          </p>
        </div>

        <div className="mt-14 grid gap-6 md:grid-cols-2">
          <PlatformDownload
            title="Windows"
            shortcut="双击 Alt"
            description="适用于 Windows 10+，下载后解压运行 SayToAny.exe。"
            assets={release.windows}
            emptyText="当前 Release 中未找到 Windows 包。"
            icon={<WindowsIcon />}
          />
          <PlatformDownload
            title="macOS"
            shortcut="双击 Option"
            description="提供 Intel 与 Apple Silicon 版本，首次使用需授予麦克风与辅助功能权限。"
            assets={release.macos}
            emptyText="当前 Release 中未找到 macOS 包。"
            icon={<MacIcon />}
          />
        </div>

        <div className="mt-10 flex flex-col gap-3 border-t border-[#e0ded8] pt-7 text-sm text-[#777b82] md:flex-row md:items-center md:justify-between">
          <div>
            <span className="font-semibold text-[#06080d]">{release.tagName}</span>
            <span className="mx-3 text-[#c1beb6]">/</span>
            <span>{release.publishedAt}</span>
          </div>
          <a href={release.releaseUrl} className="font-semibold text-[#06080d] transition hover:text-[#535761]">
            查看 GitHub Release
          </a>
        </div>

        {release.error ? (
          <div className="mt-8 rounded-[14px] border border-[#efe0d4] bg-[#fff7f0] px-5 py-4 text-sm leading-6 text-[#8a4c21]">
            {release.error}
          </div>
        ) : null}
      </section>
      <SiteFooter />
    </main>
  );
}

function createPendingReleaseData(): DownloadData {
  return {
    tagName: "读取中",
    releaseUrl: "https://github.com/largeprob/say-to-any/releases",
    publishedAt: "正在读取 GitHub Release",
    windows: [],
    macos: [],
  };
}

function PlatformDownload({
  title,
  shortcut,
  description,
  assets,
  emptyText,
  icon,
}: {
  title: string;
  shortcut: string;
  description: string;
  assets: DownloadAsset[];
  emptyText: string;
  icon: ReactNode;
}) {
  const primaryAsset = assets[0];
  const hasMultipleAssets = assets.length > 1;
  const hasNoAssets = assets.length === 0;
  const shouldShowAssetList = hasMultipleAssets || assets.length === 0;
  const desktopRevealClass = hasMultipleAssets
    ? "md:max-h-0 md:opacity-0 md:group-hover/download:max-h-[360px] md:group-hover/download:opacity-100 md:group-focus-within/download:max-h-[360px] md:group-focus-within/download:opacity-100"
    : hasNoAssets
      ? "md:max-h-[360px] md:opacity-100"
      : "md:max-h-0 md:opacity-0";

  return (
    <section className="group/download rounded-[24px] border border-[#e3e1dc] bg-white/58 p-7 shadow-[0_24px_70px_rgba(20,24,32,0.06)] backdrop-blur transition duration-300 hover:-translate-y-1 hover:bg-white/76 md:p-8">
      <div className="flex min-h-[178px] flex-col justify-between">
        <div className="flex items-start justify-between gap-5">
          <div>
            <div className="flex items-center gap-4">
              <span className="grid size-12 place-items-center rounded-[16px] bg-[#06080d] text-white">
                {icon}
              </span>
              <div>
                <h2 className="text-[30px] font-semibold leading-tight">{title}</h2>
                <p className="mt-1 text-sm font-semibold text-[#777b82]">{shortcut}</p>
              </div>
            </div>
            <p className="mt-6 max-w-[420px] text-base leading-7 text-[#6f737b]">{description}</p>
          </div>
          <span className="hidden rounded-full border border-[#e1dfd9] px-4 py-2 text-xs font-semibold text-[#777b82] sm:inline-flex">
            {assets.length > 0 ? `${assets.length} 个包` : "暂无"}
          </span>
        </div>

        <div className="mt-8">
          {primaryAsset && !hasMultipleAssets ? (
            <a
              href={primaryAsset.url}
              className="inline-flex h-[52px] w-full items-center justify-center rounded-[14px] bg-[#06080d] px-7 text-base font-semibold text-white shadow-[0_16px_36px_rgba(6,8,13,0.18)] transition hover:-translate-y-0.5 sm:w-auto"
            >
              下载 {title}
            </a>
          ) : (
            <button
              type="button"
              disabled={!hasMultipleAssets}
              className="inline-flex h-[52px] w-full cursor-default items-center justify-center rounded-[14px] bg-[#06080d] px-7 text-base font-semibold text-white shadow-[0_16px_36px_rgba(6,8,13,0.18)] transition group-hover/download:-translate-y-0.5 disabled:bg-[#b9b6af] sm:w-auto"
            >
              {hasMultipleAssets ? `选择 ${title} 版本` : "暂无可下载版本"}
            </button>
          )}
        </div>
      </div>

      <div
        className={`mt-6 grid gap-3 overflow-hidden transition-all duration-300 ${desktopRevealClass} ${
          shouldShowAssetList ? "max-h-[360px] opacity-100" : "max-h-0 opacity-0"
        }`}
      >
        {hasMultipleAssets ? (
          assets.map((asset) => (
            <a
              key={asset.name}
              href={asset.url}
              className="grid gap-2 rounded-[14px] border border-[#e5e3de] bg-white/76 px-5 py-4 text-left transition hover:-translate-y-0.5 hover:border-[#cfcac1] hover:bg-white sm:grid-cols-[1fr_auto] sm:items-center"
            >
              <span>
                <span className="block text-base font-semibold text-[#06080d]">{asset.label}</span>
                <span className="mt-1 block text-sm text-[#777b82]">{asset.detail}</span>
              </span>
              <span className="text-sm font-semibold text-[#06080d]">{asset.size}</span>
            </a>
          ))
        ) : primaryAsset ? null : (
          <p className="rounded-[14px] border border-[#e5e3de] bg-white/76 px-5 py-4 text-sm text-[#777b82]">
            {emptyText}
          </p>
        )}
      </div>
    </section>
  );
}

function toDownloadAsset(asset: GitHubReleaseAsset): DownloadAsset {
  return {
    name: asset.name,
    url: asset.browser_download_url,
    size: formatBytes(asset.size),
    label: createAssetLabel(asset.name),
    detail: asset.name,
  };
}

function isWindowsAsset(name: string) {
  const lowerName = name.toLowerCase();
  return lowerName.includes("win") || lowerName.includes("windows");
}

function isMacAsset(name: string) {
  const lowerName = name.toLowerCase();
  return lowerName.includes("osx") || lowerName.includes("macos") || lowerName.includes("darwin");
}

function isInstallableAsset(name: string) {
  const lowerName = name.toLowerCase();
  return [".exe", ".msi", ".zip", ".dmg", ".pkg"].some((extension) => lowerName.endsWith(extension));
}

function sortAssets(assets: DownloadAsset[], platform: "windows" | "macos") {
  const order = platform === "macos" ? ["arm64", "x64"] : ["setup", "portable", "msi", "x64", "arm64"];
  return [...assets].sort((left, right) => getAssetRank(left.name, order) - getAssetRank(right.name, order));
}

function getAssetRank(name: string, order: string[]) {
  const lowerName = name.toLowerCase();
  const index = order.findIndex((item) => lowerName.includes(item));
  return index === -1 ? order.length : index;
}

function createAssetLabel(name: string) {
  const lowerName = name.toLowerCase();
  if (isWindowsAsset(name)) {
    if (lowerName.endsWith(".msi")) {
      return "Windows MSI";
    }

    if (lowerName.includes("portable")) {
      return "Windows 便携版";
    }

    if (lowerName.includes("setup")) {
      return "Windows 安装器";
    }

    if (lowerName.includes("arm64")) {
      return "Windows ARM64";
    }

    if (lowerName.includes("x64")) {
      return "Windows x64";
    }

    return "Windows";
  }

  if (isMacAsset(name)) {
    if (lowerName.includes("arm64")) {
      return "macOS Apple Silicon";
    }

    if (lowerName.includes("x64")) {
      return "macOS Intel";
    }

    return "macOS";
  }

  return name;
}

function formatBytes(bytes: number) {
  if (!Number.isFinite(bytes) || bytes <= 0) {
    return "未知大小";
  }

  const units = ["B", "KB", "MB", "GB"];
  let value = bytes;
  let unitIndex = 0;

  while (value >= 1024 && unitIndex < units.length - 1) {
    value /= 1024;
    unitIndex += 1;
  }

  return `${value.toFixed(unitIndex === 0 ? 0 : 1)} ${units[unitIndex]}`;
}

function formatDate(value: string | null) {
  if (!value) {
    return "发布时间未知";
  }

  return new Intl.DateTimeFormat("zh-CN", {
    year: "numeric",
    month: "long",
    day: "numeric",
  }).format(new Date(value));
}

function WindowsIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-7" fill="none" aria-hidden="true">
      <path d="M4 6.8 13 5.5v8.1H4V6.8ZM15 5.2 24 4v9.6h-9V5.2ZM4 15.3h9v7.2l-9-1.3v-5.9ZM15 15.3h9V24l-9-1.2v-7.5Z" fill="currentColor" />
    </svg>
  );
}

function MacIcon() {
  return (
    <svg viewBox="0 0 28 28" className="size-7" fill="none" aria-hidden="true">
      <path d="M11 11h6v6h-6z" stroke="currentColor" strokeWidth="2" />
      <path d="M11 11H8.7A3.7 3.7 0 1 1 11 8.7V11ZM17 11V8.7A3.7 3.7 0 1 1 19.3 11H17ZM17 17h2.3A3.7 3.7 0 1 1 17 19.3V17ZM11 17v2.3A3.7 3.7 0 1 1 8.7 17H11Z" stroke="currentColor" strokeWidth="2" strokeLinejoin="round" />
    </svg>
  );
}
