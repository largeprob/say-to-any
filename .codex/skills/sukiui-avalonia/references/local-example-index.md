# Local SukiUI Example Index

Use this index to find concrete examples in the bundled SukiUI source. Load only the relevant files for the current task.

## Fast Search

Start with `references/demo-derived-patterns.md` for distilled usage patterns. Use this file when the distilled pattern is not enough and you need the full local source example.

```powershell
rg -n "SukiDialogHost|SukiDialog|SukiToast|SukiWindow|SukiMainHost|SukiSideMenu|SettingsLayout|GlassCard|WaveProgress|BusyArea|CircleProgressBar|InfoBar|Stepper" src/SukiUI-main src/pc
```

For AXAML-only searches:

```powershell
rg -n "Classes=|Style Selector|ControlTheme|DynamicResource Suki|Transitions|Suki" src/SukiUI-main -g "*.axaml" -g "*.xaml"
```

## Demo App Entry Points

- Demo shell: `src/SukiUI-main/SukiUI.Demo/SukiUIDemoView.axaml` and `SukiUIDemoViewModel.cs`.
- Demo app setup/theme: `src/SukiUI-main/SukiUI.Demo/App.axaml` and `App.axaml.cs`.
- Demo navigation/view registry: `src/SukiUI-main/SukiUI.Demo/Common/SukiViews.cs`.
- Demo feature base classes: `src/SukiUI-main/SukiUI.Demo/Features/DemoPageBase.cs`.

## Controls Library Examples

- Buttons: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/ButtonsView.axaml`.
- Cards/glass cards: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/CardsView.axaml`.
- Text and typography: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/TextView.axaml`.
- Toggles and switches: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/TogglesView.axaml`.
- Progress/loading: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/ProgressView.axaml`.
- Info bars: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/InfoBarView.axaml`.
- Expanders: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/ExpanderView.axaml`.
- Menus/context menus: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/ContextMenusView.axaml`.
- Collections/lists: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/CollectionsView.axaml`.
- Tabs: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/TabControl/TabControlView.axaml`.
- Stack pages: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/StackPage/StackPageView.axaml`.
- Property grid: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/PropertyGridView.axaml`.
- Icons: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/IconsView.axaml`.
- Misc controls: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/MiscView.axaml`.

## Dialogs, Toasts, And Windows

- Dialog overview: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Dialogs/DialogsView.axaml`.
- Dialog ViewModel logic: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Dialogs/DialogsViewModel.Dialogs.cs`.
- Message box logic: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Dialogs/DialogsViewModel.MessageBoxes.cs`.
- Dialog content example: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Dialogs/VmDialogView.axaml`.
- Dialog window example: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Dialogs/DialogWindowDemo.axaml`.
- Tool window example: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Dialogs/ToolWindow.axaml`.
- Toast examples: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Toasts/ToastsView.axaml` and `ToastsViewModel.cs`.

## Theming, Effects, And Motion

- Theme switching: `src/SukiUI-main/SukiUI.Demo/Features/Theming/ThemingView.axaml`.
- Custom theme dialog: `src/SukiUI-main/SukiUI.Demo/Features/CustomTheme/CustomThemeDialogView.axaml`.
- Color palette browser: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/Colors/ColorsView.axaml`.
- Easy animations: `src/SukiUI-main/SukiUI.Demo/Features/Helpers/Easy Animations.axaml`.
- Animation behaviors: `src/SukiUI-main/SukiUI.Demo/Features/Helpers/AnimationBehaviors.axaml`.
- Easing examples: `src/SukiUI-main/SukiUI.Demo/Features/Helpers/CustomEasings.axaml` and `SpringEase/SpringEasing.axaml`.
- Organic movement: `src/SukiUI-main/SukiUI.Demo/Features/Helpers/OrganicMove.axaml`.
- Pulling effect: `src/SukiUI-main/SukiUI.Demo/Features/Helpers/PullingEffect.axaml`.
- Shader/effects: `src/SukiUI-main/SukiUI.Demo/Features/Effects/EffectsView.axaml`.

## Dock And Dashboard Examples

Use these only when a task explicitly needs complex workspace/docking UI:

- Dock control page: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/DockView.axaml`.
- Dock MVVM page: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/DockMvvmView.axaml`.
- Dock factory: `src/SukiUI-main/SukiUI.Demo/Features/ControlsLibrary/DockControls/DockFactory.cs`.
- Dashboard page: `src/SukiUI-main/SukiUI.Demo/Features/Dashboard/DashboardView.axaml`.

## SukiUI Control Source

Open these when the demo does not explain a control's API or template:

- Theme index: `src/SukiUI-main/SukiUI/Theme/Index.axaml`.
- Light/dark resources: `src/SukiUI-main/SukiUI/ColorTheme/Light.axaml` and `Dark.axaml`.
- Buttons: `src/SukiUI-main/SukiUI/Theme/Button.axaml`.
- Text boxes: `src/SukiUI-main/SukiUI/Theme/TextBox.axaml`.
- Sliders: `src/SukiUI-main/SukiUI/Theme/SliderStyles.xaml`.
- Progress bars: `src/SukiUI-main/SukiUI/Theme/ProgressBar.axaml`.
- Dialog host: `src/SukiUI-main/SukiUI/Controls/Hosts/SukiDialogHost.axaml` and `SukiDialogHost.cs`.
- Dialog control: `src/SukiUI-main/SukiUI/Controls/SukiDialog.axaml` and `SukiDialog.cs`.
- Window: `src/SukiUI-main/SukiUI/Controls/SukiWindow.axaml` and `SukiWindow.axaml.cs`.
- Main host/navigation: `src/SukiUI-main/SukiUI/Controls/SukiMainHost.axaml`, `SukiSideMenu.axaml`, and `SukiSideMenuItem.axaml`.
- Settings layout: `src/SukiUI-main/SukiUI/Controls/Settings/SettingsLayout.axaml`.
- Glass cards: `src/SukiUI-main/SukiUI/Controls/GlassMorphism/GlassCard.axaml`.
- Busy/loading: `src/SukiUI-main/SukiUI/Controls/BusyArea.axaml`, `Loading.cs`, `CircleProgressBar.axaml`, and `WaveProgress.axaml`.
- Info bar/badge: `src/SukiUI-main/SukiUI/Controls/InfoBar.axaml` and `InfoBadge.axaml`.
- Stepper: `src/SukiUI-main/SukiUI/Controls/Stepper.axaml` and `VerticalStepper.axaml`.

## Adaptation Rule

SukiUI demo pages are intentionally broad and decorative. For this app, adapt examples into compact production UI: prefer fewer panels, stable dimensions, restrained shadows, blue primary actions, and the existing Chinese product copy style.
