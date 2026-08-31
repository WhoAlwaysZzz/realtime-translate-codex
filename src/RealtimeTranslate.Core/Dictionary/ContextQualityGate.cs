using System.Text.RegularExpressions;

namespace RealtimeTranslate.Core.Dictionary;

/// <summary>Conservative guard against fragmentary OCR steering sense ranking.</summary>
public static partial class ContextQualityGate
{
    public static bool IsSafe(string context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return false;
        }

        var tokens = Token().Matches(context).Select(m => m.Value).ToArray();
        if (tokens.Length < 3)
        {
            return false;
        }

        // OCR crops commonly split words into one-character debris ("s a e")
        // or emit isolated two-character noise. Keep the gate conservative:
        // ordinary function words (a, an, am, I, to, of, ...) are allowed.
        var suspicious = tokens.Count(token => token.Length == 1 && !AllowedSingle.Contains(token)
            || token.Length == 2 && !AllowedShort.Contains(token));
        if (suspicious > 0 && suspicious * 5 > tokens.Length)
        {
            return false;
        }

        return true;
    }

    private static readonly HashSet<string> AllowedSingle = new(StringComparer.OrdinalIgnoreCase) { "a", "I" };
    private static readonly HashSet<string> AllowedShort = new(StringComparer.OrdinalIgnoreCase)
    {
        "am", "an", "as", "at", "be", "by", "do", "go", "he", "if", "in", "is", "it", "me", "my",
        "no", "of", "on", "or", "so", "to", "up", "us", "we",
    };

    [GeneratedRegex("[A-Za-z]+(?:'[A-Za-z]+)?")]
    private static partial Regex Token();
}
