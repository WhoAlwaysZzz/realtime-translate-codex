using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Ocr;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace RealtimeTranslate.App.Ocr;

public sealed class WindowsOcrService : IOcrService
{
    private static readonly TimeSpan VariantTimeout = TimeSpan.FromSeconds(5);
    private readonly OcrEngine? _engine = CreateEngine();

    public async Task<IReadOnlyList<OcrCandidate>> RecognizeAsync(byte[] imagePng, CancellationToken cancellationToken = default)
    {
        var engine = _engine;
        if (engine is null)
        {
            throw new InvalidOperationException("Windows OCR engine is unavailable. Install English OCR language support in Windows language settings.");
        }

        if (imagePng.Length == 0)
        {
            return [];
        }

        var allCandidates = new List<OcrCandidate>();
        foreach (var variant in CreateImageVariants(imagePng))
        {
            IReadOnlyList<OcrCandidate> candidates;
            try
            {
                candidates = await RecognizeVariantAsync(engine, variant, cancellationToken)
                    .WaitAsync(VariantTimeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                // A malformed or unusually large crop must not stall the whole lookup.
                continue;
            }
            AddUniqueCandidates(allCandidates, candidates);
            if (allCandidates.Count > 0)
            {
                return allCandidates;
            }
        }

        return allCandidates;
    }

    private static async Task<IReadOnlyList<OcrCandidate>> RecognizeVariantAsync(
        OcrEngine engine,
        byte[] imagePng,
        CancellationToken cancellationToken)
    {
        using var randomAccessStream = new InMemoryRandomAccessStream();
        using (var writer = new DataWriter(randomAccessStream))
        {
            writer.WriteBytes(imagePng);
            await writer.StoreAsync().AsTask(cancellationToken);
            await writer.FlushAsync().AsTask(cancellationToken);
            writer.DetachStream();
        }

        randomAccessStream.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(randomAccessStream).AsTask(cancellationToken);
        using var bitmap = await decoder
            .GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied)
            .AsTask(cancellationToken);

        var result = await engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
        return result.Lines
            .SelectMany(line => line.Words)
            .Select(word => new OcrCandidate(word.Text, 1.0))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Text))
            .ToArray();
    }

    private static IReadOnlyList<byte[]> CreateImageVariants(byte[] imagePng)
    {
        using var sourceStream = new MemoryStream(imagePng);
        using var source = new Bitmap(sourceStream);

        var variants = new List<byte[]> { imagePng };
        using var scaled = Scale(source, 3);
        variants.Add(ToPngBytes(scaled));

        using var binary = BinarizeForOcr(source);
        using var binaryScaled = Scale(binary, 4);
        variants.Add(ToPngBytes(binaryScaled));

        return variants;
    }

    private static Bitmap Scale(Bitmap source, int scale)
    {
        var bitmap = new Bitmap(Math.Max(1, source.Width * scale), Math.Max(1, source.Height * scale), PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.White);
        graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
        graphics.PixelOffsetMode = PixelOffsetMode.Half;
        graphics.DrawImage(source, new Rectangle(0, 0, bitmap.Width, bitmap.Height));
        return bitmap;
    }

    private static Bitmap BinarizeForOcr(Bitmap source)
    {
        var background = EstimateBorderLuminance(source);
        var darkBackground = background < 128;
        var threshold = darkBackground
            ? Math.Min(245, background + 35)
            : Math.Max(10, background - 35);

        var bitmap = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (var y = 0; y < source.Height; y++)
        {
            for (var x = 0; x < source.Width; x++)
            {
                var pixel = source.GetPixel(x, y);
                var luminance = GetLuminance(pixel);
                var isText = darkBackground ? luminance > threshold : luminance < threshold;
                bitmap.SetPixel(x, y, isText ? Color.Black : Color.White);
            }
        }

        return bitmap;
    }

    private static double EstimateBorderLuminance(Bitmap source)
    {
        var total = 0.0;
        var count = 0;
        for (var x = 0; x < source.Width; x++)
        {
            total += GetLuminance(source.GetPixel(x, 0));
            total += GetLuminance(source.GetPixel(x, source.Height - 1));
            count += 2;
        }

        for (var y = 1; y < source.Height - 1; y++)
        {
            total += GetLuminance(source.GetPixel(0, y));
            total += GetLuminance(source.GetPixel(source.Width - 1, y));
            count += 2;
        }

        return count == 0 ? 255 : total / count;
    }

    private static double GetLuminance(Color color)
    {
        return color.R * 0.299 + color.G * 0.587 + color.B * 0.114;
    }

    private static byte[] ToPngBytes(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
        bitmap.Save(stream, ImageFormat.Png);
        return stream.ToArray();
    }

    private static void AddUniqueCandidates(List<OcrCandidate> target, IReadOnlyList<OcrCandidate> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (!target.Any(existing => string.Equals(existing.Text, candidate.Text, StringComparison.OrdinalIgnoreCase)))
            {
                target.Add(candidate);
            }
        }
    }

    private static OcrEngine? CreateEngine()
    {
        var englishLanguage = OcrEngine.AvailableRecognizerLanguages
            .FirstOrDefault(language => language.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase));
        if (englishLanguage is not null)
        {
            return OcrEngine.TryCreateFromLanguage(englishLanguage);
        }

        var enUs = new Language("en-US");
        if (OcrEngine.IsLanguageSupported(enUs))
        {
            return OcrEngine.TryCreateFromLanguage(enUs);
        }

        return OcrEngine.TryCreateFromUserProfileLanguages();
    }
}
