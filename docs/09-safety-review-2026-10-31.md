# Sicherheits- und Zuverlässigkeitsreview – 2026-10-31

Prüfstand: lokaler Arbeitsbaum, Projektversion 1.2.35. Bereits vorhandene uncommittete Änderungen wurden nicht verändert. Keine Live-Reparatur, kein Windows-Starttest, kein neuer Release. Befunde betreffen den gelesenen Quellcode, nicht einen unabhängig dekompilierten Auslieferungsstand.

## Kritisch: fehlertoleranter Restore ist nicht transaktional

`Programm/Programm/checkpoint-restore.txt:148–159` und `backup-restore.txt:74–81`: mv-Ergebnisse werden nicht geprüft; CURRENT_MOVED wird anschließend pauschal true. `undo_swap` löscht beide aktiven Datenverzeichnisse, bevor es prüft, ob die jeweilige Rückfallkopie existiert. `undo_restore` hat das entsprechende Risiko nach EXTRACTION_STARTED (Variable im Code: EXTRACTION_STARTED). Ein fehlgeschlagener Move kann Originale am alten Ort zurücklassen, die beim Rückfall gelöscht werden. Ein cp in ein noch vorhandenes Ziel kann außerdem unerwünschte Verschachtelungen erzeugen.

Beleg: Originalfunktion undo_swap isoliert gegen temporäre Testverzeichnisse aufgerufen, Zustand nach fehlgeschlagenem Move simuliert. Ergebnis: Original fehlt, Rückfallkopie fehlt. Keine echten ioBroker-Dateien beteiligt.

Abhilfe: Kopie/Entpacken vollständig in frisches Staging, Validierung vor Dateitausch, pro Pfad protokollierte erfolgreich ausgeführte Moves, Abbruch bei jedem Move-Fehler; niemals Original löschen, dessen Rückfallkopie nicht nachgewiesen ist. Wiederanlauf nach Prozessabbruch über persistentes Transaktionsjournal.

## Hoch: Reparatur läuft trotz Stop-Fehler weiter

`repair.txt:536`: run_step Stop || true. Anschließend sind Node-Wechsel, Fixer, Controller und npm rebuild erreichbar. Auch nach anderen kritischen Fehlern werden abhängige Stufen weiter ausgeführt.

Beleg: Original-Ablaufabschnitt mit harmlosen Funktions-Stubs ausgeführt: STOP_FAILED -> NODE_CHANGE_REACHED.

Abhilfe: bestätigter Stillstand als harte Voraussetzung; abhängige Änderungen nach Fehler sperren. Ursprünglichen Betriebszustand erfassen und nicht pauschal einen vorher bewusst gestoppten Dienst starten.

## Hoch: Laufzeit-Syntaxprüfung prüft nur die äußere Hülle

`Services/SshService.cs:266` prüft bash -n auf eine Datei mit bash-Heredoc. Bash interpretiert dessen Inhalt bei dieser Prüfung nicht als Skript. SHA-256 beweist nur unveränderten Transport.

Beleg: absichtlich beschädigter Heredoc: äußere Prüfung Exit 0, innere Prüfung Exit 2. Bestehende Tests prüfen den inneren Text korrekt; die Laufzeitprüfung nicht.

Abhilfe: reine Bash-Dateien als Ressourcen oder separater Parser/Extraktion mit Prüfung des tatsächlich ausgeführten Inhalts vor jeder Mutation.

## Hoch: Pflicht-Checkpoint blockiert typische Reparaturfälle

`MainWindow.axaml.cs:475–491` verlangt einen erfolgreichen Checkpoint. Dieser verlangt lesbare Dateien als iobroker (`checkpoint-create.txt:134`), eine funktionierende Stop-CLI und am Ende einen erfolgreichen Start. Gerade falsche Rechte oder eine defekte Node/Controller-Installation können diese Bedingungen verhindern. Die Reparatur wird dann nicht erreicht.

Abhilfe: getrennte Notfallsicherung bei bestätigtem Stillstand, gegebenenfalls privilegiertes Lesen unter Erhalt der Original-Metadaten. Sicherungserfolg nicht mit Startfähigkeit gleichsetzen. Ohne erfolgreiche Sicherung nicht einfach blind fortfahren.

## Hoch: Checkpoint ist kein vollständiger Rückweg nach Node-Änderungen

Gesichert werden iobroker-data, node_modules und package-Dateien; nicht systemweites Node.js, Paketquellen, Systempakete oder Dienste. repair_node kann Node wechseln und Paketmanager-Fallbacks ausführen. Alte native Module können nach Rückkopieren mit neuer Node-ABI inkompatibel sein.

