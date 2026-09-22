# Danganronpa V3 Thai Mod Installer

Source-code archive for the current Nexus Mods upload of the fan-made Thai
translation installer for **Danganronpa V3: Killing Harmony**.

This repository is published for transparency and security review. It contains
the installer source code, tests, manifests, build notes, and hashes of the
executables uploaded to Nexus Mods.

## Security-relevant behavior

- Does not inject code or read/write game memory.
- Contains no networking or download functionality.
- Verifies the translation payload and supported Steam CPK archives with
  hard-coded SHA-256 hashes.
- Extracts two CPK archives into loose files under the game's `data/win`
  directory.
- Replaces 56 loose files with Thai translations.
- Moves the original CPK archives to a backup directory for uninstall/restore.
- Uses a standard WinForms GUI with no custom drawing, embedded artwork,
  direct `user32.dll` calls, networking, or download functionality.
- Requests administrator privileges when the GUI starts. It launches the
  backend without a visible console and reads progress from redirected output.

## Repository scope

The 56 translation assets, original game files, compiled executables, and icon
are intentionally not included. Translation releases are distributed separately
through Nexus Mods. No original CPK archive or third-party executable is included
here.

Because the private release assets are omitted, this public repository documents
the build process but does not reproduce the uploaded binaries byte-for-byte on
its own.

## Source layout

- `source/drv3_thai_installer.py` — Python installer backend.
- `source/Drv3ThaiGui.cs` — .NET Framework WinForms GUI.
- `source/app.manifest` — Windows application manifest.
- `source/backend.manifest` — asInvoker manifest for the backend executable.
- `source/test_reuse_backup.py` — backend integration test using synthetic data.
- `DRV3ThaiBackend.spec` — PyInstaller configuration used for the backend.
- `BUILDING.md` — build and verification notes.
- `SHA256SUMS.txt` — hashes of the executables uploaded to Nexus Mods.

## Current Nexus upload hashes

```text
F8441A03FC317795D3213565F66281EF7D24977D3BF220854043FCAFE2E3A467  Danganronpa_V3_Thai_Mod_Installer.zip
8CD2B56536C5A3AAF1337A107698B70A9AC7AEDA3F825E6E174330D0DAB0AE95  Danganronpa V3 Thai Mod Installer.exe
F36740895A84CD9EBBA31CAF1D112298C4DEDC299877D93C26870D69B31F43FD  bin/DRV3ThaiBackend.exe
```

VirusTotal report for the quarantined ZIP:

https://www.virustotal.com/gui/file/f8441a03fc317795d3213565f66281ef7d24977d3bf220854043fcafe2e3a467

The backend is unchanged from the earlier release and remains a PyInstaller
one-file executable. Some scanners classify PyInstaller bootloaders using
generic or machine-learning labels. The complete backend source and its payload
hash table are included here for manual review.

## License

The installer source code is licensed under the MIT License. Danganronpa,
its characters, artwork, audio, and game data belong to their respective
rights holders. See `LICENSE.txt` and `THIRD_PARTY_NOTICES.txt`.

