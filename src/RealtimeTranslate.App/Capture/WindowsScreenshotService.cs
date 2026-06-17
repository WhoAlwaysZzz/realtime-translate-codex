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
    private readonly string _screenshotsDirectory;

    public WindowsScreenshotService(string archiveRoot)
    {
        _screenshotsDirectory = Path.Combine(archiveRoot, "screenshots");
    }

    public Task<CapturedSelection> CaptureAsync(ScreenSelection selection, CancellationToken cancellationToken = default)
    {
        if (selection.IsEmpty)
        {
            throw new ArgumentException("Selection cannot be empty.", nameof(selection));
        }

        Directory.CreateDirectory(_screenshotsDirectory);
        var bounds = Forms.SystemInformation.VirtualScreen;
        using var full = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb);
        using (var graphics = Graphics.FromImage(full))
        {
            graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size, CopyPixelOperation.SourceCopy);
        }

        var screenshotPath = Path.Combine(_screenshotsDirectory, $"{DateTimeOffset.Now:yyyyMMdd-HHmmss-fff}.png");
        full.Save(screenshotPath, ImageFormat.Png);

        var cropRect = ClampSelectionToBounds(selection, bounds, CropPaddingPixels);
        using var cropped = full.Clone(cropRect, PixelFormat.Format32bppArgb);
        using var stream = new MemoryStream();
        cropped.Save(stream, ImageFormat.Png);

        return Task.FromResult(new CapturedSelection(selection, screenshotPath, stream.ToArray()));
    }

    private static Rectangle ClampSelectionToBounds(ScreenSelection selection, Rectangle bounds, int padding)
    {
        var x = Math.Clamp(selection.X - bounds.Left - padding, 0, bounds.Width - 1);
        var y = Math.Clamp(selection.Y - bounds.Top - padding, 0, bounds.Height - 1);
        var right = Math.Clamp(selection.X + selection.Width - bounds.Left + padding, x + 1, bounds.Width);
        var bottom = Math.Clamp(selection.Y + selection.Height - bounds.Top + padding, y + 1, bounds.Height);
        return Rectangle.FromLTRB(x, y, right, bottom);
    }
}
