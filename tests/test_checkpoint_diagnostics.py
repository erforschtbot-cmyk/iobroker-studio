"""Non-destructive access diagnostics regression tests; no ioBroker host needed."""
import os
from pathlib import Path
import subprocess
import tempfile
import unittest

ROOT = Path(__file__).resolve().parents[1]
SOURCE = (ROOT / 'Programm/Programm/checkpoint-create.txt').read_text()
# Der geschnittene Abschnitt enthaelt alle Helfer, die die Quellenpruefung
# benoetigt (neutrales Arbeitsverzeichnis + Scan).
for _marker in ('as_iobroker_in_root() {', 'scan_as_iobroker() {',
                'verify_source_access() {'):
    if _marker in SOURCE:
        SCAN_START = SOURCE.index(_marker)
        break
else:
    raise AssertionError('Quellenpruefung nicht im Skript gefunden')
CHECK = SOURCE[SCAN_START:SOURCE.index('\ncopy_tree()')]

@unittest.skipIf(os.geteuid() == 0, 'Access tests require an unprivileged user')
class AccessDiagnostics(unittest.TestCase):
    def test_access_cases(self):
        with tempfile.TemporaryDirectory(prefix='studio-access-') as tmp:
            root = Path(tmp)
            data = root / 'iobroker-data'
            data.mkdir()
            modules = root / 'node_modules'
            modules.mkdir()
            file = data / 'unreadable file.txt'
            file.write_text('test')
            blocked = modules / 'blocked directory'
            blocked.mkdir()
            (blocked / 'child').write_text('test')
            def check():
                script = 'IOB_DIR=$1; IOB_USER=$(id -un)\nas_iobroker() { "$@"; }\nprogress() { :; }\n' + CHECK + '\nverify_source_access\n'
                return subprocess.run(['bash', '-c', script, 'test', tmp], text=True, capture_output=True)
            self.assertEqual(check().returncode, 0)
            file.chmod(0)
            blocked.chmod(0)
            try:
                result = check()
                self.assertEqual(result.returncode, 1)
                self.assertIn(str(file), result.stdout)
                self.assertIn(str(blocked), result.stdout)
                self.assertIn('Besitzer:', result.stdout)
                self.assertIn('Benutzer:', result.stdout)
            finally:
                file.chmod(0o600)
                blocked.chmod(0o700)
            # Missing execute permission must also be reported.
            blocked.chmod(0o600)
            try:
                result = check()
                self.assertEqual(result.returncode, 1)
                self.assertIn(str(blocked), result.stdout)
            finally:
                blocked.chmod(0o700)
            (data / 'broken link').symlink_to(data / 'missing')
            self.assertEqual(check().returncode, 0)

    def test_unreadable_working_directory_is_not_an_access_error(self):
        """SSH-Benutzer in /home/pi (Modus 700) darf keinen Fehlalarm ausloesen.

        GNU find stellt beim Beenden sein Startverzeichnis per fchdir wieder her.
        Ist das Home des aufrufenden Benutzers fuer den Zielbenutzer gesperrt,
        scheitert das selbst bei voll lesbarer Quelle ("Failed to restore initial
        working directory", Exitcode 1) — genau der gemeldete Studio-Fehler 13.
        """
        with tempfile.TemporaryDirectory(prefix='studio-cwd-') as tmp:
            root = Path(tmp)
            data = root / 'iobroker-data'
            data.mkdir()
            (data / 'objects.json').write_text('{}')
            home = root / 'home-pi'
            home.mkdir()
            script = (
                'IOB_DIR=$1; IOB_USER=$(id -un)\n'
                'as_iobroker() { "$@"; }\n'
                'progress() { :; }\n'
                + CHECK +
                '\ncd "$2" || exit 99; chmod 000 "$2"; verify_source_access; rc=$?; '
                'chmod 700 "$2"; exit "$rc"\n'
            )
            result = subprocess.run(
                ['bash', '-c', script, 'test', tmp, str(home)],
                text=True, capture_output=True)
            self.assertEqual(
                result.returncode, 0,
                'Quellenpruefung darf gesperrtes Arbeitsverzeichnis nicht als '
                'Rechteproblem der Quelle melden.\n' + result.stdout + result.stderr)

class Syntax(unittest.TestCase):
    def test_embedded_scripts(self):
        for path in (ROOT / 'Programm/Programm').glob('*.txt'):
            content = path.read_text()
            if content.startswith("bash -s <<'"):
                content = '\n'.join(content.splitlines()[1:-1])
            if '#!/usr/bin/env bash' in content:
                with self.subTest(script=path.name):
                    result = subprocess.run(['bash', '-n'], input=content, text=True, capture_output=True)
                    self.assertEqual(result.returncode, 0, result.stderr)

class RepairScript(unittest.TestCase):
    """Regression: der offizielle Fixer darf nie auf eine Tastatureingabe warten."""

    def test_official_fixer_reads_from_dev_null(self):
        source = (ROOT / 'Programm/Programm/repair.txt').read_text()
        self.assertIn('bash "$fix_script" </dev/null', source)

    def test_system_stages_are_opt_in(self):
        source = (ROOT / 'Programm/Programm/repair.txt').read_text()
        self.assertIn('STUDIO_REPAIR_BOOTTARGET="${STUDIO_REPAIR_BOOTTARGET:-0}"', source)
        self.assertIn('STUDIO_REPAIR_OSUPDATES="${STUDIO_REPAIR_OSUPDATES:-0}"', source)


if __name__ == '__main__':
    unittest.main()
