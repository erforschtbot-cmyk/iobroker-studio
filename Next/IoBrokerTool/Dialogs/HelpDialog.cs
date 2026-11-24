using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace IoBrokerTool.Dialogs;

public sealed class HelpDialog : Window
{
    public HelpDialog()
    {
        Title = "Hilfe & Anleitung";
        Width = 720;
        Height = 680;
        MinWidth = 560;
        MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Brush.Parse("#111925");

        var close = new Button
        {
            Content = "Verstanden",
            Classes = { "primary" },
            HorizontalAlignment = HorizontalAlignment.Right
        };
        ToolTip.SetTip(close, "Schließt die Anleitung und kehrt zur Systemzentrale zurück.");
        close.Click += (_, _) => Close();

        var content = new StackPanel
        {
            Margin = new Thickness(28),
            Spacing = 18,
            Children =
            {
                new TextBlock
                {
                    Text = "Hilfe & Anleitung",
                    FontSize = 28,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brushes.White
                },
                Paragraph(
                    "Halte den Mauszeiger über eine Schaltfläche, um jederzeit eine kurze Erklärung " +
                    "anzuzeigen. Hier findest du zusätzlich den vollständigen Überblick."),
                Section("1. Verbindung",
                    "Trage IP-Adresse oder Hostname, Linux-Benutzer, SSH-Port und SSH-Passwort ein. " +
                    "„Verbinden“ prüft die Verbindung und lässt dich beim ersten Mal den eindeutigen " +
                    "Server-Fingerabdruck bestätigen. „Passwort merken“ speichert das SSH-Passwort " +
                    "verschlüsselt für dein Windows-Benutzerkonto."),
                Section("2. Checkpoint",
                    "Erstellt direkt auf dem ioBroker-Rechner eine schnelle Sicherung von " +
                    "iobroker-data und node_modules. Sie ist für den Fall gedacht: etwas ausprobieren, " +
                    "ioBroker geht kaputt, sofort auf den funktionierenden Zustand zurücksetzen. " +
                    "Ein neuer Checkpoint ersetzt den bisherigen. Dauerhaft existiert nur dieser eine Ordner."),
                Section("3. Häkchen „Zusätzlich nach Windows herunterladen“",
                    "Ist das Häkchen gesetzt, wird der neu erstellte Checkpoint zusätzlich als Datei " +
                    "auf den Windows-PC geladen. Ohne Häkchen bleibt er nur auf dem Linux-Rechner und " +
                    "ist dort trotzdem sofort für eine Wiederherstellung verfügbar."),
                Section("4. Wiederherstellen",
                    "Öffnet zwei Möglichkeiten: „Linux-Checkpoint“ nimmt die schnelle Sicherung direkt " +
                    "vom ioBroker-Rechner. „Backup-Datei“ lädt eine zuvor auf Windows gespeicherte " +
                    "Sicherung hoch. Beim Linux-Checkpoint wird dessen Inhalt zurückkopiert. Der Checkpoint " +
                    "bleibt erhalten und es wird keine zusätzliche Vorher-/Nachher-Sicherung behalten."),
                Section("5. Backup-Ordner",
                    "„Ordner“ öffnet den Windows-Ordner mit den heruntergeladenen Sicherungen. " +
                    "Ein Checkpoint wird ausschließlich während seiner Erstellung über das Häkchen " +
                    "„Zusätzlich nach Windows herunterladen“ als Datei gespeichert."),
                Section("6. Reparatur & Upgrade",
                    "Prüft die von ioBroker empfohlene Node.js-Version, Berechtigungen, JS-Controller, " +
                    "native npm-Module und Adapter. Die einzelnen Schritte laufen möglichst weiter, " +
                    "Nach dem Anklicken kannst du Basisreparatur, Node.js, JS-Controller, native Module " +
                    "und Adapter einzeln an- oder abwählen. Sind alle Haken gesetzt, läuft die vollständige Reparatur. " +
                    "Zusätzlich stehen zwei Stufen für das Linux-System bereit: „Boot-Ziel auf Serverbetrieb“ " +
                    "stellt multi-user.target ein, wenn der Rechner mit grafischer Oberfläche startet, und " +
                    "„Systempakete aktualisieren“ führt das reguläre Paket-Update der Distribution aus. " +
                    "Beide sind standardmäßig abgewählt und wirken erst beim nächsten Neustart. " +
                    "Rückfragen des offiziellen ioBroker-Fixers werden ohne Eingabe mit „Nein“ beantwortet, " +
                    "damit der Lauf nicht auf eine Tastatureingabe wartet. " +
                    "Die gewählten Schritte laufen möglichst weiter, auch wenn eine Nebenstufe fehlschlägt. " +
                    "Wirklich fehlgeschlagen ist die Reparatur " +
                    "erst, wenn ioBroker am Ende nicht mehr startet."),
                Section("7. Integriertes Terminal",
                    "Nach „Verbinden“ wird die komplette Terminal-Karte automatisch mit dem Linux-System " +
                    "verbunden. Klicke in die schwarze Fläche und gib Befehle direkt ein. Farben, Cursor, " +
                    "Pfeiltasten, Scrollback und interaktive Konsolenprogramme werden unterstützt. " +
                    "Zum Kopieren Text mit der Maus markieren und rechtsklicken. Ist nichts markiert, " +
                    "fügt ein Rechtsklick den Inhalt der Windows-Zwischenablage in das Terminal ein. " +
                    "Mit ↗ löst du das laufende Terminal in ein eigenes Fenster. Tool und Terminal bleiben " +
                    "gleichzeitig bedienbar. Beim Schließen wird das Terminal automatisch wieder eingebettet. " +
                    "Strg+C behält seine normale Linux-Funktion zum Abbrechen eines laufenden Befehls."),
                Section("8. Neuinstallation",
                    "„Pi Imager“ öffnet das offizielle Programm zum Vorbereiten einer SD-Karte. " +
                    "„ioBroker installieren“ ist nur für ein vorbereitetes 64-Bit-Linux ohne bereits " +
                    "vorhandene ioBroker-Installation gedacht."),
                Section("9. Netzwerk und Datei-Transfer",
                    "„Prüfen“ testet Adresse, Ping und SSH-Port. „Router“ öffnet die Seite des erkannten " +
                    "Standard-Gateways. „WinSCP“ öffnet eine grafische SFTP-Verbindung für Dateien."),
                Section("10. Passwortabfragen während einer Aktion",
                    "Wenn Linux sudo- oder doas-Rechte verlangt, erscheint ein eigener Passwortdialog. " +
                    "Dieses Administratorkennwort kann vom SSH-/PuTTY-Passwort abweichen und wird weder " +
                    "gespeichert noch im Protokoll angezeigt."),
                Section("11. Live-Protokoll und Abbrechen",
                    "Das Live-Protokoll zeigt, was gerade passiert. Lange Vorgänge zeigen zusätzlich " +
                    "Schritt, Laufzeit und Prozent. „Speichern“ exportiert das Protokoll für eine " +
                    "Fehlersuche. „Abbrechen“ fordert einen kontrollierten Abbruch der laufenden Aktion an."),
                close
            }
        };

        Content = new ScrollViewer
        {
            Content = content,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
    }

    private static TextBlock Paragraph(string text) => new()
    {
        Text = text,
        TextWrapping = TextWrapping.Wrap,
        Foreground = Brush.Parse("#B6C2D2"),
        LineHeight = 22
    };

    private static Border Section(string title, string text) => new()
    {
        Background = Brush.Parse("#162130"),
        BorderBrush = Brush.Parse("#26364C"),
        BorderThickness = new Thickness(1),
        CornerRadius = new CornerRadius(10),
        Padding = new Thickness(16),
        Child = new StackPanel
        {
            Spacing = 7,
            Children =
            {
                new TextBlock
                {
                    Text = title,
                    FontSize = 16,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brush.Parse("#55E1C8")
                },
                Paragraph(text)
            }
        }
    };
}
