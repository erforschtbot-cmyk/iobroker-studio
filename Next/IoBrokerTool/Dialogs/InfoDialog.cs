using Avalonia;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed class InfoDialog : Window
{
    public InfoDialog(string title, string message)
    {
        Title = title;
        Width = 620;
        MinWidth = 460;
        Height = 520;
        MinHeight = 300;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        // Der Meldungstext ist oft die einzige Fehlerquelle (Remote-Ausgabe,
        // Pfade, Exitcodes). Er steht deshalb in einer schreibgeschuetzten
        // TextBox: markierbar, per Strg+C kopierbar und scrollbar, statt in
        // einem TextBlock, aus dem nichts uebernommen werden kann.
        var textBox = new TextBox
        {
            Text = message,
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = FontFamily.Parse("Consolas, monospace"),
            FontSize = 13,
            [ScrollViewer.VerticalScrollBarVisibilityProperty] =
                Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            [ScrollViewer.HorizontalScrollBarVisibilityProperty] =
                Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };

        var copy = new Button
        {
            Content = "Text kopieren",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        ToolTip.SetTip(copy, "Kopiert den vollständigen Text in die Windows-Zwischenablage (Strg+C im Textfeld genügt ebenfalls).");
        var copyLabel = copy;
        copy.Click += async (_, _) =>
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null)
            {
                copyLabel.Content = "Zwischenablage nicht verfügbar";
                return;
            }
            await clipboard.SetTextAsync(message);
            copyLabel.Content = "Kopiert";
        };

        var close = new Button
        {
            Content = "Schließen",
            Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        ToolTip.SetTip(close, "Schließt dieses Informationsfenster.");
        close.Click += (_, _) => Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10,
            Children = { copy, close }
        };

        Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White,
                    TextWrapping = TextWrapping.Wrap
                },
                textBox,
                buttons
            }
        };
    }
}
