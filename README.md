# Danganronpa V3 Thai Mod Installer

Source-code archive for the current Nexus Mods upload of the fan-made Thai
translation installer for **Danganronpa V3: Killing Harmony**.

The current reviewed source corresponds to release **2.0.0**.

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
D4FECC0C2C672953E1FE285F3EBB27158ABA4686C99331E19401CA13775B9FA4  Danganronpa_V3_Thai_Mod_Installer_v2.0.0.zip
C3F1EFFCD1B1BC2BBBB4C369432BA919D63BD741930C0C4FB04DF2C126E083F7  Danganronpa V3 Thai Mod Installer.exe
47F2EE7875C03652C7B5F36E9C460162F3D34AA53087B2E92CBB08A0DB406EFA  bin/DRV3ThaiBackend.exe
```

VirusTotal report for the quarantined ZIP:

https://www.virustotal.com/gui/file/d4fecc0c2c672953e1fe285f3ebb27158aba4686c99331e19401ca13775b9fa4

The backend remains a PyInstaller one-file executable. Release 2.0.0 updates 20
of the 56 translation payload files and the corresponding hard-coded SHA-256
table; its installer behavior is unchanged. Some scanners classify PyInstaller
bootloaders using generic or machine-learning labels. The complete backend
source and its payload hash table are included here for manual review.

## License

The installer source code is licensed under the MIT License. Danganronpa,
its characters, artwork, audio, and game data belong to their respective
rights holders. See `LICENSE.txt` and `THIRD_PARTY_NOTICES.txt`.

