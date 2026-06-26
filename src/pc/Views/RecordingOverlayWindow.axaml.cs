using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace pc.Views;

public partial class RecordingOverlayWindow : Window
{
    private const int BottomMargin = 34;
    private const double DefaultWidth = 220;
    private const double DefaultHeight = 48;

    public RecordingOverlayWindow()
    {
        InitializeComponent();
        Opened += (_, _) => PositionNearBottomCenter();
    }

    public void PositionNearBottomCenter(Window? referenceWindow = null)
    {
        PositionNearBottomCenterCore(referenceWindow);
        Dispatcher.UIThread.Post(() => PositionNearBottomCenterCore(referenceWindow), DispatcherPriority.Loaded);
    }

    private void PositionNearBottomCenterCore(Window? referenceWindow)
    {
        var screen = referenceWindow is not null
            ? Screens.ScreenFromWindow(referenceWindow)
            : Screens.Primary;

        screen ??= Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workingArea = screen.WorkingArea;
        var scale = screen.Scaling;
        var width = ResolveDimension(Bounds.Width, DesiredSize.Width, Width, DefaultWidth);
        var height = ResolveDimension(Bounds.Height, DesiredSize.Height, Height, DefaultHeight);
        var pixelWidth = (int)Math.Round(width * scale);
        var pixelHeight = (int)Math.Round(height * scale);
        var bottomMargin = (int)Math.Round(BottomMargin * scale);

        Position = new PixelPoint(
            workingArea.X + (workingArea.Width - pixelWidth) / 2,
            workingArea.Y + workingArea.Height - pixelHeight - bottomMargin);
    }

    private static double ResolveDimension(double actual, double desired, double configured, double fallback)
    {
        if (IsUsableDimension(actual))
        {
            return actual;
        }

        if (IsUsableDimension(desired))
        {
            return desired;
        }

        return IsUsableDimension(configured) ? configured : fallback;
    }

    private static bool IsUsableDimension(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    }
}
