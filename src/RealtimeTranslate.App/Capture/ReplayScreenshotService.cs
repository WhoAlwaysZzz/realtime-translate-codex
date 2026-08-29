using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using RealtimeTranslate.Core.Capture;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.App.Capture;

/// <summary>Builds the same word/context crops as the live capture service from an archived PNG.</summary>
public sealed class ReplayScreenshotService
{
    private const int CropPaddingPixels = 16;
    private const int ContextHorizontalPaddingPixels = 800;
    private const int ContextVerticalPaddingPixels = 180;

    public Task<CapturedSelection> CaptureAsync(
        string screenshotPath,
        ScreenSelection selection,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!File.Exists(screenshotPath))
        {
            throw new FileNotFoundException("Archived screenshot was not found.", screenshotPath);
        }

        using var source = new Bitmap(screenshotPath);
        var bounds = new Rectangle(0, 0, source.Width, source.Height);
        var normalized = NormalizeSelection(selection, bounds);
        var word = CropToPng(source, Clamp(normalized, bounds, CropPaddingPixels, CropPaddingPixels));
        var contextRectangle = Clamp(normalized, bounds, ContextHorizontalPaddingPixels, ContextVerticalPaddingPixels);
        var context = CropContextToPng(source, contextRectangle, normalized);
        return Task.FromResult(new CapturedSelection(selection, screenshotPath, word, context));
    }

    private static ScreenSelection NormalizeSelection(ScreenSelection selection, Rectangle bounds)
    {
        if (selection.IsEmpty) throw new ArgumentException("Selection cannot be empty.", nameof(selection));
        if (selection.X < 0 || selection.Y < 0 || selection.X >= bounds.Width || selection.Y >= bounds.Height)
            throw new ArgumentOutOfRangeException(nameof(selection), "Selection origin is outside archived screenshot bounds.");
        return selection;
    }

    private static Rectangle Clamp(ScreenSelection selection, Rectangle bounds, int horizontal, int vertical)
    {
        var x = Math.Clamp(selection.X - horizontal, 0, bounds.Width - 1);
        var y = Math.Clamp(selection.Y - vertical, 0, bounds.Height - 1);
        var right = Math.Clamp(selection.X + selection.Width + horizontal, x + 1, bounds.Width);
        var bottom = Math.Clamp(selection.Y + selection.Height + vertical, y + 1, bounds.Height);
        return Rectangle.FromLTRB(x, y, right, bottom);
    }

    private static byte[] CropToPng(Bitmap source, Rectangle rectangle)
    {
        using var crop = source.Clone(rectangle, PixelFormat.Format32bppArgb);
        using var stream = new MemoryStream();
        crop.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static byte[] CropContextToPng(Bitmap source, Rectangle contextRectangle, ScreenSelection selection)
    {
        using var cropped = source.Clone(contextRectangle, PixelFormat.Format32bppArgb);
        var selected = new Rectangle(
            selection.X - contextRectangle.X,
            selection.Y - contextRectangle.Y,
            selection.Width,
            selection.Height);
        selected.Intersect(new Rectangle(Point.Empty, cropped.Size));
        if (!selected.IsEmpty)
        {
            using var graphics = Graphics.FromImage(cropped);
            graphics.FillRectangle(Brushes.White, selected);
        }

        using var stream = new MemoryStream();
        cropped.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }
}
