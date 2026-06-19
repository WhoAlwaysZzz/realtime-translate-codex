using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Ocr;

public sealed class OcrTextComposer
{
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SpaceBeforePunctuationPattern = new(@"\s+([,.;:!?%)\]\}])", RegexOptions.Compiled);
    private static readonly Regex SpaceAfterOpeningPunctuationPattern = new(@"([(\[\{])\s+", RegexOptions.Compiled);

    public string Compose(IReadOnlyList<OcrCandidate> candidates)
    {
        var text = string.Join(" ", candidates.Select(candidate => candidate.Text.Trim()).Where(text => text.Length > 0));
        text = WhitespacePattern.Replace(text, " ").Trim();
        text = SpaceBeforePunctuationPattern.Replace(text, "$1");
        text = SpaceAfterOpeningPunctuationPattern.Replace(text, "$1");
        return text;
    }
}
