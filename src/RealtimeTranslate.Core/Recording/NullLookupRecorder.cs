using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Recording;

/// <summary>
/// Lookup recorder used by replay and other non-persistent runs.
/// </summary>
public sealed class NullLookupRecorder : ILookupRecorder
{
    public void Record(LookupRecord record)
    {
        // Intentionally discard replay records so production history is unchanged.
    }
}
