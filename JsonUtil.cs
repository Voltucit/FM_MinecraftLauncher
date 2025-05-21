using System.IO;
using Newtonsoft.Json;

namespace 忘却的旋律_EP;

public static class JsonUtil
{
    private static readonly string FilePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, 
        ".minecraft/", 
        "LauncherSetting.json"
    );

    public static LauncherSettings Load()
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(FilePath))
        {
            return new LauncherSettings();
        }

        var json = File.ReadAllText(FilePath);
        return JsonConvert.DeserializeObject<LauncherSettings>(json) ?? new LauncherSettings();
    }

    public static void Save(LauncherSettings settings)
    {
        var directory = Path.GetDirectoryName(FilePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }
}