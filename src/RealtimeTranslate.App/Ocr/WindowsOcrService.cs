using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Ocr;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;

namespace RealtimeTranslate.App.Ocr;

public sealed class WindowsOcrService : IOcrService
{
    private readonly OcrEngine? _engine = OcrEngine.TryCreateFromUserProfileLanguages();

    public async Task<IReadOnlyList<OcrCandidate>> RecognizeAsync(byte[] imagePng, CancellationToken cancellationToken = default)
    {
        if (_engine is null || imagePng.Length == 0)
        {
            return [];
        }

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

        var result = await _engine.RecognizeAsync(bitmap).AsTask(cancellationToken);
        return result.Lines
            .SelectMany(line => line.Words)
            .Select(word => new OcrCandidate(word.Text, 1.0))
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.Text))
            .ToArray();
    }
}
