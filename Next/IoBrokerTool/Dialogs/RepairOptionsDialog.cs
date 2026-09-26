using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed record RepairOptions(
    bool BaseRepair,
    bool UpdateNode,
    bool UpdateController,
    bool RebuildNativeModules,
    bool UpdateAdapters)
{
    public IEnumerable<string> SelectedLabels()
    {
        if (BaseRepair) yield return "Basisreparatur";
        if (UpdateNode) yield return "Node.js aktualisieren";
        if (UpdateController) yield return "JS-Controller aktualisieren";
        if (RebuildNativeModules) yield return "Native Module neu bauen";
        if (UpdateAdapters) yield return "Adapter aktualisieren";
    }
}

public sealed class RepairOptionsDialog : Window
{
    private readonly CheckBox _baseRepair;
    private readonly CheckBox _updateNode;
    private readonly CheckBox _updateController;
    private readonly CheckBox _rebuildNativeModules;
    private readonly CheckBox _updateAdapters;
    private readonly TextBlock _validation;

    public RepairOptionsDialog()
    {
        Title = "Reparaturstufen auswählen";
        Width = 610;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        _baseRepair = CreateOption(
            "Basisreparatur",
            "Offiziellen ioBroker-Fixer ausführen und Besitzerrechte kontrollieren.");
        _updateNode = CreateOption(
            "Node.js aktualisieren",
            "Aktive Hauptversion mit ioBrokers offizieller Empfehlung abgleichen.");
        _updateController = CreateOption(
            "JS-Controller aktualisieren",
            "ioBroker JS-Controller reparieren beziehungsweise aktualisieren.");
        _rebuildNativeModules = CreateOption(
            "Native Module neu bauen",
            "Native npm-Module passend zur aktiven Node.js-Version neu kompilieren.");
        _updateAdapters = CreateOption(
            "Adapter aktualisieren",
            "Repository aktualisieren und verfügbare Adapterupdates installieren.");

        _validation = new TextBlock
        {
            Text = "",
            Foreground = Brush.Parse("#FFB86B"),
            TextWrapping = TextWrapping.Wrap
        };

        var cancel = new Button { Content = "Abbrechen", Classes = { "secondary" } };
        cancel.Click += (_, _) => Close(null);
        var start = new Button
        {
            Content = "Auswahl starten",
            Classes = { "danger" }
        };
        start.Click += (_, _) => Complete();

        Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 15,
            Children =
            {
                new TextBlock
                {
                    Text = "Was soll ausgeführt werden?",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                new TextBlock
                {
                    Text = "Alle Stufen sind vorausgewählt und entsprechen dem vollständigen bisherigen Reparaturlauf. " +
                           "Nicht benötigte Stufen kannst du abwählen.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#B6C2D2"),
                    LineHeight = 22
                },
                _baseRepair,
                _updateNode,
                _updateController,
                _rebuildNativeModules,
                _updateAdapters,
                _validation,
                new TextBlock
                {
                    Text = "Systemdiagnose, kontrolliertes Stoppen, Endkontrolle und Wiederstart werden immer ausgeführt.",
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brush.Parse("#8FA1B8")
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 10,
                    Children = { cancel, start }
                }
            }
        };
    }

    private static CheckBox CreateOption(string title, string description)
    {
        var option = new CheckBox
        {
            Content = title,
            IsChecked = true,
            Foreground = Brushes.White,
            FontSize = 16
        };
        ToolTip.SetTip(option, description);
        return option;
    }

    private void Complete()
    {
        var result = new RepairOptions(
            _baseRepair.IsChecked == true,
            _updateNode.IsChecked == true,
            _updateController.IsChecked == true,
            _rebuildNativeModules.IsChecked == true,
            _updateAdapters.IsChecked == true);
        if (!result.SelectedLabels().Any())
        {
            _validation.Text = "Bitte mindestens eine Reparaturstufe auswählen.";
            return;
        }
        Close(result);
    }
}
