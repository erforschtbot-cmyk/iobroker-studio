# 05 — Die acht Remote-Skripte

Beim Verbinden lädt die Anwendung den Inhalt eines Skripts aus den eingebetteten
Ressourcen und schickt ihn per SSH an das Zielsystem. `ScriptResources.Load(name)`
liefert den Text; das Skript beginnt mit einem Here-Dokument (`bash -s <<'MARKE'`) und
wird als **ein** Aufruf übertragen. Auf dem Zielsystem wird nichts abgelegt und nichts
ausgeführt, was nicht in diesen Dateien steht.

## Warum die Dateiendung `.txt`?

Die Dateien heißen im Verzeichnis `*.txt`, liegen aber als Bash-Skripte vor
(`file`: „bash script, UTF-8 text executable"). Der Name ist historisch; die
`IoBrokerTool.csproj` bindet sie über `LogicalName="IoBrokerTool.Scripts.<name>.sh"` als
`EmbeddedResource` ein. **Nicht umbenennen**, ohne die `csproj`-Einträge anzupassen — der
Build bricht sonst ab.

## Übersicht

| Datei | Ressourcenname | Aufgabe |
|-------|----------------|---------|
| `backup-create.txt` | `backup-create.sh` | Vollbackup nach `/tmp/iobroker-tool-backup.tar.gz` |
| `backup-restore.txt` | `backup-restore.sh` | Archiv hochladen, stoppen, nach `/opt/iobroker` entpacken, starten |
| `checkpoint-create.txt` | `checkpoint-create.sh` | Linux-Checkpoint `.studio-checkpoint` atomar aufbauen |
| `checkpoint-restore.txt` | `checkpoint-restore.sh` | Aus `.studio-checkpoint` zurücksetzen, Checkpoint erhalten |
| `checkpoint-export.txt` | `checkpoint-export.sh` | Checkpoint einmalig als Archiv für den Download packen |
| `repair.txt` | `repair.sh` | Distributionsübergreifende Best-Effort-Reparatur/Upgrade |
| `error-diagnostics.txt` | `error-diagnostics.sh` | Ruft den offiziellen `iob diag` auf und zeigt die Ausgabe |
| `iobroker-install.txt` | `iobroker-install.sh` | Offizieller Installer auf einem frischen Ziel |

## Gemeinsame Konventionen

* **Rechteerhöhung dynamisch:** Die Skripte setzen `ROOT_MODE` auf `root`, `sudo`, `doas`
  oder `none` und kapseln das in `as_root()`. Feste Annahmen über die Distribution
  werden vermieden.
* **Arbeiten als ioBroker-Benutzer:** Schreibende Vorgänge laufen möglichst über
  `as_iobroker()` (`sudo -H -u iobroker`, `doas -u iobroker`, bei Root `runuser`),
  damit unter `/opt/iobroker` keine fremdbesessenen Dateien entstehen.
* **Fortschritt:** Die Skripte melden Fortschritt als Zeilen `STUDIO_PROGRESS|<Prozent>|<Text>`,
  die die Anwendung auswertet und in der Oberfläche anzeigt.
* **Abbruch/Rollback:** Kritische Abläufe setzen `trap`-Handler, die bei Abbruch den
  vorherigen Zustand wiederherstellen (z. B. `STOPPED`, `SWAPPED`, `CURRENT_MOVED`).

## Feste Pfade

* ioBroker-Basis: `/opt/iobroker` (einzige feste Annahme)
* Checkpoint: `/opt/iobroker/.studio-checkpoint`
* Temporär beim Checkpoint-Aufbau: `.studio-checkpoint-new`, `.studio-checkpoint-old`
* Temporär beim Restore: `/opt/iobroker/.studio-restore-temp` (wird danach gelöscht)

## Herkunft der Bezugsquellen

`repair.txt` und `iobroker-install.txt` verwenden offizielle Quellen:

* `https://raw.githubusercontent.com/ioBroker/ioBroker/master/versions.json`
  (empfohlene Node.js-Version)
* `https://iobroker.net/install.sh` (offizieller Installer)

Es wird **keine** Node.js-Version im Tool festgeschrieben; die jeweils offiziell
empfohlene Version wird ermittelt.

## Nächster Schritt

→ [docs/06-release.md](06-release.md)
