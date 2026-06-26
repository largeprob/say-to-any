# Say To Any

[![GitHub stars](https://img.shields.io/github/stars/largeprob/say-to-any?style=social)](https://github.com/largeprob/say-to-any/stargazers)
[![Release](https://img.shields.io/github/v/release/largeprob/say-to-any?display_name=tag)](https://github.com/largeprob/say-to-any/releases)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)

Say To Any 是一个 Windows 桌面听写工具：录音、调用 OpenAI 兼容语音识别接口、可选调用大模型整理文本，然后把结果复制或自动粘贴到当前输入位置。

## 功能特性

- 双击 `Alt` 开始或停止听写，适合在任意输入框中快速调用。
- 支持分别配置 ASR 和 LLM 的 `Base URL`、`API Key`、`Model`。
- 支持语音识别后自动清洗文本、补标点、整理任务列表。
- 支持自动粘贴到当前焦点窗口，失败时提供复制兜底。
- 支持麦克风选择、输入音量反馈、最长录音时间和请求超时设置。
- 本地保存听写历史，可复制文本、下载音频、设置历史保留周期。
- 使用 Velopack 支持 GitHub Release 更新与 Windows 打包。

## 截图

暂无截图。欢迎在提交 UI 变更时补充主界面、设置页和历史记录页截图。

## 快速开始

### 下载使用

前往 [Releases](https://github.com/largeprob/say-to-any/releases) 下载最新 Windows 版本并安装。

首次启动后，在设置中填写模型服务：

- LLM：用于文本整理，默认模型为 `gpt-4o-mini`。
- ASR：用于语音识别，默认模型为 `qwen3-asr-flash`。
- Language：可设为 `auto` 或具体语言代码。

### 本地开发

桌面端：

```powershell
dotnet restore src/pc/pc.sln
dotnet build src/pc/pc.sln
dotnet run --project src/pc/pc.csproj
```

前端：

```powershell
cd src/frontend
pnpm install
pnpm dev
pnpm typecheck
pnpm build
```

打包桌面端：

```powershell
.\scripts\package-pc.ps1 -Version 0.1.0
```

## 项目结构

```text
src/pc                Windows 桌面端，Avalonia + SukiUI
src/pc/Views          AXAML 视图和窗口
src/pc/ViewModels     MVVM 状态与命令
src/pc/Services       录音、热键、文本插入、模型请求、更新服务
src/pc/Models         设置、历史记录、音频设备等数据模型
src/frontend          React Router 前端页面
scripts               打包和发布脚本
artifacts             本地构建与发布产物
```

## Star History

下方图表展示 GitHub Star 增长历史，并会随仓库数据自动更新。

[![Star History Chart](https://api.star-history.com/svg?repos=largeprob/say-to-any&type=Date)](https://www.star-history.com/#largeprob/say-to-any&Date)

## 配置与安全

- 不要把 API Key、GitHub Token、录音文件或本地设置提交到仓库。
- 上传 GitHub Release 时通过环境变量 `GITHUB_TOKEN` 传入令牌。
- 更新检查可通过 `SAY_TO_ANY_UPDATE_REPOSITORY_URL`、`SAY_TO_ANY_UPDATE_GITHUB_TOKEN`、`SAY_TO_ANY_UPDATE_PRERELEASE`、`SAY_TO_ANY_UPDATE_AUTO_RESTART` 调整。

## 贡献

欢迎提交 Issue 和 Pull Request。提交 PR 时请说明修改内容、运行过的命令，以及是否影响 UI、录音、热键、自动粘贴或打包发布流程。
