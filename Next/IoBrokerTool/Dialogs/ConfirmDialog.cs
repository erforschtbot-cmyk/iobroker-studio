using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed class ConfirmDialog : Window
{
    private bool _result;

    public ConfirmDialog(string title, string message, string confirmText, bool danger = false)
    {
        Title = title;
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        var cancel = new Button { Content = "Abbrechen", Classes = { "secondary" } };
        ToolTip.SetTip(cancel, "Schließt dieses Fenster, ohne die angezeigte Aktion auszuführen.");
        cancel.Click += (_, _) => Close(false);
        var confirm = new Button
        {
            Content = confirmText,
            Classes = { danger ? "danger" : "primary" }
        };
        ToolTip.SetTip(confirm, $"Bestätigt und startet: {confirmText}.");
        confirm.Click += (_, _) => Close(true);

        Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 18,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = message,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#B6C2D2"),
                    LineHeight = 22
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, confirm }
                }
            }
        };
    }

    public new async Task<bool> ShowDialog(Window owner)
    {
        _result = false;
        Closed += (_, _) => { };
        _result = await base.ShowDialog<bool>(owner);
        return _result;
    }
}
