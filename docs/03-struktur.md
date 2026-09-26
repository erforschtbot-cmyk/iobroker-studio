# 03 — Struktur und Rolle jeder Komponente

## Baum

```
.
├── README.md                       Einstieg + Schnellstart
├── CHECKSUMS.sha256                Prüfsummen aller ausgelieferten Dateien
├── docs/                           Diese Dokumentation (01–08)
├── scripts/
│   ├── install-dotnet-8.0.423.sh   SDK-Installation (gepinnt)
│   ├── build-windows-x64.sh        Reproduzierbarer Build
│   └── make-release-zip.sh         Release-Archiv erzeugen
├── Next/
│   ├── IoBrokerTool/               ← das Produkt (C#/Avalonia)
│   │   ├── IoBrokerTool.csproj     Projektdatei + Ressourcen-Einbindung
│   │   ├── packages.lock.json      exakte NuGet-Pakete (direkt + transitiv)
│   │   ├── App.axaml(.cs)          App-Lebenszyklus, Theme
│   │   ├── MainWindow.axaml(.cs)   Hauptfenster + Ablaufsteuerung
│   │   ├── Dialogs/                Dialoge (Hilfe, Passwort, Reparaturauswahl …)
│   │   ├── Models/AppSettings.cs   Einstellmodell
│   │   ├── Services/
│   │   │   ├── SshService.cs       SSH-Verbindung, Skript-Ausführung, Host-Key-Prüfung
│   │   │   ├── EmbeddedTerminalService.cs  Integriertes Terminal
│   │   │   ├── SettingsService.cs  Persistenz (%USERPROFILE%, DPAPI)
│   │   │   ├── DesktopService.cs   PuTTY-Schnellzugriff
│   │   │   └── ScriptResources.cs  lädt die acht Remote-Skripte aus den Ressourcen
│   │   └── Assets/                 AppIcon + mitgeliefertes putty.exe
│   ├── Legacy-start.bat            Starter der Legacy-Variante
│   ├── README-Windows.txt          Nutzeranleitung (liegt im Release)
│   ├── README-Release.txt          README aus dem ausgelieferten Ordner
│   └── THIRD-PARTY-NOTICES.txt     Open-Source-Hinweise
├── Programm/
│   ├── Programm/                   ← die acht Bash-Skripte (Remote-Ablauf)
│   ├── *.bat                       Legacy-Batch-Steuerung (plink/pscp)
│   ├── Repl.bat, LICENSE           Hilfsprogramm + Lizenz (Batch-Variante)
│   └── .gitattributes              LF-Normalisierung
└── release/
    └── ioBroker-Studio-1.2.28-Windows-x64.zip   Wiederherstellungsanker
```

## Was ist Quelle, was ist Erzeugnis?

| Kategorie | Dateien | Im Repo? |
|-----------|---------|----------|
| Quellcode | `Next/IoBrokerTool/**` | **ja** |
| Remote-Skripte (Quelle) | `Programm/Programm/*.txt` | **ja** |
| Build-/Nutzungsanleitungen | `docs/`, `Next/README-*.txt` | **ja** |
| Lock-Datei | `Next/IoBrokerTool/packages.lock.json` | **ja** |
| Vorbestätigtes Release | `release/*.zip` | **ja** (Anker) |
| Build-Erzeugnis `publish/**` | EXE, DLLs, PDB | nein (wird gebaut) |
| Fremd-Binaries der Batch-Variante | `putty.exe`, `plink.exe`, `pscp.exe`, `XXMKLINK.EXE` | **nein** |
| Zugangsdaten / Laufzeitdaten | — | **nie** |

## Warum fehlen `plink.exe`, `pscp.exe`, `putty.exe`, `XXMKLINK.EXE`?

* `plink.exe`, `pscp.exe`, `putty.exe` sind **Programme Dritter** (PuTTY-Projekt). Sie
  werden für die Legacy-Batch-Variante gebraucht, aber nicht für einen Neuaufbau.
* `XXMKLINK.EXE` ist ebenfalls ein Fremdprogramm (Symlink-Hilfe).
* Das von der Anwendung selbst mitgelieferte `putty.exe` liegt dagegen unter
  `Next/IoBrokerTool/Assets/Tools/` und ist dort enthalten, weil es als
  Anwendungsressource ausgeliefert wird (Lizenz: `PUTTY-LICENSE.html`).

Wer die Legacy-Batch-Variante vollständig betreiben will, lädt die PuTTY-Werkzeuge
separat von https://www.chiark.greenend.org.uk/~sgtatham/putty/latest.html und legt sie
in `Programm/Programm/` ab.

## Nächster Schritt

→ [docs/04-betrieb.md](04-betrieb.md)