Abhilfe: Node-Wechsel als eigene Migration mit Versions-/ABI-Inventar und eigenem Wiederherstellungsplan; Dateicheckpoint nicht als vollständigen Systemrollback bezeichnen.

## Hoch: Erfolgskontrolle und Rückfallsicherung

Restore löscht RESTORE_TEMP bereits nach erfolgreichem Startkommando, ohne Bereitschaftsprüfung. Ein erfolgreicher Startauftrag beweist keine dauerhaft laufende Instanz. Reparatur kontrolliert nur Dienst/CLI, nicht erwartete aktivierte Adapter, Datenbankverbindungen oder neue Startfehler. Ein neuer Pflicht-Checkpoint ersetzt zudem den bisherigen, auch wenn der neue Ausgangszustand bereits beschädigt ist.

Abhilfe: begrenzte Bereitschaftsprüfung mit Wiederholungen, Sollzustand aktivierter Instanzen berücksichtigen, Rückfallsicherung bis nach Gesundheitsprüfung behalten. Letzten bekannten guten Checkpoint von einer Sicherung des aktuellen Fehlerzustands unterscheiden.

## Weitere wichtige Verbesserungen

- Reparatur und Update trennen: alle fünf Stufen sind vorausgewählt (`RepairOptionsDialog.cs:124`). Normale Reparatur sollte Versionen erhalten; Updates separat mit Vorschau und gezielter Auswahl.
- Controller-Fallback `npm install ...@latest` umgeht die durch den ioBroker-Upgradepfad getroffene Versionswahl. Fehler diagnostizieren, nicht automatisch auf ein ungeprüftes npm-Ziel ausweichen.
- Node-Paketmanager-Fallback kann Systempakete entfernen; `pacman -Sy` mit einzelnen Paketen birgt Partial-Upgrade-Risiko. Plattformabhängige, unterstützte Verfahren statt pauschaler Eskalation.
- SSH-Abbruch meldet lokal abgebrochen, ohne bestätigten Zustand der Remote-Prozessgruppe. Leseschleife hat keine explizite Disconnect-Abfrage und ein Limit von drei Stunden. Persistente Job-ID/Journal, Wiederverbinden und bestätigter Abbruch fehlen.
- Keine hostweite flock-Sperre; UI-Sperre schützt nur diese App-Instanz. Feste Restore-/Checkpoint-Pfade können bei parallelen Instanzen kollidieren.
- Checkpoint/Restore verwenden teils `trap cleanup EXIT INT TERM` ohne terminierenden Signalhandler; Cleanup kann während laufender Kopie eingreifen und die Ausführung danach weitergehen. Signalbehandlung, Kindprozesse und Restore-Zustand gemeinsam absichern.
- Hochgeladenes Archiv wird direkt in die Installation entpackt. Format, erlaubte Mitglieder, Links, Integrität und Speicherbedarf vor Stop/Dateitausch validieren und in Staging entpacken. Keine Kompatibilität mit beliebigen ioBroker-/Backitup-Archiven behaupten.
- Startup-Logging beginnt nach InitializeComponent. Kein globaler Startup-Abfanghandler in Program.cs. Die frühere _settings-Reihenfolge ist korrigiert, frühe Startfehler bleiben aber unzureichend erfasst.
- Meldung „bevor ioBroker gestoppt oder verändert wurde“ nach jedem Checkpoint-Fehler ist falsch: Fehler können nach Stop/Kopie/Checkpoint-Aktivierung auftreten.

## Durchgeführte Prüfungen

- python3 -m unittest discover -s tests -v: 3/3 bestanden (Dateizugriff, gesperrtes Arbeitsverzeichnis, innere Bash-Syntax).
- Drei isolierte Reproduktionen oben: gefährlicher Recovery-Pfad, Stop-Fehler-Fortsetzung, falsches Syntax-Sicherheitsgefühl bestätigt.
- Kein Windows-GUI-Test, keine echten Updates, kein Restore auf Produktivdaten. Deshalb keine Aussage „läuft perfekt“.

## Empfohlene Reihenfolge

1. Restore/Undo transaktional absichern und Fehler-Injektionstests erstellen.
2. Harte Stop-/Abhängigkeitsprüfungen, echte Syntaxprüfung, hostweite Sperre.
3. Sicherungsweg für defekte Installationen und Schutz des letzten guten Checkpoints.
4. Reparatur von Updates trennen; Node-Migration und gezielte Adapterupdates.
5. Gesundheitsprüfung, Remote-Job-Abbruch/Wiederaufnahme, Startup-Diagnose.
6. Wegwerf-Testsysteme: voller Datenträger, Rechtefehler, defekte CLI, Verbindungsabbruch, fehlgeschlagene Moves, verzögerter Start und erfolgloser Start. Danach Windows-Smoke-Test und geprüfte Auslieferung.
