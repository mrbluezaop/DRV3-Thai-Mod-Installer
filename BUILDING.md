# Build notes for v1.2.4

## Requirements

- Windows 10 or 11
- Python 3.13
- PyInstaller 6.x
- .NET Framework 4.x C# compiler (`csc.exe`)
- The separately distributed `mod_data` directory containing the 56 translation
  files
- The separately maintained GUI image resources listed below

## Backend

Place `mod_data` at the repository root, then run:

```text
pyinstaller --clean --noconfirm DRV3ThaiBackend.spec
```

The resulting backend is written to `dist/DRV3ThaiBackend.exe`.

The release configuration uses PyInstaller one-file mode, a windowed backend,
UPX when available, and an administrator manifest. The translation payload is
embedded in the executable.

## GUI

The GUI requires these image resources in `source/`:

```text
app.ico
logo.png
translator.png
monokuma.png
monokuma_question.png
monokuma_warning.png
monokuma_info.png
monokuma_error.png
```

Build from a Developer Command Prompt for .NET Framework:

```text
csc /target:winexe /optimize+ ^
  /out:"Danganronpa V3 Thai Mod Installer.exe" ^
  /win32manifest:source\app.manifest ^
  /win32icon:source\app.ico ^
  /resource:source\app.ico,app.ico ^
  /resource:source\logo.png,logo.png ^
  /resource:source\translator.png,translator.png ^
  /resource:source\monokuma.png,monokuma.png ^
  /resource:source\monokuma_question.png,monokuma_question.png ^
  /resource:source\monokuma_warning.png,monokuma_warning.png ^
  /resource:source\monokuma_info.png,monokuma_info.png ^
  /resource:source\monokuma_error.png,monokuma_error.png ^
  /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll ^
  source\Drv3ThaiGui.cs
```

## Tests and read-only verification

Run the synthetic integration test:

```text
python -X utf8 source\test_reuse_backup.py
```

After building the complete private release payload, verify all embedded files
without changing the game installation:

```text
dist\DRV3ThaiBackend.exe --verify-payload --no-pause
```

