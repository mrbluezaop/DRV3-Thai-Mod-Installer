# Danganronpa V3 Thai Mod Installer

Source-code archive for version 1.2.4 of the fan-made Thai translation
installer for **Danganronpa V3: Killing Harmony**.

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
- Requests administrator privileges only for install and uninstall operations.

## Repository scope

The 56 translation assets, original game files, compiled executables, and GUI
art assets are intentionally not included. Translation releases are distributed
separately through Nexus Mods. No original CPK archive or third-party executable
is included here.

Because the private release assets are omitted, this public repository documents
the build process but does not reproduce the uploaded binaries byte-for-byte on
its own.

## Source layout

- `source/drv3_thai_installer.py` — Python installer backend.
- `source/Drv3ThaiGui.cs` — .NET Framework WinForms GUI.
- `source/app.manifest` — Windows application manifest.
- `source/test_reuse_backup.py` — backend integration test using synthetic data.
- `DRV3ThaiBackend.spec` — PyInstaller configuration used for the backend.
- `BUILDING.md` — build and verification notes.
- `SHA256SUMS.txt` — hashes of the executables uploaded to Nexus Mods.

## Uploaded executable hashes

```text
0F758B36DD2F795AB5AA7CFF8457ED437480E05C551E84C7B51B14D8E136E67F  Danganronpa V3 Thai Mod Installer.exe
F36740895A84CD9EBBA31CAF1D112298C4DEDC299877D93C26870D69B31F43FD  bin/DRV3ThaiBackend.exe
```

## License

The installer source code is licensed under the MIT License. Danganronpa,
its characters, artwork, audio, and game data belong to their respective
rights holders. See `LICENSE.txt` and `THIRD_PARTY_NOTICES.txt`.

