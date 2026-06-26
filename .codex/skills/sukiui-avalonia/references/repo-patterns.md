# Say To Any Desktop UI Patterns

## Project Map

- Desktop app: `src/pc/pc.csproj`.
- App theme and global resources: `src/pc/App.axaml`.
- Main shell: `src/pc/Views/MainWindow.axaml`, `src/pc/Views/MainWindow.axaml.cs`, `src/pc/ViewModels/MainWindowViewModel.cs`.
- Settings dialog: `src/pc/Views/PreferencesDialogView.axaml`, `src/pc/ViewModels/PreferencesDialogViewModel.cs`.
- Floating windows: `src/pc/Views/RecordingOverlayWindow.axaml`, `PromptOverlayWindow.axaml`, and `PasteFallbackOverlayWindow.axaml`.
- Shared window chrome: `src/pc/Styles/WindowDrawnDecorations.axaml`.

## Architecture

- `pc.csproj` enables compiled bindings with `AvaloniaUseCompiledBindingsByDefault`.
- Keep `x:DataType` on views and add strongly typed properties/commands to ViewModels.
- ViewModels use CommunityToolkit MVVM attributes:
  - `[ObservableProperty]` for mutable state.
  - `[NotifyPropertyChangedFor]` for derived visibility, labels, colors, and scale values.
  - `[RelayCommand]` for UI actions.
- Avoid moving platform/window behavior into ViewModels. Put window positioning, activation, and lifecycle behavior in `.axaml.cs`.
- Use `ViewLocator` and existing view naming conventions rather than creating a second routing system.

## Visual Language

- Main background: `#F8F9FB` or nearby light blue-white.
- Primary action blue: `#126DFF`; deep hover blue: `#0F3F89`.
- Main text: `#102A56`; secondary text: `#526681` or `#6E7D99`.
- Light hover/focus fill: `#EAF3FF`; border: `#DCE8F8` or `#E5EEFA`.
- Overlay panels use transparent top-level windows with black translucent surfaces such as `#E6000000` and compact shadows.
- Prefer compact, stable dimensions for toolbars, overlays, nav rows, icon buttons, and repeated list items.
- Use `TextTrimming="CharacterEllipsis"` or wrapping where user content can be long.

## Dialogs And Overlays

- Main window hosts Suki dialogs with `suki:SukiDialogHost Manager="{Binding DialogManager}"`.
- The main ViewModel exposes `ISukiDialogManager DialogManager = new SukiDialogManager()`.
- Dialog ViewModels that close themselves receive an `ISukiDialog` and call `Dismiss()`.
- Floating overlays are `WindowDecorations="None"`, `ShowInTaskbar="False"`, `Topmost="True"`, `Background="Transparent"`, and `TransparencyLevelHint="Transparent"`.
- Overlay visual state should be bound to MainWindowViewModel booleans such as `IsDictationControlVisible`, `IsDictationProcessing`, or `IsDictationResultVisible`.

## Controls And Styling

- Keep view-local styles in `Window.Styles` or `UserControl.Styles` when only one surface uses them.
- Use class selectors for repeated styles, for example `Button.result-copy` or `Border.wave-bar`.
- For Avalonia button hover visuals, often set both the control state and template root:
  - `Button.some-class:pointerover`
  - `Button.some-class:pointerover /template/ Border#RootBorder`
- Use `Grid` with explicit `RowDefinitions` and `ColumnDefinitions` for settings forms and dense desktop layouts.
- Use `ScrollViewer` around settings content or long generated text.

## Validation

Run:

```powershell
dotnet build src/pc/pc.csproj
```

If the change affects window behavior, overlays, focus, or interactive layout, also run the app and inspect the affected surface manually when practical:

```powershell
dotnet run --project src/pc/pc.csproj
```
