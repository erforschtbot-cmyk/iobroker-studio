# Aktionsübergreifende Live-Fehlerdetails

Die gemeinsame SSH-Ausgabe zeigt erkannte Fehler/Warnungen mit Zeitpunkt und
Arbeitsschritt separat an. Der Bereich bleibt nach Abschluss sichtbar, auch wenn
ein Fallback erfolgreich war. Er ist eine Historie erkannter Meldungen, keine
Behauptung, dass sämtliche gemeldeten Probleme weiterhin bestehen.

Backup, Checkpoint, Export, Restore, Reparatur, Installation und Diagnose nutzen
dieselbe Ausgabe. Fehlerdialoge enthalten zusätzlich die letzten Systemausgaben.
SSH-Paketgrenzen dürfen Fehlerwörter oder Fortschrittsmarkierungen nicht zerlegen;
deshalb werden vollständige Zeilen in Reihenfolge an die Oberfläche geliefert.

Dateisystemprüfungen und Bereinigungen behalten ihre stderr-Ausgaben. Erwartbare
negative Fähigkeitstests (z.B. command -v, sudo -n vor Passwortabfrage) sowie
Fortschritts-Polling werden nicht pauschal als Fehler gewertet. Die Quellenprüfung
für Checkpoints nennt alle auffindbaren unlesbaren Dateien und nicht zugänglichen
Verzeichnisse mit Pfad, Rechten und Besitzer. Hinter einem unzugänglichen
Verzeichnis können naturgemäß keine weiteren Dateien aufgelistet werden.

## Fehlalarm: „Failed to restore initial working directory"

GNU `find` stellt beim Beenden sein Startverzeichnis per `fchdir` wieder her. Läuft
Studio über SSH als Benutzer mit Home `/home/pi` (Modus 700) und wird `find`
per `sudo -u iobroker` gestartet, bleibt das Arbeitsverzeichnis `/home/pi`. Für
`iobroker` ist es gesperrt — `find` endet dann mit Exitcode 1 und der Meldung
`find: Failed to restore initial working directory: /home/pi: Permission denied`,
**auch wenn die Quelle vollständig lesbar ist**. Studio meldete das bis 1.2.29 als
`FEHLER: Schritt Quellenpruefung … find-Exitcode: 1` und brach mit Exitcode 13 ab.

Erkennungsmerkmal: Es erscheint **keine** Zeile `FEHLER: Zugriff fehlt | Pfad: …`.
Dann ist die Quelle in Ordnung und nur das Arbeitsverzeichnis das Problem.

Seit 1.2.30 laufen Quellen- und Besitzerprüfung über `as_iobroker_in_root` in `/`;
das gesperrte Home kann den Exitcode nicht mehr verfälschen. Unlesbare Dateien und
Verzeichnisse werden weiterhin gemeldet. Regressionstest:
`tests/test_checkpoint_diagnostics.py::test_unreadable_working_directory_is_not_an_access_error`.

Fremdprogramme liefern nicht immer strukturierte Fehler: bekannte Fehler- und
Warnformulierungen werden hervorgehoben; die vollständige Originalausgabe bleibt
im Live-Protokoll. Ohne gemeldete Ursache kann Studio keine Ursache erfinden.

Prüfung: `python3 -m unittest discover -s tests -v` und .NET-Release-Build.
Keine Reparaturen auf einem realen ioBroker-Host für diese Tests ausführen.
Die grafische Interaktion wurde bislang nicht manuell geprüft.

## Übertragungssicherung vor dem Start (ab 1.2.31)

Skripte werden per SFTP nach `/tmp` geladen und dort ausgeführt. Ein einzelnes
verlorenes Byte im übertragenen Text kann ein Skript syntaktisch zerstören: Aus
`if selected "$STUDIO_REPAIR_CONTROLLER"; then` wird `f selected …`, und bash
bricht **mitten im Lauf** mit Exitcode 2 ab — nachdem ioBroker bereits gestoppt
und Node.js geprüft wurde.

