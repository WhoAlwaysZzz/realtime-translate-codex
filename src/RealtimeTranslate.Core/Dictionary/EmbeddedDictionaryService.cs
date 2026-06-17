using RealtimeTranslate.Core.Models;

namespace RealtimeTranslate.Core.Dictionary;

public sealed class EmbeddedDictionaryService : IDictionaryService
{
    private readonly Dictionary<string, string[]> _entries;

    public EmbeddedDictionaryService(string? tsvPath = null)
    {
        _entries = CreateSeedEntries();
        if (!string.IsNullOrWhiteSpace(tsvPath) && File.Exists(tsvPath))
        {
            LoadTsv(tsvPath);
        }
    }

    public DictionaryLookupResult Lookup(NormalizedWord word)
    {
        foreach (var candidate in word.Candidates.Where(c => !string.IsNullOrWhiteSpace(c)))
        {
            if (_entries.TryGetValue(candidate, out var definitions))
            {
                return new DictionaryLookupResult(true, candidate, definitions);
            }
        }

        var fallback = string.IsNullOrWhiteSpace(word.Lemma) ? word.DisplayWord : word.Lemma;
        return DictionaryLookupResult.NotFound(fallback);
    }

    private static Dictionary<string, string[]> CreateSeedEntries()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["guess"] = ["猜测", "推测", "估计"],
            ["child"] = ["儿童", "孩子"],
            ["run"] = ["跑", "运行", "流动"],
            ["future"] = ["未来", "将来"],
            ["return"] = ["返回", "归还", "重新出现"],
            ["time"] = ["时间", "时刻"],
            ["word"] = ["单词", "话语"],
            ["memory"] = ["记忆", "回忆"],
            ["dream"] = ["梦", "梦想"],
            ["story"] = ["故事"],
            ["night"] = ["夜晚"],
            ["light"] = ["光", "光线", "轻的"],
            ["world"] = ["世界"],
            ["look"] = ["看", "寻找"],
            ["see"] = ["看见", "理解"],
            ["go"] = ["去", "进行"],
            ["be"] = ["是", "存在"],
            ["make"] = ["制作", "使得"],
            ["take"] = ["拿取", "带走"],
            ["know"] = ["知道", "了解"],
            ["think"] = ["思考", "认为"],
            ["want"] = ["想要"],
            ["need"] = ["需要"],
            ["open"] = ["打开", "开放的"],
            ["close"] = ["关闭", "接近的"],
            ["silent"] = ["沉默的", "安静的"],
            ["strange"] = ["奇怪的", "陌生的"],
            ["reverse"] = ["反转", "颠倒"],
        };
    }

    private void LoadTsv(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            {
                continue;
            }

            var parts = line.Split('\t', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2 || parts[0].Length == 0 || parts[1].Length == 0)
            {
                continue;
            }

            _entries[parts[0]] = parts[1]
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
