using System;
using System.IO;

namespace RealtimeTranslate.App;

public sealed record AppSettings(string ArchiveRoot)
{
    public static AppSettings CreateDefault()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RealtimeTranslateCodex");
        return new AppSettings(root);
    }
}
