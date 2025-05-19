using System.IO;
using Newtonsoft.Json;


namespace 忘却的旋律_EP;

public static class JsonUtil
{
    private static readonly string FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LauncherSetting.json");

    public static LauncherSettings Load()
    {
        if (!File.Exists(FilePath))
        {
            // 如果文件不存在，则返回默认值
            return new LauncherSettings();
        }

        var json = File.ReadAllText(FilePath);
        
        
        return JsonConvert.DeserializeObject<LauncherSettings>(json) ?? new LauncherSettings();
    }

    public static void Save(LauncherSettings settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }
}