Deshalb prüft `ExecuteScriptAsync` jetzt vor jedem Start:

1. **SHA-256 nach dem Upload.** Der Hash der gesendeten Bytes wird mit
   `sha256sum` auf dem Zielsystem verglichen. Bei Abweichung wird bis zu
   dreimal neu hochgeladen; danach bricht die Aktion mit einer Meldung ab,
   ohne dass etwas verändert wurde.
2. **Syntaxprüfung mit `bash -n`.** Schlägt sie fehl, wird die Datei gelöscht
   und die Aktion abgebrochen. Das Skript wird nie gestartet.

Beide Prüfungen erscheinen im Live-Protokoll (`[Pruefung] …`), damit ein
fehlgeschlagener Upload sichtbar wird, statt sich als Laufzeitfehler zu tarnen.

## Pflicht-Checkpoint vor der Reparatur (ab 1.2.31)

Die Reparatur stoppt ioBroker und kann Node.js austauschen; der Reparaturpfad
selbst erstellt bewusst kein Backup. Ab 1.2.31 legt Studio deshalb **vor** dem
Reparaturskript zwingend einen Schnell-Checkpoint an. Scheitert er, wird die
Reparatur abgebrochen, bevor ioBroker gestoppt oder verändert wurde. Damit gibt
es für jeden Reparaturlauf einen Rückweg.

## Wartende Rückfrage im offiziellen Fixer (ab 1.2.39)

Meldung im Feld: der Fixer fragt „Your system is booting into 'graphical.target' …
Do you want to switch to 'multi-user.target'? (y/N)" und der Lauf bleibt stehen,
ohne dass sich eine Antwort eingeben lässt.

Ursache: `https://iobroker.net/fix.sh` stellt im klassischen Modus mehrere
Ja/Nein-Fragen per `read -r -s -n 1` von **stdin**. Studio führt das Skript in
einem SSH-Shell-Kanal aus und schreibt dort nur das sudo-Passwort hinein –
für Rückfragen ist keine Eingabezeile vorgesehen. Das Skript wartet damit bis
zum Zeitlimit (drei Stunden).

Seit 1.2.39 startet `official_fix()` den Fixer mit `</dev/null`. Jedes `read`
endet sofort ohne Zeichen, damit gilt jede Ja/Nein-Frage als **Nein** und der
Lauf geht weiter. Das Protokoll sagt das ausdrücklich, damit „Nein" nicht als
Antwort des Anwenders missverstanden wird. Das sudo-Passwort ist zu diesem
Zeitpunkt bereits über `detect_root_access` freigeschaltet (Zeitstempel-Cache
von sudo), der Fixer braucht also keine Eingabe mehr.

Bewusste Entscheidungen trifft der Anwender stattdessen in den Reparaturstufen:

* **Boot-Ziel auf Serverbetrieb** – prüft `systemctl get-default` und setzt
  `multi-user.target` nur, wenn derzeit `graphical.target` aktiv ist.
* **Systempakete aktualisieren** – `apt-get update` plus `apt-get -y upgrade`
  (bzw. die Entsprechung von dnf/yum/zypper/pacman/apk), ohne `dist-upgrade`,
  damit kein Distributionswechsel und keine automatische Deinstallation
  passiert. Rückfragen von `dpkg`/`needrestart` werden über
  `DEBIAN_FRONTEND=noninteractive`, `NEEDRESTART_MODE=a` und
  `--force-confdef/--force-confold` unterdrückt.

Beide Stufen sind standardmäßig **abgewählt**, laufen aber im selben
Reparaturfenster (ioBroker ist dann gestoppt) und melden ihr Ergebnis als
normale Reparaturstufe. Ein Neustart des Linux-Systems ist danach ggf. nötig;
das Skript sagt das am Ende an.
