# SukiUI And Avalonia Patterns

## Local Sources First

- The app references `SukiUI` package `7.0.1`.
- Local SukiUI source and demos live under `src/SukiUI-main`.
- Search local examples before using external memory:

```powershell
rg -n "SukiDialogHost|SukiTheme|SukiWindow|SukiCard|SukiBackground|SettingsLayout" src/SukiUI-main src/pc
```

## Theme Setup

`src/pc/App.axaml` registers SukiUI:

```xml
<sukiUi:SukiTheme Locale="zh-CN" ThemeColor="Blue" />
```

The app also defines reusable icon `StreamGeometry` resources in `Application.Resources`. Prefer reusing those resources or adding nearby resources in the same style when a desktop icon is needed.

## Namespaces

Common AXAML namespaces:

```xml
xmlns="https://github.com/avaloniaui"
xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
xmlns:vm="using:pc.ViewModels"
xmlns:suki="https://github.com/kikipoulet/SukiUI"
```

Use the Suki namespace only when the file needs Suki controls such as `SukiDialogHost`. Standard Avalonia controls are acceptable and often match this app better for compact settings and overlay surfaces.

## Resource Usage

Prefer dynamic Suki resources when styling should follow the active theme:

- `SukiCardBackground`
- `SukiPopupBackground`
- `SukiBorderBrush`
- `SukiText`
- `SukiLowText`
- `SukiMuteText`
- `SukiPrimaryColor`
- `SukiAccentColor5`
- `SukiPopupShadow`

When the surrounding view already uses explicit product colors, stay consistent with those explicit values instead of mixing unrelated Suki resource colors into one surface.

## Motion And State

- Use Avalonia `Transitions` for simple visual smoothing such as opacity, height, and transform changes.
- Use ViewModel `DispatcherTimer` values for live visualizations, waveforms, progress fills, or deliberately irregular motion.
- Bind fill progress with a `ScaleTransform` and `RenderTransformOrigin="0,0.5"` for left-to-right filling.
- Keep animation state deterministic enough to stop cleanly when the UI state changes.

## Layout Guardrails

- Keep overlay and toolbar controls fixed in width/height so state changes do not resize the window unexpectedly.
- Use `ClipToBounds="True"` on rounded panels that contain animated fills or scrollable text.
- Use `ScrollViewer MaxHeight` for generated text inside floating overlays.
- Avoid nesting decorative cards; use a single bordered panel for each settings group or overlay surface.

## Suki Dialog Pattern

The project uses Suki dialogs through a host in the main window:

```xml
<suki:SukiDialogHost Manager="{Binding DialogManager}" />
```

Keep dialog content as a `UserControl`, bind it to a dedicated ViewModel, and pass the dialog instance to the ViewModel when it needs to close itself. Avoid placing long-running service calls in the view.
