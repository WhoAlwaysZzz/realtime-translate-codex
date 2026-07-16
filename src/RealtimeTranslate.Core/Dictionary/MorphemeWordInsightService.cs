using System.Text.Json;
using System.Text.RegularExpressions;
using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed partial class MorphemeWordInsightService : IWordInsightService
{
    private readonly Dictionary<string, List<MorphemeGroup>> _groupsByExample = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, WordInsight> _curated = new(StringComparer.OrdinalIgnoreCase);

    public MorphemeWordInsightService(string morphemesJsonPath, string? curatedTsvPath = null)
    {
        LoadMorphemes(morphemesJsonPath);
        if (!string.IsNullOrWhiteSpace(curatedTsvPath) && File.Exists(curatedTsvPath))
        {
            LoadCurated(curatedTsvPath);
        }
    }

    public WordInsight Lookup(string word)
    {
        var key = word.Trim().ToLowerInvariant();
        if (key.Length == 0)
        {
            return WordInsight.Empty;
        }

        if (_curated.TryGetValue(key, out var curated))
        {
            return curated;
        }

        if (!_groupsByExample.TryGetValue(key, out var groups))
        {
            return WordInsight.Empty;
        }

        var parts = groups
            .Select(group => MatchPart(key, group))
            .Where(part => part is not null)
            .Cast<MatchedPart>()
            .DistinctBy(part => (part.Start, part.Label, part.Meaning))
            .OrderBy(part => part.Start)
            .ThenByDescending(part => part.Label.Length)
            .ToArray();

        var family = groups
            .SelectMany(group => group.Examples)
            .Where(example => !example.Equals(key, StringComparison.OrdinalIgnoreCase))
            .Where(example => SimpleWord().IsMatch(example))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(example => example.Length)
            .ThenBy(example => example, StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToArray();

        var themes = groups
            .Select(group => group.Theme)
            .Where(theme => !string.IsNullOrWhiteSpace(theme))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .Select(theme => theme.Replace('_', ' '))
            .ToArray();

        return new WordInsight(
            string.Join(" + ", parts.Select(part => $"{part.Label}({part.Meaning})")),
            family,
            themes.Length == 0 ? string.Empty : $"often relates to: {string.Join(", ", themes)}");
    }

    private void LoadMorphemes(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        using var document = JsonDocument.Parse(File.ReadAllText(path));
        foreach (var property in document.RootElement.EnumerateObject())
        {
            var element = property.Value;
            var meanings = ReadStrings(element, "meaning");
            var examples = ReadStrings(element, "examples")
                .Select(example => example.ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (meanings.Count == 0 || examples.Length == 0 || !element.TryGetProperty("forms", out var formsElement))
            {
                continue;
            }

            var forms = new List<MorphemeForm>();
            foreach (var form in formsElement.EnumerateArray())
            {
                var text = ReadString(form, "form").ToLowerInvariant();
                var location = ReadString(form, "loc").ToLowerInvariant();
                if (text.Length == 0 || location.Length == 0)
                {
                    continue;
                }

                var label = ReadString(form, "root");
                forms.Add(new MorphemeForm(text, label.Length == 0 ? text : label, location));
            }

            if (forms.Count == 0)
            {
                continue;
            }

            var group = new MorphemeGroup(forms, meanings, examples, ReadString(element, "theme"));
            foreach (var example in examples)
            {
                if (!_groupsByExample.TryGetValue(example, out var indexedGroups))
                {
                    indexedGroups = [];
                    _groupsByExample[example] = indexedGroups;
                }

                indexedGroups.Add(group);
            }
        }
    }

    private void LoadCurated(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var fields = line.Split('\t');
            if (fields.Length != 4)
            {
                continue;
            }

            var word = fields[0].Trim();
            if (word.Length == 0)
            {
                continue;
            }

            _curated[word] = new WordInsight(
                fields[1].Trim(),
                fields[2].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                fields[3].Trim());
        }
    }

    private static MatchedPart? MatchPart(string word, MorphemeGroup group)
    {
        return group.Forms
            .Select(form => MatchForm(word, form, group.Meanings))
            .Where(part => part is not null)
            .Cast<MatchedPart>()
            .OrderByDescending(part => part.Length)
            .ThenBy(part => part.Start)
            .FirstOrDefault();
    }

    private static MatchedPart? MatchForm(string word, MorphemeForm form, IReadOnlyList<string> meanings)
    {
        var start = form.Location switch
        {
            "prefix" when word.StartsWith(form.Text, StringComparison.OrdinalIgnoreCase) => 0,
            "suffix" when word.EndsWith(form.Text, StringComparison.OrdinalIgnoreCase) => word.Length - form.Text.Length,
            "embedded" => word.IndexOf(form.Text, StringComparison.OrdinalIgnoreCase),
            _ => -1,
        };
        if (start < 0)
        {
            return null;
        }

        return new MatchedPart(start, form.Text.Length, form.Label, string.Join("/", meanings.Take(2)));
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value) || value.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString() ?? string.Empty)
            .Where(item => item.Length > 0)
            .ToArray();
    }

    private static string ReadString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    [GeneratedRegex("^[A-Za-z][A-Za-z-]*$")]
    private static partial Regex SimpleWord();

    private sealed record MorphemeGroup(
        IReadOnlyList<MorphemeForm> Forms,
        IReadOnlyList<string> Meanings,
        IReadOnlyList<string> Examples,
        string Theme);

    private sealed record MorphemeForm(string Text, string Label, string Location);

    private sealed record MatchedPart(int Start, int Length, string Label, string Meaning);
}
