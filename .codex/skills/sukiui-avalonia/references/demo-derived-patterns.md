# Demo-Derived SukiUI Patterns

These notes are extracted from `src/SukiUI-main/SukiUI.Demo` and selected SukiUI source files. Use them before copying snippets from memory.

## Demo Architecture

The demo registers `SukiTheme`, SukiUI styles, and optional third-party styles in `SukiUI.Demo/App.axaml`.

Core demo setup in `App.axaml.cs`:

- Build a `ServiceCollection`.
- Register view/view-model pairs with `SukiViews.AddView<TView, TViewModel>()`.
- Register singleton managers:
  - `ISukiToastManager -> SukiToastManager`
  - `ISukiDialogManager -> SukiDialogManager`
- Add a `ViewLocator` to `Application.DataTemplates`.
- Create the main window from the main ViewModel.

For this app, do not copy the full demo DI shell unless building a new SukiUI sample app. `src/pc` already has its own ViewModels and dialog host pattern.

## Main Window Pattern

The demo main shell is a `suki:SukiWindow` with built-in hosts, title-bar content, menu items, and `SukiSideMenu`.

Useful properties seen in `SukiUIDemoView.axaml`:

```xml
<suki:SukiWindow
    BackgroundAnimationEnabled="{Binding AnimationsEnabled}"
    BackgroundStyle="{Binding BackgroundStyle}"
    BackgroundTransitionsEnabled="{Binding TransitionsEnabled}"
    CanFullScreen="True"
    CanPin="True"
    IsMenuVisible="True"
    IsTitleBarVisible="{Binding TitleBarVisible, Mode=TwoWay}"
    ShowBottomBorder="{Binding ShowBottomBar}"
    ShowTitlebarBackground="{Binding ShowTitleBar}">
    <suki:SukiWindow.Hosts>
        <suki:SukiToastHost Manager="{Binding ToastManager}" />
        <suki:SukiDialogHost Manager="{Binding DialogManager}" />
    </suki:SukiWindow.Hosts>
</suki:SukiWindow>
```

The current app uses standard `Window` plus custom styles instead of `SukiWindow`. Preserve that unless the task explicitly asks to migrate the shell.

## Theme Pattern

Theme setup:

```xml
<suki:SukiTheme Locale="zh-CN" ThemeColor="Blue" />
```

Theme ViewModel operations from the demo:

```csharp
private readonly SukiTheme theme = SukiTheme.GetInstance();

theme.SwitchBaseTheme();
theme.ChangeBaseTheme(ThemeVariant.Light);
theme.ChangeColorTheme(colorTheme);
theme.IsRightToLeft = !theme.IsRightToLeft;
```

The demo listens to `OnBaseThemeChanged` and `OnColorThemeChanged` to sync UI state and show toasts. In `src/pc`, prefer the existing fixed blue product palette unless a user asks for runtime theming.

## Navigation And Pages

The demo maps ViewModels to Views with `SukiViews`; demo pages inherit `DemoPageBase(displayName, icon, index)`.

`SukiSideMenu` usage:

```xml
<suki:SukiSideMenu
    IsSearchEnabled="True"
    ItemsSource="{Binding DemoPages}"
    SelectedItem="{Binding ActivePage}">
    <suki:SukiSideMenu.ItemTemplate>
        <DataTemplate>
            <suki:SukiSideMenuItem Classes="Compact" Header="{Binding DisplayName}">
                <suki:SukiSideMenuItem.Icon>
                    <avalonia:MaterialIcon Kind="{Binding Icon}" />
                </suki:SukiSideMenuItem.Icon>
            </suki:SukiSideMenuItem>
        </DataTemplate>
    </suki:SukiSideMenu.ItemTemplate>
</suki:SukiSideMenu>
```

Use this only for a side-menu style desktop shell. The Say To Any app currently uses a custom compact navigation in `MainWindow.axaml`.

## Buttons

Button classes seen in `ButtonsView.axaml` and `XamlData.cs`:

- Default: `<Button Content="Neutral" />`
- Basic: `<Button Classes="Basic" Content="Basic" />`
- Flat: `<Button Classes="Flat" Content="Flat" />`
- Rounded flat: `<Button Classes="Flat Rounded" Content="Rounded" />`
- Outlined: `<Button Classes="Outlined" Content="Outlined" />`
- Accent variants: `Basic Accent`, `Flat Accent`, `Outlined Accent`
- Semantic variants: `Success`, `Information`, `Warning`, `Danger`
- Sizes and icon variants: `Small`, `Large`, `Icon`

