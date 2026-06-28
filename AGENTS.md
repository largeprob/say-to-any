# Repository Guidelines

## Project Structure & Module Organization

There are four main modules under `src/`. `src/frontend` is the Say To Any official website, built with React Router 7, TypeScript, React 19, and Tailwind CSS 4; route modules live in `app/routes`, shared UI in `app/components`, and global styles in `app/app.css`. `src/pc` is the current Windows desktop product, built with .NET 10, Avalonia, SukiUI, and Velopack; desktop code is grouped by role: `Views/` for AXAML windows, `ViewModels/` for MVVM state, `Models/` for data objects, `Services/` for platform logic and integrations, `Styles/` for shared AXAML styles, and `Assets/` for resources. `src/server` is the backend service solution for API, application, domain, infrastructure, EF Core, and service defaults projects. `src/tauriRust` is the Rust-based desktop product implementation using Tauri, React Router, TypeScript, and Rust. Packaging output belongs under `artifacts/`.

## Build, Test, and Development Commands

- `dotnet build src/pc/pc.sln`: compile the Avalonia desktop app.
- `dotnet run --project src/pc/pc.csproj`: run the desktop app locally.
- `.\scripts\package-pc.ps1 -Version 0.1.0`: publish and package the desktop app with Velopack.
- `dotnet build src/server/SqlBoTx.slnx`: compile the backend service solution.
- `cd src/frontend; pnpm install`: install frontend dependencies from `pnpm-lock.yaml`.
- `cd src/frontend; pnpm dev`: start the official website React Router dev server on port `6002`.
- `cd src/frontend; pnpm build`: build the official website.
- `cd src/frontend; pnpm typecheck`: generate website route types and run TypeScript checks.
- `cd src/tauriRust; pnpm install`: install Tauri React frontend dependencies.
- `cd src/tauriRust; pnpm tauri:dev`: run the Rust/Tauri desktop app locally.
- `cd src/tauriRust; pnpm build`: build the Tauri React frontend.
- `cd src/tauriRust; pnpm typecheck`: generate Tauri route types and run TypeScript checks.

## Coding Style & Naming Conventions

Use nullable-aware C# with implicit usings, matching the project settings. Keep desktop classes PascalCase and suffix Avalonia pairs consistently, for example `MainWindow.axaml`, `MainWindow.axaml.cs`, and `MainWindowViewModel.cs`. Name services by responsibility, such as `SettingsStore`. Keep backend projects aligned with their existing application/domain/infrastructure boundaries. For website and Tauri React code, use TypeScript modules, PascalCase React components, and lowercase route filenames such as `about.tsx`.

## Testing Guidelines

No test projects are checked in. For desktop changes, add focused unit tests in a future sibling project such as `src/pc.Tests` when logic can be isolated from UI or Windows APIs. For backend changes, prefer focused tests near the service or domain behavior when a test project is added. For website or Tauri React changes, run the relevant `pnpm typecheck`; add route or component tests once available. Document manual verification for microphone, hotkey, overlay, packaging, text insertion, authentication, or API changes.

## Commit & Pull Request Guidelines

Git history currently contains only `Initial release`, so there is no strict local convention. Use short, descriptive commit subjects such as `Add microphone preference persistence` or `Fix frontend pricing route`. Pull requests should describe the change, list commands run, link issues, and include screenshots or recordings for UI changes. Note configuration, packaging, or release-impacting changes explicitly.

## Security & Configuration Tips

Do not commit API keys, tokens, microphone captures, or generated release artifacts. Packaging uploads use `GITHUB_TOKEN`; pass it through the environment instead of hardcoding it. Keep local settings and generated files out of source control unless they are intentionally shared configuration.
