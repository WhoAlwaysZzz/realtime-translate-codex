using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Ocr;

public interface IOcrService
{
    Task<IReadOnlyList<OcrCandidate>> RecognizeAsync(byte[] imagePng, CancellationToken cancellationToken = default);
}
