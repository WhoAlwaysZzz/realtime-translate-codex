using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Normalization;

public interface IWordNormalizer
{
    NormalizedWord Normalize(string text);
}
