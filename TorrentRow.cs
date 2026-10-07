using System.ComponentModel;
using MonoTorrent.Client;

namespace Torrentik;

public class TorrentRow : INotifyPropertyChanged
{
    public TorrentManager Manager { get; }
    public Settings.Saved Saved { get; }

    public TorrentRow(TorrentManager m, Settings.Saved saved)
    {
        Manager = m;
        Saved = saved;
    }

    public string Name => string.IsNullOrWhiteSpace(Manager.Name) ? "Получаю метаданные…" : Manager.Name;
    public string Size => Manager.HasMetadata ? Fmt(Manager.Torrent!.Size) : "—";
    public double Progress => Manager.Progress;
    public string ProgressText => $"{Manager.Progress:0.0}%";
    public string Status => Manager.State switch
    {
        TorrentState.Downloading => "Скачивание",
        TorrentState.Seeding => "Раздача",
        TorrentState.Paused => "Пауза",
        TorrentState.Stopped => "Остановлен",
        TorrentState.Stopping => "Останавливается",
        TorrentState.Starting => "Запуск",
        TorrentState.Hashing => "Проверка",
        TorrentState.HashingPaused => "Проверка (пауза)",
        TorrentState.Metadata => "Метаданные",
        TorrentState.Error => "Ошибка",
        _ => Manager.State.ToString()
    };
    public string DownSpeed => Fmt(Manager.Monitor.DownloadRate) + "/с";
    public string UpSpeed => Fmt(Manager.Monitor.UploadRate) + "/с";
    public int Peers => Manager.Peers.Available;

    public event PropertyChangedEventHandler? PropertyChanged;
    public void Refresh() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));

    public static string Fmt(long bytes)
    {
        string[] u = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
        double v = bytes; int i = 0;
        while (v >= 1024 && i < u.Length - 1) { v /= 1024; i++; }
        return i == 0 ? $"{v:0} {u[i]}" : $"{v:0.0} {u[i]}";
    }
}
