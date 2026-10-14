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

        // OCR often splices HUD counters or menu labels into a subtitle crop
        // (for example, "exp 01 e arms"). Digits become unsafe when they are
        // coupled with the isolated-letter debris that marks this failure;
        // ordinary desktop prose containing a number remains usable.
        if (context.Any(char.IsDigit)
            && tokens.Any(token => token.Length == 1 && !AllowedSingle.Contains(token)))
        {
            return false;
        }

        // OCR crops commonly split words into one-character debris ("s a e")
        // or emit isolated two-character noise. Keep the gate conservative:
        // ordinary function words (a, an, am, I, to, of, ...) are allowed.
        // A lone unexpected letter ("th slender", "s a e") is not useful
        // sentence context.  More importantly, letting it pass merely because
        // a long noisy crop contains enough ordinary words can let a nearby
        // keyword select the main sense.  This is deliberately a hard reject:
        // the surrounding subtitle is optional, whereas a wrong main gloss is
        // not recoverable at a glance.
        if (tokens.Any(token => token.Length == 1 && !AllowedSingle.Contains(token)
            || token.Length == 2 && !AllowedShort.Contains(token)))
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
