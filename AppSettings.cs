using System.IO;
using System.Text.Json;

namespace PrivacyShield;

internal sealed class AppSettings
{
    public string FiveMPath { get; set; } = "";
    public string Arguments { get; set; } = "-b3570 -pure_1 -binafk";
    public bool ClearCache { get; set; } = true;
    public bool BlockFingerprinting { get; set; } = true;
    public bool DisableWebRtc { get; set; } = true;
    public bool SkipIntro { get; set; } = false;

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "PrivacyShield", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath)) ?? new AppSettings();
        }
        catch
        {
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
        }
    }
}
