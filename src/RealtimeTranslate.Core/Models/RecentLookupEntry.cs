namespace RealtimeTranslate.Core.Models;

public sealed record RecentLookupEntry(DateTimeOffset OccurredAt, LookupPresentation Presentation);