Attached properties from Suki source:

```xml
<Button suki:ButtonExtensions.ShowProgress="{Binding IsBusy}" />
<Button suki:ButtonExtensions.Icon="{avalonia:MaterialIconExt Kind=Calendar}" Classes="Flat" />
```

For `src/pc`, prefer local button styles when the surface already has hand-tuned colors. Use Suki button classes for new Suki-native dialogs or settings surfaces.

## Cards And Grouping

`GlassCard` properties from source and demo:

```xml
<suki:GlassCard
    CornerRadius="15"
    IsOpaque="{Binding IsOpaque}"
    IsInteractive="{Binding IsInteractive}"
    IsAnimated="True"
    Command="{Binding SomeCommand}">
    <suki:GroupBox Header="Header">
        <!-- content -->
    </suki:GroupBox>
</suki:GlassCard>
```

Classes include `Primary` and `Accent`. Card-local opacity can be overridden with a `GlassOpacity` resource.

For Say To Any, avoid copying the demo's decorative card-heavy pages. Use one lightweight panel per settings group or overlay state.

## Inputs And Toggles

Minimal snippets from the demo:

```xml
<TextBox Text="Text box" />
<ToggleSwitch IsChecked="{Binding Enabled}" />
<CheckBox Content="Option" IsChecked="{Binding Enabled}" />
<Slider Minimum="0" Maximum="100" TickFrequency="1" IsSnapToTickEnabled="True" Value="{Binding Value}" />
<NumericUpDown FormatString="F0" Value="{Binding Value}" />
```

Radio/toggle chip styles:

```xml
<RadioButton Classes="Chips" GroupName="Mode" Content="Option One" />
<RadioButton Classes="GigaChips" GroupName="Plan" Content="Professional" />
<ToggleButton Classes="Switch Accent" IsChecked="{Binding Enabled}" />
```

Use chips for segmented choices only when they are visually compact and labels fit.

## Progress And Loading

From `ProgressView.axaml` and `XamlData.cs`:

```xml
<suki:WaveProgress Value="{Binding ProgressValue}" IsTextVisible="{Binding IsTextVisible}" />

<suki:CircleProgressBar Width="130" Height="130" StrokeWidth="11" Value="{Binding ProgressValue}">
    <TextBlock Text="{Binding ProgressValue, StringFormat={}{0:#0}%}" />
</suki:CircleProgressBar>

<ProgressBar Value="{Binding ProgressValue}" ShowProgressText="{Binding IsTextVisible}" />
<ProgressBar IsIndeterminate="{Binding IsIndeterminate}" />

<suki:Loading LoadingStyle="Simple" />
<suki:Loading LoadingStyle="Glow" />
<suki:Loading LoadingStyle="Pellets" />
```

Stepper:

```xml
<suki:Stepper Index="{Binding StepIndex}" Steps="{Binding Steps}" />
<suki:Stepper AlternativeStyle="True" Index="{Binding StepIndex}" Steps="{Binding Steps}" />
<suki:VerticalStepper Index="{Binding StepIndex}" Steps="{Binding VerticalSteps}" />
```

For the current recording/loading overlay, the app intentionally uses a custom left-to-right fill rather than Suki's default progress controls. Preserve that UX unless asked otherwise.

## InfoBar

`InfoBar` supports title, message, severity, closability, opacity, selectable text, and open state:

```xml
<suki:InfoBar
    Title="Warning"
    Message="{Binding Message}"
    Severity="Warning"
    IsClosable="{Binding IsClosable}"
    IsOpen="{Binding IsOpen, Mode=TwoWay}"
    IsOpaque="{Binding IsOpaque}"
    IsTextSelectable="{Binding IsTextSelectable}" />
```

Severity values map to Avalonia `NotificationType`: `Information`, `Success`, `Warning`, `Error`.

## Dialogs

The demo uses a fluent builder on `ISukiDialogManager`.

Simple dialog:

