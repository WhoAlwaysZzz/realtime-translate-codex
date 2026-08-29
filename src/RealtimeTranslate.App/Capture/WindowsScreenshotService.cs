using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Forms = System.Windows.Forms;
using RealtimeTranslate.Core.Capture;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.App.Capture;

public sealed class WindowsScreenshotService : IScreenshotService
{
    private const int CropPaddingPixels = 16;
    private const int ContextHorizontalPaddingPixels = 800;
    // Include nearby dialogue lines above/below a word selection. The desktop
    // fixture places its sentence roughly 100px below the target; 48px cropped
    // that line out of the context OCR input.
    private const int ContextVerticalPaddingPixels = 180;
    private readonly string _screenshotsDirectory;

    public WindowsScreenshotService(string archiveRoot)
    {
        _screenshotsDirectory = Path.Combine(archiveRoot, "screenshots");
    }

    public Task<CapturedSelection> CaptureAsync(
        ScreenSelection selection,
        bool saveScreenshot = true,
        CancellationToken cancellationToken = default)
    {
        if (selection.IsEmpty)
        {
            throw new ArgumentException("Selection cannot be empty.", nameof(selection));
        }

        var bounds = Forms.SystemInformation.VirtualScreen;
        using var full = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(full))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        var screenshotPath = string.Empty;
        if (saveScreenshot)
        {
            Directory.CreateDirectory(_screenshotsDirectory);
            screenshotPath = Path.Combine(_screenshotsDirectory, $"{DateTimeOffset.Now:yyyyMMdd-HHmmss-fff}.png");
            full.Save(screenshotPath, ImageFormat.Png);
        }

        var wordCrop = CropToPng(full, ClampSelectionToBounds(selection, bounds, CropPaddingPixels, CropPaddingPixels));
        var contextCrop = CropToPng(full, ClampSelectionToBounds(selection, bounds, ContextHorizontalPaddingPixels, ContextVerticalPaddingPixels));

        return Task.FromResult(new CapturedSelection(selection, screenshotPath, wordCrop, contextCrop));
    }

    private static byte[] CropToPng(Bitmap full, Rectangle rectangle)
    {
        using var cropped = full.Clone(rectangle, PixelFormat.Format32bppArgb);
        using var stream = new MemoryStream();
        cropped.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static Rectangle ClampSelectionToBounds(ScreenSelection selection, Rectangle bounds, int horizontalPadding, int verticalPadding)
    {
        var x = Math.Clamp(selection.X - bounds.Left - horizontalPadding, 0, bounds.Width - 1);
        var y = Math.Clamp(selection.Y - bounds.Top - verticalPadding, 0, bounds.Height - 1);
        var right = Math.Clamp(selection.X + selection.Width - bounds.Left + horizontalPadding, x + 1, bounds.Width);
        var bottom = Math.Clamp(selection.Y + selection.Height - bounds.Top + verticalPadding, y + 1, bounds.Height);
        return Rectangle.FromLTRB(x, y, right, bottom);
    }
}
