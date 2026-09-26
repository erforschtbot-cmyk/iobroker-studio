using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public enum RestoreSource
{
    Cancel,
    LinuxCheckpoint,
    LocalFile
}

public sealed class RestoreChoiceDialog : Window
{
    public RestoreChoiceDialog()
    {
        Title = "Wiederherstellung auswählen";
        Width = 540;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        var linux = new Button
        {
            Content = "Linux-Checkpoint verwenden",
            Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(18, 13)
        };
        ToolTip.SetTip(
            linux,
            "Verwendet den Checkpoint, der bereits auf dem Linux-Rechner liegt. Das ist der schnellste Rückweg.");
        linux.Click += (_, _) => Close(RestoreSource.LinuxCheckpoint);

        var file = new Button
        {
            Content = "Backup-Datei auswählen",
            Classes = { "secondary" },
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(18, 13)
        };
        ToolTip.SetTip(
            file,
            "Öffnet die Dateiauswahl und lädt eine zuvor unter Windows gespeicherte Sicherung auf Linux hoch.");
        file.Click += (_, _) => Close(RestoreSource.LocalFile);

        var cancel = new Button
        {
            Content = "Abbrechen",
            Classes = { "secondary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        ToolTip.SetTip(cancel, "Schließt die Auswahl, ohne ioBroker zu verändern.");
        cancel.Click += (_, _) => Close(RestoreSource.Cancel);

        Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 16,
            Children =
            {
                new TextBlock
                {
                    Text = "Wie möchtest du zurücksetzen?",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = "Der Linux-Checkpoint ist der schnelle Weg ohne Upload. " +
                           "Alternativ kannst du eine zuvor heruntergeladene Backup-Datei verwenden.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#B6C2D2"),
                    LineHeight = 22
                },
                linux,
                file,
                cancel
            }
        };
    }
}