```csharp
DialogManager.CreateDialog()
    .WithTitle("A Standard Dialog")
    .WithContent("This is a standard dialog.")
    .WithActionButton("Dismiss", _ => { }, true)
    .TryShow();
```

Dismiss by background click:

```csharp
DialogManager.CreateDialog()
    .WithTitle("Background Closing Dialog")
    .WithContent("Dismiss by clicking outside.")
    .Dismiss().ByClickingBackground()
    .TryShow();
```

ViewModel dialog:

```csharp
DialogManager.CreateDialog()
    .WithViewModel(dialog => new VmDialogViewModel(dialog))
    .TryShow();
```

Dialog ViewModel close pattern:

```csharp
public partial class VmDialogViewModel(ISukiDialog dialog) : ObservableObject
{
    [RelayCommand]
    private void CloseDialog() => dialog.Dismiss();
}
```

Builder methods confirmed in source include `OfType`, `WithTitle`, `WithContent`, `ShowCardBackground`, `WithViewModel`, `Dismiss().ByClickingBackground()`, `WithActionButton`, `OnDismissed`, `WithYesNoResult`, and `WithOkResult`.

## Toasts

The demo uses `ISukiToastManager`.

Simple info toast:

```csharp
ToastManager.CreateSimpleInfoToast()
    .WithTitle("Theme Changed")
    .WithContent("Theme has changed.")
    .Queue();
```

Typed toast:

```csharp
ToastManager.CreateToast()
    .WithTitle("Warning")
    .WithContent("Something happened.")
    .OfType(NotificationType.Warning)
    .Dismiss().After(TimeSpan.FromSeconds(3))
    .Dismiss().ByClicking()
    .Queue();
```

Action/loading toast:

```csharp
ToastManager.CreateToast()
    .WithTitle("Update Available")
    .WithContent("Update is available.")
    .WithActionButton("Later", _ => { }, true, SukiButtonStyles.Basic)
    .WithActionButton("Update", _ => { }, true)
    .Queue();

ToastManager.CreateToast()
    .WithTitle("Loading")
    .WithLoadingState(true)
    .WithContent("Working...")
    .Dismiss().After(TimeSpan.FromSeconds(3))
    .Queue();
```

Builder methods confirmed in source include `CreateToast`, `CreateSimpleInfoToast`, `WithTitle`, `WithLoadingState`, `WithContent`, `OfType`, `Dismiss().After`, `Dismiss().ByClicking`, `OnClicked`, `OnDismissed`, and `WithActionButton`.

## Animations And Behaviors

Use the Suki animations namespace:

```xml
xmlns:animations="clr-namespace:SukiUI.Animations;assembly=SukiUI"
```

Attached behaviors used in the demo:

```xml
<suki:GlassCard animations:HoverBehavior.Scale="1.1" />
<suki:GlassCard animations:LoadingBehavior.IsBusy="{Binding IsBusy}" />
<suki:GlassCard animations:GlowBehavior.IsActive="{Binding IsActive}"
                animations:GlowBehavior.Color="{DynamicResource SukiPrimaryColor}"
                animations:GlowBehavior.Thickness="3" />
<Button animations:VisibilityBehavior.IsVisible="{Binding IsEnabled}" />
<suki:GlassCard animations:FadeInBehavior.Enable="True" />
```

The demo also shows fluent C# animation helpers:

```csharp
border.Animate(WidthProperty)
    .From(50).To(300)
    .WithDuration(TimeSpan.FromSeconds(1.2))
    .Start();
```

Other available chain operations in the demo include `WithEasing`, `WithDelay`, `ContinueWith`, `And`, `Then`, `Repeat`, `RepeatForever`, and `RunAsync`.

For Say To Any, prefer Avalonia `Transitions` for simple UI smoothing and timers/ViewModel state for waveform/progress movement. Use Suki animation helpers when a Suki-native interaction needs it.

## Adaptation Checklist

- Copy the smallest relevant Suki pattern, not the whole demo page.
- Keep `x:DataType` and compile-safe bindings.
- Add the correct `xmlns:suki` and, when needed, `xmlns:animations`.
- Include managers/hosts before using Dialog or Toast builders.
- Keep visual density aligned with `src/pc`: compact layout, restrained shadows, blue primary action, Chinese copy where appropriate.
- Validate with `dotnet build src/pc/pc.csproj`.
