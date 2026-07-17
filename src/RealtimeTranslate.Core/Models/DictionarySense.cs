namespace RealtimeTranslate.Core.Models;

public sealed record DictionarySense(
    string PartOfSpeech,
    string ShortDefinition,
    IReadOnlyList<string> EnglishDefinitions,
    IReadOnlyList<string>? EnglishExamples = null);
