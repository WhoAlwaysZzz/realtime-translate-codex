using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Explanation;

public interface ITextExplanationService
{
    Task<TextExplanation> ExplainAsync(string sourceText, CancellationToken cancellationToken = default);
}
