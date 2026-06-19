using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.History;

public interface IRecentLookupStore
{
    void Add(LookupPresentation presentation);

    IReadOnlyList<LookupPresentation> GetRecent(int count);

    string? CompletePrefix(string prefix);
}
