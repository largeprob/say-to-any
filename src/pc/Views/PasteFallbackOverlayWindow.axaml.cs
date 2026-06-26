using Avalonia;
using Avalonia.Controls;

namespace pc.Views;

public partial class PasteFallbackOverlayWindow : Window
{
    private const int BottomMargin = 34;

    public PasteFallbackOverlayWindow()
    {
        InitializeComponent();
        Opened += (_, _) => PositionNearBottomCenter();
    }

    public void PositionNearBottomCenter(Window? referenceWindow = null)
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
        var width = Bounds.Width > 0 ? Bounds.Width : Width;
        var height = Bounds.Height > 0 ? Bounds.Height : Height;
        var pixelWidth = (int)Math.Round(width * scale);
        var pixelHeight = (int)Math.Round(height * scale);
        var bottomMargin = (int)Math.Round(BottomMargin * scale);

        Position = new PixelPoint(
            workingArea.X + (workingArea.Width - pixelWidth) / 2,
            workingArea.Y + workingArea.Height - pixelHeight - bottomMargin);
    }
}
