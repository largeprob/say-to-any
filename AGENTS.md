# Repository Guidelines

## Project Structure & Module Organization

There are two applications. `src/pc` is the Windows desktop app, built with .NET 10, Avalonia, SukiUI, and Velopack. Desktop code is grouped by role: `Views/` for AXAML windows, `ViewModels/` for MVVM state, `Models/` for data objects, `Services/` for platform logic and integrations, `Styles/` for shared AXAML styles, and `Assets/` for resources. `src/frontend` is a React Router 7 frontend using TypeScript, React 19, and Tailwind CSS 4; route modules live in `app/routes`, shared UI in `app/components`, and global styles in `app/app.css`. Packaging output belongs under `artifacts/`.

## Build, Test, and Development Commands

- `dotnet build src/pc/pc.sln`: compile the Avalonia desktop app.
- `dotnet run --project src/pc/pc.csproj`: run the desktop app locally.
- `.\scripts\package-pc.ps1 -Version 0.1.0`: publish and package the desktop app with Velopack.
- `cd src/frontend; pnpm install`: install frontend dependencies from `pnpm-lock.yaml`.
- `cd src/frontend; pnpm dev`: start the React Router dev server on port `6002`.
- `cd src/frontend; pnpm build`: build the frontend.
- `cd src/frontend; pnpm typecheck`: generate route types and run TypeScript checks.

## Coding Style & Naming Conventions

Use nullable-aware C# with implicit usings, matching the project settings. Keep desktop classes PascalCase and suffix Avalonia pairs consistently, for example `MainWindow.axaml`, `MainWindow.axaml.cs`, and `MainWindowViewModel.cs`. Name services by responsibility, such as `SettingsStore`. For frontend code, use TypeScript modules, PascalCase React components, and lowercase route filenames such as `about.tsx`.

## Testing Guidelines

No test projects are checked in. For desktop changes, add focused unit tests in a future sibling project such as `src/pc.Tests` when logic can be isolated from UI or Windows APIs. For frontend changes, run `pnpm typecheck`; add route or component tests once available. Document manual verification for microphone, hotkey, overlay, packaging, or text insertion changes.

## Commit & Pull Request Guidelines

Git history currently contains only `Initial release`, so there is no strict local convention. Use short, descriptive commit subjects such as `Add microphone preference persistence` or `Fix frontend pricing route`. Pull requests should describe the change, list commands run, link issues, and include screenshots or recordings for UI changes. Note configuration, packaging, or release-impacting changes explicitly.

## Security & Configuration Tips

Do not commit API keys, tokens, microphone captures, or generated release artifacts. Packaging uploads use `GITHUB_TOKEN`; pass it through the environment instead of hardcoding it. Keep local settings and generated files out of source control unless they are intentionally shared configuration.
