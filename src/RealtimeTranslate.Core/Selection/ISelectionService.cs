using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Selection;

public interface ISelectionService
{
    Task<ScreenSelection?> SelectAsync(CancellationToken cancellationToken = default);
}
