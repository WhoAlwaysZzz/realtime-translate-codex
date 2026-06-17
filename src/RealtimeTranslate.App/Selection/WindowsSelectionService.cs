using RealtimeTranslate.Core.Models;
using RealtimeTranslate.Core.Selection;

namespace RealtimeTranslate.App.Selection;

public sealed class WindowsSelectionService : ISelectionService
{
    public async Task<ScreenSelection?> SelectAsync(CancellationToken cancellationToken = default)
    {
        var overlay = new SelectionOverlayWindow();
        await using var registration = cancellationToken.Register(() =>
        {
            overlay.Dispatcher.Invoke(() => overlay.Close());
        });

        overlay.Show();
        var selection = await overlay.Completion;
        if (selection is not null)
        {
            await Task.Delay(120, cancellationToken);
        }

        return selection;
    }
}
