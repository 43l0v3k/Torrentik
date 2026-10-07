using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Torrentik;

public class PromptWindow : Window
{
    readonly TextBox box = new() { Margin = new Thickness(0, 8, 0, 12), Height = 28 };
    public string? Result;

    PromptWindow(string title, string initial)
    {
        Title = "Torrentik";
        Width = 520; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.FromRgb(0x1B, 0x1B, 0x1F));
        Foreground = Brushes.White;
        ResizeMode = ResizeMode.NoResize;
        var panel = new StackPanel { Margin = new Thickness(14) };
        panel.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White });
        box.Text = initial;
        box.Background = new SolidColorBrush(Color.FromRgb(0x2A, 0x2A, 0x32));
        box.Foreground = Brushes.White;
        panel.Children.Add(box);
        var ok = new Button { Content = "Добавить", Padding = new Thickness(16, 6, 16, 6), IsDefault = true, HorizontalAlignment = HorizontalAlignment.Right };
        ok.Click += (_, _) => { Result = box.Text; DialogResult = true; };
        panel.Children.Add(ok);
        Content = panel;
        Loaded += (_, _) => { box.Focus(); box.SelectAll(); };
    }

    public static string? Ask(Window owner, string title, string initial)
    {
        var w = new PromptWindow(title, initial) { Owner = owner };
        return w.ShowDialog() == true ? w.Result : null;
    }
}
