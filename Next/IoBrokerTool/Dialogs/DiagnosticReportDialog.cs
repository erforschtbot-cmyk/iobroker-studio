using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed class DiagnosticReportDialog : Window
{
    public DiagnosticReportDialog(string report)
    {
        Title = "ioBroker-Fehlerdiagnose";
        Width = 900;
        Height = 680;
        MinWidth = 680;
        MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        var close = new Button
        {
            Content = "Schließen",
            HorizontalAlignment = HorizontalAlignment.Right,
            Classes = { "primary" }
        };
        close.Click += (_, _) => Close();

        Content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"),
            RowSpacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = "Offizielle ioBroker-Diagnose",
                    FontSize = 23,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brush.Parse("#65E0BD")
                },
                new TextBlock
                {
                    [Grid.RowProperty] = 1,
                    Text = "Unveränderte Ausgabe des offiziellen Befehls „iob diag“. Die Standardauswahl wird automatisch mit Enter bestätigt.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#B6C2D2")
                },
                new TextBox
                {
                    [Grid.RowProperty] = 2,
                    Text = report,
                    IsReadOnly = true,
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    FontFamily = FontFamily.Parse("Consolas, monospace"),
                    FontSize = 13,
                    [ScrollViewer.VerticalScrollBarVisibilityProperty] = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
                    [ScrollViewer.HorizontalScrollBarVisibilityProperty] = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
                },
                new StackPanel
                {
                    [Grid.RowProperty] = 3,
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Children = { close }
                }
            }
        };
    }
}
