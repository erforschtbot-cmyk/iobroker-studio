using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed class InfoDialog : Window
{
    public InfoDialog(string title, string message)
    {
        Title = title;
        Width = 500;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        var close = new Button
        {
            Content = "Schließen",
            Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        ToolTip.SetTip(close, "Schließt dieses Informationsfenster.");
        close.Click += (_, _) => Close();
        Content = new StackPanel
        {
            Margin = new Avalonia.Thickness(26),
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
                close
            }
        };
    }
}
