# Build notes for the current Nexus upload

## Requirements

- Windows 10 or 11
- Python 3.13
- PyInstaller 6.x
- .NET Framework 4.x C# compiler (`csc.exe`)
- The separately distributed `mod_data` directory containing the 56 translation
  files

## Backend

Place `mod_data` at the repository root, then run:

```text
pyinstaller --clean --noconfirm DRV3ThaiBackend.spec
```

The resulting backend is written to `dist/DRV3ThaiBackend.exe`.

The release configuration uses PyInstaller one-file console mode and embeds the
translation payload. The console subsystem is retained for deterministic output,
but the elevated GUI launches it with `CreateNoWindow=true` and captures progress
through redirected standard output and error.

The public specification omits the release icon because the artwork is not part
of this source repository. This changes only the executable resource section;
the installer logic and embedded translation payload are unchanged.

## GUI

The GUI uses standard Windows Forms controls. It does not require or embed image
resources, call `user32.dll` directly, use custom window drawing, or contain
network/download code. Its manifest requests administrator privileges at startup
so the backend can be launched without another shell or visible console window.

Build from a Developer Command Prompt for .NET Framework:

```text
csc /nologo /target:winexe /optimize+ /platform:anycpu ^
  /out:"Danganronpa V3 Thai Mod Installer.exe" ^
  /win32manifest:source\app.manifest ^
  /reference:System.dll,System.Drawing.dll,System.Windows.Forms.dll ^
  source\Drv3ThaiGui.cs
```

The uploaded GUI was compiled with the .NET Framework 4.x compiler at:

```text
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
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

Expected SHA-256 values for the reviewed upload are recorded in
`SHA256SUMS.txt`.

The legacy .NET Framework compiler writes build-specific metadata, so a clean
rebuild from the reviewed source can be functionally identical without being
byte-for-byte identical to the uploaded GUI hash.

