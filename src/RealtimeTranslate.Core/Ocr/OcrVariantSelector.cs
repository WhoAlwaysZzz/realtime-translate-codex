using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Ocr;

/// <summary>Chooses a stable OCR variant without treating first non-empty output as success.</summary>
public static partial class OcrVariantSelector
{
    public static IReadOnlyList<OcrCandidate> Select(IReadOnlyList<IReadOnlyList<OcrCandidate>> variants)
    {
        var nonEmpty = variants
            .Where(variant => variant.Any(candidate => !string.IsNullOrWhiteSpace(candidate.Text)))
            .ToArray();
        if (nonEmpty.Length == 0)
        {
            return [];
        }

        var first = nonEmpty[0];
        var firstScore = UsableTokenCount(first);
        var best = nonEmpty
            .Select((variant, index) => new RankedVariant(variant, UsableTokenCount(variant), index))
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Index)
            .First();

        // A tight word crop normally has one meaningful token in every
        // variant. Do not replace that known-good first result on a marginal
        // score difference. Context OCR needs a clear extra sentence token
        // before another preprocessing result can take over.
        return best.Score >= firstScore + 2 ? best.Candidates : first;
    }

    private static int UsableTokenCount(IReadOnlyList<OcrCandidate> candidates) => candidates.Count(candidate =>
        WordToken().IsMatch(candidate.Text.Trim()) && candidate.Text.Trim().Length >= 2);

    private sealed record RankedVariant(IReadOnlyList<OcrCandidate> Candidates, int Score, int Index);

    [GeneratedRegex("^[A-Za-z]+(?:'[A-Za-z]+)?$")]
    private static partial Regex WordToken();
}
