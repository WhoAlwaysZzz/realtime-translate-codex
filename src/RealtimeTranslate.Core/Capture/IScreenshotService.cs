using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Capture;

public interface IScreenshotService
{
    Task<CapturedSelection> CaptureAsync(
        ScreenSelection selection,
        bool saveScreenshot = true,
        CancellationToken cancellationToken = default);
}
