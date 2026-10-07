using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using MonoTorrent;
using MonoTorrent.Client;

namespace Torrentik;

public partial class MainWindow : Window
{
    readonly ObservableCollection<TorrentRow> rows = new();
    readonly Settings settings = Settings.Load();
    ClientEngine engine = null!;
    readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public MainWindow()
    {
        InitializeComponent();
        Grid.ItemsSource = rows;
        Loaded += async (_, _) => await InitAsync();
        Closing += (_, _) => SaveAll();
        timer.Tick += (_, _) => Tick();
        timer.Start();
    }

    EngineSettings BuildEngineSettings() => new EngineSettingsBuilder
    {
        CacheDirectory = Path.Combine(Settings.DataDir, "cache"),
        MaximumDownloadRate = settings.MaxDownKb * 1024,
        MaximumUploadRate = settings.MaxUpKb * 1024,
        AllowPortForwarding = true,
    }.ToSettings();

    async Task InitAsync()
    {
        Directory.CreateDirectory(Settings.DataDir);
        engine = new ClientEngine(BuildEngineSettings());
        foreach (var s in settings.Torrents.ToList())
        {
            try { await AddSavedAsync(s); }
            catch { settings.Torrents.Remove(s); }
        }
        // magnet из командной строки / файл по ассоциации
        var args = Environment.GetCommandLineArgs().Skip(1).ToArray();
        foreach (var a in args) await AddSourceAsync(a);
    }

    async Task AddSavedAsync(Settings.Saved s)
    {
        TorrentManager m = s.Source.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase)
            ? await engine.AddAsync(MagnetLink.Parse(s.Source), s.SaveDir)
            : await engine.AddAsync(await Torrent.LoadAsync(s.Source), s.SaveDir);
        rows.Add(new TorrentRow(m, s));
        if (!s.Paused) await m.StartAsync();
    }

    async Task AddSourceAsync(string source)
    {
        try
        {
            source = source.Trim();
            if (source.Length == 0) return;
            string stored = source;
            if (!source.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase))
            {
                // копия .torrent в папку программы, чтобы не зависеть от исходного файла
                var dir = Path.Combine(Settings.DataDir, "torrents");
                Directory.CreateDirectory(dir);
                stored = Path.Combine(dir, Path.GetFileName(source));
                if (!string.Equals(Path.GetFullPath(source), Path.GetFullPath(stored), StringComparison.OrdinalIgnoreCase))
                    File.Copy(source, stored, true);
            }
            var saved = new Settings.Saved { Source = stored, SaveDir = settings.DownloadDir };
            await AddSavedAsync(saved);
            settings.Torrents.Add(saved);
            settings.Save();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "Не получилось добавить:\n" + ex.Message, "Torrentik");
        }
    }

    void Tick()
    {
        foreach (var r in rows) r.Refresh();
        long d = rows.Sum(r => r.Manager.Monitor.DownloadRate);
        long u = rows.Sum(r => r.Manager.Monitor.UploadRate);
        StatusBar.Text = $"Торрентов: {rows.Count}   ↓ {TorrentRow.Fmt(d)}/с   ↑ {TorrentRow.Fmt(u)}/с   Папка: {settings.DownloadDir}";
    }

    void SaveAll()
    {
        foreach (var r in rows) r.Saved.Paused = r.Manager.State is TorrentState.Paused or TorrentState.Stopped;
        settings.Save();
    }

    IEnumerable<TorrentRow> Selected() => Grid.SelectedItems.Cast<TorrentRow>().ToList();

    async void AddFile_Click(object s, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "Torrent|*.torrent", Multiselect = true };
        if (dlg.ShowDialog(this) == true)
            foreach (var f in dlg.FileNames) await AddSourceAsync(f);
    }

    async void AddMagnet_Click(object s, RoutedEventArgs e)
    {
        var text = Clipboard.ContainsText() ? Clipboard.GetText() : "";
        var input = PromptWindow.Ask(this, "Вставь magnet-ссылку", text.StartsWith("magnet:") ? text : "");
        if (!string.IsNullOrWhiteSpace(input)) await AddSourceAsync(input);
    }

    async void Start_Click(object s, RoutedEventArgs e)
    {
        foreach (var r in Selected()) { r.Saved.Paused = false; await r.Manager.StartAsync(); }
    }

    async void Pause_Click(object s, RoutedEventArgs e)
    {
        foreach (var r in Selected()) { r.Saved.Paused = true; await r.Manager.PauseAsync(); }
    }

    async void Remove_Click(object s, RoutedEventArgs e)
    {
        var sel = Selected().ToList();
        if (sel.Count == 0) return;
        if (MessageBox.Show(this, "Убрать из списка? Скачанные файлы останутся на диске.", "Torrentik",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        foreach (var r in sel)
        {
            try { await r.Manager.StopAsync(); await engine.RemoveAsync(r.Manager); } catch { }
            rows.Remove(r);
            settings.Torrents.Remove(r.Saved);
        }
        settings.Save();
    }

    void OpenFolder_Click(object s, RoutedEventArgs e)
    {
        var r = Selected().FirstOrDefault();
        var dir = r?.Saved.SaveDir ?? settings.DownloadDir;
        if (Directory.Exists(dir)) Process.Start("explorer.exe", dir);
    }

    async void Settings_Click(object s, RoutedEventArgs e)
    {
        var w = new SettingsWindow(settings) { Owner = this };
        if (w.ShowDialog() == true)
        {
            settings.Save();
            await engine.UpdateSettingsAsync(BuildEngineSettings());
        }
    }

    async void Window_Drop(object s, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] files)
            foreach (var f in files.Where(f => f.EndsWith(".torrent", StringComparison.OrdinalIgnoreCase)))
                await AddSourceAsync(f);
        else if (e.Data.GetData(DataFormats.Text) is string t && t.StartsWith("magnet:"))
            await AddSourceAsync(t);
    }
}
