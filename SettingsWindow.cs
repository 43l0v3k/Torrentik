using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace Torrentik;

public class SettingsWindow : Window
{
    readonly TextBox dir = new() { Height = 28 };
    readonly TextBox down = new() { Width = 100, Height = 28 };
    readonly TextBox up = new() { Width = 100, Height = 28 };

    public SettingsWindow(Settings s)
    {
        Title = "Настройки";
        Width = 520; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x1B, 0x1F));
        Foreground = Brushes.White;
        ResizeMode = ResizeMode.NoResize;

        foreach (var t in new[] { dir, down, up })
        {
            t.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x32));
            t.Foreground = Brushes.White;
        }
        dir.Text = s.DownloadDir;
        down.Text = s.MaxDownKb.ToString();
        up.Text = s.MaxUpKb.ToString();

        var p = new StackPanel { Margin = new Thickness(14) };
        p.Children.Add(Label("Папка загрузки"));
        var row = new DockPanel { Margin = new Thickness(0, 4, 0, 12) };
        var browse = new Button { Content = "…", Width = 32, Margin = new Thickness(6, 0, 0, 0) };
        DockPanel.SetDock(browse, Dock.Right);
        browse.Click += (_, _) =>
        {
            var d = new OpenFolderDialog();
            if (d.ShowDialog() == true) dir.Text = d.FolderName;
        };
        row.Children.Add(browse);
        row.Children.Add(dir);
        p.Children.Add(row);

        p.Children.Add(Label("Лимит скачивания, КБ/с (0 — без лимита)"));
        down.Margin = new Thickness(0, 4, 0, 12); down.HorizontalAlignment = HorizontalAlignment.Left;
        p.Children.Add(down);
        p.Children.Add(Label("Лимит раздачи, КБ/с (0 — без лимита)"));
        up.Margin = new Thickness(0, 4, 0, 16); up.HorizontalAlignment = HorizontalAlignment.Left;
        p.Children.Add(up);

        var ok = new Button { Content = "Сохранить", Padding = new Thickness(16, 6, 16, 6), IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) =>
        {
            s.DownloadDir = dir.Text.Trim();
            s.MaxDownKb = int.TryParse(down.Text, out var d) && d > 0 ? d : 0;
            s.MaxUpKb = int.TryParse(up.Text, out var u) && u > 0 ? u : 0;
            DialogResult = true;
        };
        p.Children.Add(ok);
        Content = p;
    }

    static TextBlock Label(string t) => new() { Text = t, Foreground = Brushes.White };
}
