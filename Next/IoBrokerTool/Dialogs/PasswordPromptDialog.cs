using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed class PasswordPromptDialog : Window
{
    private readonly TextBox _passwordBox;

    public PasswordPromptDialog(string username, string? hint = null)
    {
        Title = "Administrator-Passwort erforderlich";
        Width = 520;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        _passwordBox = new TextBox
        {
            PasswordChar = '●',
            PlaceholderText = "sudo-/doas-Passwort",
            FontSize = 16
        };
        ToolTip.SetTip(
            _passwordBox,
            "Hier das von Linux angeforderte sudo-/doas-Passwort eingeben. Die Eingabe wird nicht gespeichert.");
        _passwordBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                Submit();
                e.Handled = true;
            }
        };

        var cancel = new Button { Content = "Vorgang abbrechen", Classes = { "secondary" } };
        ToolTip.SetTip(cancel, "Bricht die laufende Remote-Aktion ab, ohne ein Passwort zu senden.");
        cancel.Click += (_, _) => Close(null);
        var submit = new Button { Content = "Weiter", Classes = { "primary" } };
        ToolTip.SetTip(submit, "Sendet das eingegebene Passwort ausschließlich an die laufende Linux-Abfrage.");
        submit.Click += (_, _) => Submit();

        Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Linux benötigt Administratorrechte",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text =
                        $"Das Zielsystem verlangt das sudo-/doas-Passwort für „{username}“. " +
                        "Die Eingabe wird nur an den laufenden SSH-Vorgang gesendet, nicht gespeichert " +
                        "und nicht im Protokoll angezeigt.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#B6C2D2"),
                    LineHeight = 22
                },
                _passwordBox,
                new TextBlock
                {
                    Text = hint ?? "",
                    IsVisible = !string.IsNullOrWhiteSpace(hint),
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#FF8A8A"),
                    LineHeight = 22
                },
                new TextBlock
                {
                    Text = "Hinweis: Bei Linux bleibt die Passworteingabe normalerweise vollständig unsichtbar.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#7F91A8"),
                    FontSize = 12
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, submit }
                }
            }
        };

        Opened += (_, _) => _passwordBox.Focus();
    }

    private void Submit()
    {
        if (!string.IsNullOrEmpty(_passwordBox.Text))
            Close(_passwordBox.Text);
    }

    public new Task<string?> ShowDialog(Window owner) =>
        base.ShowDialog<string?>(owner);
}
