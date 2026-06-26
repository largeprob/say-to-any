---
name: sukiui-avalonia
description: "Build and refine the Say To Any desktop UI with Avalonia and SukiUI. Use when Codex needs to create or modify AXAML views, SukiUI dialogs, transparent overlay windows, desktop navigation, settings pages, styles, theme resources, or view-model bindings under src/pc."
---

# SukiUI Avalonia

## Overview

Use this skill to work on the `src/pc` desktop app UI. The app uses Avalonia 12, SukiUI 7, compiled AXAML bindings, CommunityToolkit MVVM, and local view/view-model patterns that should be preserved.

## Workflow

1. Locate the affected UI surface before editing:
   - Main shell: `src/pc/Views/MainWindow.axaml` and `src/pc/ViewModels/MainWindowViewModel.cs`.
   - Settings/dialog UI: `src/pc/Views/PreferencesDialogView.axaml` and `src/pc/ViewModels/PreferencesDialogViewModel.cs`.
   - Floating overlays: `src/pc/Views/*OverlayWindow.axaml` plus the matching `.axaml.cs` file.
   - Global styles/theme bootstrapping: `src/pc/App.axaml` and `src/pc/Styles/WindowDrawnDecorations.axaml`.

2. Read the relevant reference before implementation:
   - For repository-specific architecture, colors, overlays, dialogs, and validation, read `references/repo-patterns.md`.
   - For SukiUI/Avalonia control and styling guidance, read `references/sukiui-patterns.md`.
   - For extracted patterns from the actual local SukiUI demo/source, read `references/demo-derived-patterns.md`.
   - For local SukiUI demo/source examples, read `references/local-example-index.md` and open only the example files relevant to the requested UI.

3. Implement in the smallest local surface that owns the behavior:
   - Put layout and visual states in AXAML.
   - Put user actions, state, derived visibility properties, and timers in the ViewModel.
   - Keep view code-behind for window positioning, platform window behavior, and Avalonia lifecycle details.

4. Preserve compiled binding quality:
   - Keep `x:DataType` on AXAML roots.
   - Add `[ObservableProperty]`, `[NotifyPropertyChangedFor]`, and `[RelayCommand]` in ViewModels instead of manual boilerplate unless the surrounding file already needs custom logic.
   - Prefer derived boolean properties such as `IsSettingsVisible` over inline converter-heavy AXAML.

5. Validate with:

```powershell
dotnet build src/pc/pc.csproj
```

For substantial layout or window behavior changes, also run the app when practical:

```powershell
dotnet run --project src/pc/pc.csproj
```

## UI Judgment

Use the existing desktop product language: quiet, crisp, light surfaces; blue as the primary action color; compact spacing; stable control dimensions; clear hover states; and readable Chinese labels where the rest of the view uses Chinese. Avoid unrelated redesigns, large decorative sections, or introducing a second design system.

When unsure whether to use a SukiUI-specific control, inspect the local SukiUI source and demo under `src/SukiUI-main` before relying on memory or web examples. Do not copy the demo wholesale; use it as an implementation reference and adapt it to this app's quieter desktop style.
