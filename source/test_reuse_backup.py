from __future__ import annotations

import hashlib
import shutil
import sys
from pathlib import Path


sys.path.insert(0, str(Path(__file__).resolve().parent))
import drv3_thai_installer as installer


def digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest().upper()


def main() -> None:
    sys.stdout.reconfigure(encoding="utf-8")
    root = Path(__file__).resolve().parents[2] / "diagnostics" / "reuse_backup_integration"
    if root.exists():
        shutil.rmtree(root)
    game = root / "game"
    win = game / "data" / "win"
    backup = game / installer.BACKUP_DIR_NAME
    payload = root / "payload"
    win.mkdir(parents=True)
    backup.mkdir(parents=True)
    payload.mkdir(parents=True)

    archives = {
        "partition_data_win_us.cpk": b"DATA-US-TEST-CONTENT",
        "partition_resident_win.cpk": b"RESIDENT-TEST-CONTENT",
    }
    installer.CPKS = {
        name: {"sha256": digest(data), "size": len(data)}
        for name, data in archives.items()
    }
    for name, data in archives.items():
        (win / name).write_bytes(data)
        (backup / name).write_bytes(data)

    thai_rel = Path("wrd_script") / "007" / "thai_test.SPC"
    (payload / thai_rel).parent.mkdir(parents=True)
    (payload / thai_rel).write_bytes("ภาษาไทย".encode("utf-8"))
    installer.payload_root = lambda: payload
    # This integration test uses one tiny synthetic payload. Production builds
    # separately verify the exact 12-file payload and its embedded SHA-256 map.
    installer.validate_payload = lambda: [payload / thai_rel]

    def fake_rows(cpk_path: Path):
        data = archives[cpk_path.name]
        rel = Path("archive_test") / f"{cpk_path.stem}.bin"
        return ([{"path": rel, "offset": 0, "stored_size": len(data), "extract_size": len(data)}], len(data))

    installer.archive_rows = fake_rows
    installer.install(game)
    assert not any((win / name).exists() for name in archives)
    assert all((backup / name).read_bytes() == data for name, data in archives.items())
    assert not any(backup.glob("*.installer_current"))
    assert (backup / installer.MANIFEST_NAME).is_file()
    assert (win / thai_rel).read_bytes() == "ภาษาไทย".encode("utf-8")

    installer.uninstall(game)
    assert all((win / name).read_bytes() == data for name, data in archives.items())
    assert not (win / thai_rel).exists()
    assert not (backup / installer.MANIFEST_NAME).exists()
    shutil.rmtree(root)
    print("REUSE BACKUP INTEGRATION PASS")


if __name__ == "__main__":
    main()
