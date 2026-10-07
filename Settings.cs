using System.IO;
using System.Text.Json;

namespace Torrentik;

public class Settings
{
    public string DownloadDir { get; set; } =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    public int MaxDownKb { get; set; } = 0; // 0 = без лимита
    public int MaxUpKb { get; set; } = 0;
    public List<Saved> Torrents { get; set; } = new();

    public class Saved
    {
        public string Source { get; set; } = "";   // путь к .torrent или magnet
        public string SaveDir { get; set; } = "";
        public bool Paused { get; set; }
    }

    public static string DataDir { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Torrentik");

    static string FilePath => Path.Combine(DataDir, "settings.json");

    public static Settings Load()
    {
        try
        {
            if (File.Exists(FilePath))
                return JsonSerializer.Deserialize<Settings>(File.ReadAllText(FilePath)) ?? new Settings();
        }
        catch { }
        return new Settings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(DataDir);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { }
    }
}
