from __future__ import annotations

import argparse
import hashlib
import json
import mmap
import os
import shutil
import struct
import sys
import time
from dataclasses import dataclass
from pathlib import Path, PurePosixPath
from typing import Any, BinaryIO


APP_NAME = "Danganronpa V3 Thai Mod"
APP_VERSION = "1.2.4"
MANIFEST_NAME = "thai_mod_install_manifest.json"
BACKUP_DIR_NAME = "thai_mod_original_cpk_backup"
TEMP_DIR_NAME = ".drv3_thai_install_tmp"
CPKS = {
    "partition_data_win_us.cpk": {
        "sha256": "D8B310724E0BC2EF0602DF600C2FE2132B9A2CD2AF9C24AA2A96FFD9E23DF7E5",
        "size": 8_525_629_320,
    },
    "partition_resident_win.cpk": {
        "sha256": "2DE9EA6AFA15A80F1079B027D441A6DCC8554C38832109A15C18ADB79B551CC6",
        "size": 2_298_785_400,
    },
}

EXPECTED_PAYLOAD_SHA256 = {
    "flash/event/EV_229_song_US.spc": "BEF4934CB73ED1770BC81009F364E6A2789EEC237CFD620A11F99C10BE7AB45B",
    "game_resident/game_resident_US.spc": "3815AE62DA55E3310DF75509B107B97EB41AA858200161D75B1626E763751F82",
    "minigame/anagram/anagram_US.SPC": "4D59AAC41E4ADDC96EAE6164C0CD23AD8DEE89CDA317EA50427E59FCECB2E100",
    "minigame/brain_drive/brain_drive_US.SPC": "2860FB3381DA8469C3D1FC8DD877DC2860420951A9CD436EA9B46710DC6EB094",
    "minigame/imagination/item_009_US.SPC": "D9C7D3F997CE33C5CAF7B4644D888472F79ED5A51B725FCA1E280300423822AF",
    "minigame/imagination/item_010_US.SPC": "AB649D7D25D48E4FBD9A957B695F3EBB78E4F59BDBAC4C6933459B8E13C1C152",
    "minigame/imagination/item_011_US.SPC": "C63AD673E217EE071FEB8ABC7A360E4921A3BC60E5FE2C3B8559F474B1AE69C7",
    "trial_font/game_font01_1_US.spc": "96F36305BA8D2C7322144E4A4604568264D666FB75658D75D3D6844FF34D898F",
    "trial_font/game_font01_2_US.spc": "522E102BE0B8784A346312DC461E1399256414304CE092670175471DA43A3B7F",
    "trial_font/game_font01_3_US.spc": "BDD71D0D10FE7CDC546A4BE6302DCC761A8071DC85848ED98CEC06F5D9096E22",
    "trial_font/game_font01_4_US.spc": "8DD9566E597205CA7C81D26403B5B08CC4B1DB3197DC3B18EFC804A05F5F0817",
    "trial_font/game_font01_5_US.spc": "16AE0B9EA456E5D555FF37AED662E22294598C4764A078CDC12E332E74E73759",
    "trial_font/game_font01_6_US.spc": "79E107E4DD08C7E49DBC33171B24C2227CBA334AD1897AFF1FC8E129A16BD675",
    "trial_font/game_font01_8_US.spc": "ECAC82606103F54B32A9572D8E37BBEBD4F1AD94710154E754DC440BA3097122",
    "trial_font/game_font01_9_US.spc": "06408463BCE825492559979354C50BEFF758C10963B73B58BED19081C39D96F6",
    "trial_font/game_font03_US.spc": "11C3D6BF4A2ACA1237CA1162FB7E8F422FB9DB414024E06E569AC5632DBCDBFD",
    "trial_font/game_font04_US.spc": "5F686447652D5DBE2C40332DA4B1F31F7C0FDC69DA2D95D06731373CC27F9108",
    "trial_font/game_font05_US.spc": "A9FBC21C52237FAE0EA5CDE6BCA94DC8FDD880E51F8569F086321CFE3BF9C6AC",
    "trial_font/game_font06_US.spc": "CAC346C7F25442D74339E02DBA939E36F569070332BF544B7F3EE1DCD1C87879",
    "trial_font/game_font07_US.spc": "AD2901D481A2961028ADF3763D493C0222A8FDCA0906A994AC315021E12209BC",
    "trial_font/game_font08_US.spc": "CBAB679CE055B12ECC5EC3A61F407C5D3BA2957EB1BF3A995D60C461D01751E3",
    "trial_font/game_font09_US.spc": "058C63EB15FA9E38BFE3351403F3C3CC7075F32A1D9B2A2497A896AD5E59891C",
    "trial_font/game_font10_US.spc": "54143A6451309F85D7C22C7885E10AD5BA36A7E561EEB0257048130B10319D43",
    "trial_font/game_font11_US.spc": "EF9DC85218AD2DE64239A06318FE3BC4F28220A3606A388EA0DE7441B64AA686",
    "trial_font/game_font12_US.spc": "10CCF50EDC2FA97EA7DB4601B142C51693D049896513281A483F64E2EC496F5D",
    "trial_font/game_font13_US.spc": "0CF81F15A319313A3AB04FA3FFB08F8DC4909220AFC5C2E858FDE323D667DF2E",
    "trial_font/game_font14_US.spc": "7229D6640110D6C788BBC8D886B224BE1B36376A13DFC4E10D53108CECBBD094",
    "trial_font/game_font15_US.spc": "6B172351BB50567681677B7CE01C7494DE08A3AA2341707ED04D08A8596BD21E",
    "trial_font/game_font16_US.spc": "26AA2839743DEE0CFD7709EAC300DFA9C9E0144F93700F184B98D2D8A763FE97",
    "trial_font/game_font17_US.spc": "337DBA554E4DA01F24120384498268FCF4450E615D8B8CA410C53CDEC27E5184",
    "trial_font/game_font18_US.spc": "01ED4290B4EF8B0529CEE0BE483FD8BCCC00928F796962594B26D71F28128097",
    "trial_font/game_font19_US.spc": "12AFCACEFAD1CD5B6B834222EC0F766EF643EA3D69D8F18D1F675A561B741708",
    "trial_font/game_font20_US.spc": "958B65E232E9F873BD15CC9353616CA3FE2D3B2B28467C8BE5D382A9013E5D35",
    "trial_font/game_font21_US.spc": "0115FA65214DEE19BD55D855BADBD67241D4114ED7B731F4E3BD14351C4ED1BC",
    "trial_font/game_font29_US.spc": "E93CC546FDBBB1E560C9E969D490A4BCFB0AB1120944BA1C3656FDA02B6AA646",
    "trial_font/game_font30_US.spc": "FB60D1166F91EA481F71DD680CB2A59660629D95EFADBF22FC62AF995CDCBDB1",
    "wrd_data/map_obj_name_text_US.SPC": "FE44534B5FA7E1B8B1285846EF9989B3E75E985516B2E364DCA58C79D7EE7142",
    "wrd_script/003/ainori_text_US.SPC": "7D4DA092C20649EF8684D6EB12B0CFD439FA62EE1CC4183E1632D54786EE3A1A",
    "wrd_script/003/chap1_text_US.SPC": "F5BD5484EEF2EEE59C061E7C9B6BE2A164C0F1C5B8EF060CE08084AF7AE1F5F0",
    "wrd_script/004/ainori_text_US.SPC": "7D4DA092C20649EF8684D6EB12B0CFD439FA62EE1CC4183E1632D54786EE3A1A",
    "wrd_script/004/chap1_text_US.SPC": "9FFCEF005DB14B378151CC1290402D2B18F5AA624B05C61D0432AE4956FD11C3",
    "wrd_script/005/ainori_text_US.SPC": "7D4DA092C20649EF8684D6EB12B0CFD439FA62EE1CC4183E1632D54786EE3A1A",
    "wrd_script/005/chap1_text_US.SPC": "29A1D0DA042B583829930CF7FE5CD685D18273CE6F4CB4584468B7F4BF1E9B55",
    "wrd_script/006/ainori_text_US.SPC": "7D4DA092C20649EF8684D6EB12B0CFD439FA62EE1CC4183E1632D54786EE3A1A",
    "wrd_script/006/chap1_text_US.SPC": "FB97B480D3BF7DDAE54CA9ACC15E5407C61B25DEF9B9BE9243EEB8E49B93CD50",
    "wrd_script/007/ainori_text_US.SPC": "C6AAD448DD453A2FED3E71E9D63915473709969561119A082B3ECDE9827FD05B",
    "wrd_script/007/chap0_text_US.SPC": "EFF95C172732F5358B942C5695E001367D78ED23DBB0FFA3A5F61C06D8BB6032",
    "wrd_script/007/chap1_text_US.SPC": "F7B01E79301E6BDB11BECC9F65B5ECBACB9ABC8E76FAE22FE034A9AE9F91C617",
    "wrd_script/007/chap2_text_US.SPC": "8281AD9BE85D9BB2AD0B88756B83D521F7A1BEB0D480DA417C8B68EF0652692E",
    "wrd_script/007/chap3_text_US.SPC": "F4F29D0011B5D9FE97332FE928CEC7B079C32BCE7C4BDF7B3DCCFDCA10F08B5A",
    "wrd_script/007/chap4_text_US.SPC": "FF341408EF36059ADF954BFBA3A57C8F279182D74DC6B4BC5D06D7237F488A6C",
    "wrd_script/007/chap5_text_US.SPC": "9C934A5C68473279ECBE7833FD7C6762C9ABC9FA3E4F1E3C95D6CA8B7EF29E69",
    "wrd_script/007/chap6_text_US.SPC": "4831F9ED2FA77FC3BA23DCA7D13C8F5EAFC56B75BA2D5370E6430EF3D2178806",
    "wrd_script/007/chap7_text_US.SPC": "EE16D90420B989DDA065718FB188D54A8B895E535435314E1ECC062A9A60B4FD",
    "wrd_script/007/gallery_text_US.SPC": "CB083059076EC88F66F2FA1585C8FF0FC10626E00FB46338085E9AA1F95736E2",
    "wrd_script/007/sub_routine_text_US.SPC": "9551BC7FC92C03A6350EEFCC51CC01F6CB91AEDB46E26CDCF1218F7122D19D48",
}

TYPE_SIZES = {0x0: 1, 0x1: 1, 0x2: 2, 0x3: 2, 0x4: 4, 0x5: 4, 0x6: 8, 0x7: 8, 0x8: 4, 0xA: 4, 0xB: 8}


class ProgressWriter:
    def __init__(self, original, progress_stream):
        self.original = original
        self.progress_stream = progress_stream

    def write(self, text: str) -> int:
        if self.original is not None:
            self.original.write(text)
            self.original.flush()
        self.progress_stream.write(text)
        self.progress_stream.flush()
        return len(text)

    def flush(self) -> None:
        if self.original is not None:
            self.original.flush()
        self.progress_stream.flush()


@dataclass(frozen=True)
class Field:
    value: Any


def resource_root() -> Path:
    if getattr(sys, "frozen", False):
        return Path(getattr(sys, "_MEIPASS", Path(sys.executable).parent))
    return Path(__file__).resolve().parent.parent


def payload_root() -> Path:
    return resource_root() / "mod_data"


def read_cstring(data: mmap.mmap, offset: int) -> str:
    end = data.find(b"\0", offset)
    if end < 0:
        raise ValueError(f"Unterminated UTF string at 0x{offset:X}")
    return data[offset:end].decode("utf-8", errors="strict")


def decode_value(data: mmap.mmap, offset: int, type_id: int, strings: int) -> Any:
    formats = {0x0: ">B", 0x1: ">b", 0x2: ">H", 0x3: ">h", 0x4: ">I", 0x5: ">i", 0x6: ">Q", 0x7: ">q", 0x8: ">f"}
    if type_id in formats:
        return struct.unpack_from(formats[type_id], data, offset)[0]
    if type_id == 0xA:
        return read_cstring(data, strings + struct.unpack_from(">I", data, offset)[0])
    if type_id == 0xB:
        return struct.unpack_from(">II", data, offset)
    raise ValueError(f"Unsupported UTF type 0x{type_id:X}")


def parse_utf(data: mmap.mmap, offset: int) -> list[dict[str, Field]]:
    if data[offset:offset + 4] != b"@UTF":
        raise ValueError(f"Missing @UTF at 0x{offset:X}")
    rows_offset = offset + 8 + struct.unpack_from(">I", data, offset + 8)[0]
    strings = offset + 8 + struct.unpack_from(">I", data, offset + 12)[0]
    column_count, row_length = struct.unpack_from(">HH", data, offset + 24)
    row_count = struct.unpack_from(">I", data, offset + 28)[0]
    definitions: list[tuple[str, int, int, int | None, int | None]] = []
    pos = offset + 32
    per_row_pos = 0
    for _ in range(column_count):
        flags = data[pos]
        pos += 1
        name_offset = struct.unpack_from(">I", data, pos)[0]
        pos += 4
        storage, type_id = flags & 0xF0, flags & 0x0F
        if type_id not in TYPE_SIZES:
            raise ValueError(f"Unsupported UTF type 0x{type_id:X}")
        const_offset = row_offset = None
        if storage == 0x30:
            const_offset = pos
            pos += TYPE_SIZES[type_id]
        elif storage == 0x50:
            row_offset = per_row_pos
            per_row_pos += TYPE_SIZES[type_id]
        elif storage != 0x10:
            raise ValueError(f"Unsupported UTF storage 0x{storage:X}")
        definitions.append((read_cstring(data, strings + name_offset), storage, type_id, const_offset, row_offset))
    if per_row_pos != row_length:
        raise ValueError("Invalid UTF row length")
    parsed: list[dict[str, Field]] = []
    for row_index in range(row_count):
        base = rows_offset + row_index * row_length
        row: dict[str, Field] = {}
        for name, storage, type_id, const_offset, row_offset in definitions:
            if storage == 0x10:
                row[name] = Field(0)
            else:
                value_offset = const_offset if storage == 0x30 else base + int(row_offset)
                row[name] = Field(decode_value(data, int(value_offset), type_id, strings))
        parsed.append(row)
    return parsed


class ReverseBitReader:
    def __init__(self, data: bytes):
        self.data = data[::-1]
        self.bit_pos = 0

    def read(self, count: int) -> int:
        value = 0
        for _ in range(count):
            if self.bit_pos >= len(self.data) * 8:
                raise ValueError("Unexpected end of CRILAYLA bit stream")
            byte = self.data[self.bit_pos // 8]
            bit = (byte >> (7 - (self.bit_pos % 8))) & 1
            value = (value << 1) | bit
            self.bit_pos += 1
        return value


def decompress_crilayla(blob: bytes, expected_size: int) -> bytes:
    if not blob.startswith(b"CRILAYLA"):
        if len(blob) != expected_size:
            raise ValueError(f"Stored file size mismatch ({len(blob)} != {expected_size})")
        return blob
    if len(blob) < 0x110:
        raise ValueError("Truncated CRILAYLA file")
    unpacked_tail_size, compressed_size = struct.unpack_from("<II", blob, 8)
    if compressed_size + 0x110 > len(blob):
        raise ValueError("Invalid CRILAYLA compressed size")
    reader = ReverseBitReader(blob[0x10:0x10 + compressed_size])
    reversed_tail = bytearray()
    widths = (2, 3, 5)
    while len(reversed_tail) < unpacked_tail_size:
        if reader.read(1) == 0:
            reversed_tail.append(reader.read(8))
            continue
        distance = reader.read(13) + 3
        length = 3
        index = 0
        while True:
            width = widths[index] if index < len(widths) else 8
            part = reader.read(width)
            length += part
            if part != (1 << width) - 1:
                break
            index += 1
        if distance > len(reversed_tail):
            raise ValueError("Invalid CRILAYLA back-reference")
        for _ in range(min(length, unpacked_tail_size - len(reversed_tail))):
            reversed_tail.append(reversed_tail[-distance])
    header = blob[0x10 + compressed_size:0x110 + compressed_size]
    result = header + bytes(reversed_tail[::-1])
    if len(result) != expected_size:
        raise ValueError(f"CRILAYLA output size mismatch ({len(result)} != {expected_size})")
    return result


def archive_rows(cpk_path: Path) -> tuple[list[dict[str, Any]], int]:
    with cpk_path.open("rb") as stream, mmap.mmap(stream.fileno(), 0, access=mmap.ACCESS_READ) as data:
        if data[:4] != b"CPK ":
            raise ValueError(f"Not a CPK archive: {cpk_path.name}")
        header = parse_utf(data, 0x10)[0]
        content_offset = int(header["ContentOffset"].value)
        toc_offset = int(header["TocOffset"].value)
        base = min(content_offset, toc_offset)
        rows = []
        for row in parse_utf(data, toc_offset + 0x10):
            rel = safe_archive_path(str(row["DirName"].value), str(row["FileName"].value))
            rows.append({
                "path": rel,
                "offset": base + int(row["FileOffset"].value),
                "stored_size": int(row["FileSize"].value),
                "extract_size": int(row["ExtractSize"].value),
            })
        return rows, int(header["ContentSize"].value)


def safe_archive_path(directory: str, filename: str) -> Path:
    raw = "/".join(part for part in (directory.replace("\\", "/").strip("/"), filename) if part)
    posix = PurePosixPath(raw)
    if posix.is_absolute() or not raw or any(part in ("", ".", "..") for part in posix.parts):
        raise ValueError(f"Unsafe archive path: {raw!r}")
    return Path(*posix.parts)


def sha256_file(path: Path, chunk_size: int = 8 * 1024 * 1024) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while chunk := stream.read(chunk_size):
            digest.update(chunk)
    return digest.hexdigest().upper()


def write_file(source: BinaryIO, offset: int, stored_size: int, extract_size: int, destination: Path) -> str:
    source.seek(offset)
    destination.parent.mkdir(parents=True, exist_ok=True)
    signature = source.read(8)
    source.seek(offset)
    if signature == b"CRILAYLA":
        blob = source.read(stored_size)
        if len(blob) != stored_size:
            raise ValueError("Unexpected end of CPK archive")
        output = decompress_crilayla(blob, extract_size)
        destination.write_bytes(output)
        return hashlib.sha256(output).hexdigest().upper()
    if stored_size != extract_size:
        raise ValueError("Unknown compression format in CPK entry")
    digest = hashlib.sha256()
    remaining = stored_size
    with destination.open("wb") as output_stream:
        while remaining:
            chunk = source.read(min(8 * 1024 * 1024, remaining))
            if not chunk:
                raise ValueError("Unexpected end of CPK archive")
            output_stream.write(chunk)
            digest.update(chunk)
            remaining -= len(chunk)
    return digest.hexdigest().upper()


def locate_game(explicit: str | None) -> Path:
    candidates: list[Path] = []
    if explicit:
        candidates.append(Path(explicit.strip('"')))
    candidates.append(Path(r"C:\Program Files (x86)\Steam\steamapps\common\Danganronpa V3 Killing Harmony"))
    candidates.append(Path(r"C:\Program Files\Steam\steamapps\common\Danganronpa V3 Killing Harmony"))
    for candidate in candidates:
        if (candidate / "data" / "win").is_dir():
            return candidate.resolve()
    if sys.stdin.isatty():
        entered = input("วางพาธโฟลเดอร์เกม Danganronpa V3 แล้วกด Enter: ").strip().strip('"')
        candidate = Path(entered)
        if (candidate / "data" / "win").is_dir():
            return candidate.resolve()
    raise FileNotFoundError("ไม่พบโฟลเดอร์เกม ใช้ --game \"พาธโฟลเดอร์เกม\"")


def validate_payload() -> list[Path]:
    root = payload_root()
    if not root.is_dir():
        raise FileNotFoundError(f"Missing mod_data: {root}")
    files = sorted(path for path in root.rglob("*") if path.is_file())
    if not files:
        raise FileNotFoundError("mod_data is empty")
    actual = {path.relative_to(root).as_posix(): path for path in files}
    expected_names = set(EXPECTED_PAYLOAD_SHA256)
    if set(actual) != expected_names:
        missing = sorted(expected_names - set(actual))
        extra = sorted(set(actual) - expected_names)
        raise ValueError(
            f"Payload file list mismatch (missing={missing}, extra={extra})")
    for relative, expected_hash in EXPECTED_PAYLOAD_SHA256.items():
        actual_hash = sha256_file(actual[relative])
        if actual_hash != expected_hash:
            raise ValueError(
                f"Payload hash mismatch: {relative} "
                f"(expected {expected_hash}, got {actual_hash})")
    return files


def verify_supported_cpks(win_dir: Path) -> None:
    print("ตรวจสอบไฟล์เกมต้นฉบับ...")
    for name, expected in CPKS.items():
        path = win_dir / name
        if not path.is_file():
            raise FileNotFoundError(f"ไม่พบ {name} — กรุณา Verify integrity ใน Steam ก่อน")
        if path.stat().st_size != expected["size"] or sha256_file(path) != expected["sha256"]:
            raise ValueError(f"{name} ไม่ใช่ไฟล์ Steam รุ่นที่รองรับ กรุณา Verify integrity ใน Steam")


def install(game: Path) -> None:
    win_dir = game / "data" / "win"
    backup_dir = game / BACKUP_DIR_NAME
    temp_dir = game / TEMP_DIR_NAME
    manifest_path = backup_dir / MANIFEST_NAME
    if manifest_path.exists():
        raise RuntimeError("พบม็อดติดตั้งอยู่แล้ว ให้ถอนการติดตั้งก่อน")
    if temp_dir.exists():
        raise RuntimeError(f"พบโฟลเดอร์งานค้าง: {temp_dir} กรุณาลบหลังตรวจว่าเกมปิดอยู่")
    payload_files = validate_payload()
    verify_supported_cpks(win_dir)
    reuse_existing_backup = False
    if backup_dir.exists():
        if not backup_dir.is_dir():
            raise RuntimeError(f"พาธสำรองมีอยู่แต่ไม่ใช่โฟลเดอร์: {backup_dir}")
        backup_valid = True
        for name, expected in CPKS.items():
            backup = backup_dir / name
            if (
                not backup.is_file()
                or backup.stat().st_size != expected["size"]
                or sha256_file(backup) != expected["sha256"]
            ):
                backup_valid = False
                break
        if backup_valid:
            reuse_existing_backup = True
            print("พบ CPK สำรองเดิมที่ถูกต้อง จะใช้เป็น backup โดยไม่เขียนทับ...")
            for name in CPKS:
                duplicate = backup_dir / f"{name}.installer_current"
                if duplicate.exists():
                    raise RuntimeError(f"พบไฟล์งานค้างใน backup: {duplicate.name}")
        elif any(backup_dir.iterdir()):
            raise RuntimeError(
                f"โฟลเดอร์ {BACKUP_DIR_NAME} มีข้อมูลเดิมแต่ CPK ไม่ครบหรือ hash ไม่ถูกต้อง "
                "กรุณาเปลี่ยนชื่อโฟลเดอร์นี้ก่อนติดตั้ง"
            )
    archive_info: list[tuple[Path, list[dict[str, Any]]]] = []
    all_paths: set[Path] = set()
    required_bytes = 0
    for name in CPKS:
        cpk_path = win_dir / name
        rows, _ = archive_rows(cpk_path)
        archive_info.append((cpk_path, rows))
        for row in rows:
            rel = row["path"]
            if rel in all_paths:
                raise ValueError(f"Duplicate path between archives: {rel}")
            all_paths.add(rel)
            required_bytes += row["extract_size"]
    collisions = [rel for rel in all_paths if (win_dir / rel).exists()]
    if collisions:
        raise RuntimeError(f"พบไฟล์ loose เดิมในโฟลเดอร์เกม ({collisions[0]}) กรุณา Verify integrity/ล้างม็อดเดิมก่อน")
    free = shutil.disk_usage(game).free
    reserve = 1_500_000_000
    if free < required_bytes + reserve:
        need_gb = (required_bytes + reserve) / 1_000_000_000
        raise OSError(f"พื้นที่ว่างไม่พอ ต้องมีอย่างน้อยประมาณ {need_gb:.1f} GB")
    temp_win = temp_dir / "data" / "win"
    temp_win.mkdir(parents=True)
    installed: dict[str, dict[str, Any]] = {}
    try:
        total = sum(len(rows) for _, rows in archive_info)
        done = 0
        for cpk_path, rows in archive_info:
            print(f"แตก {cpk_path.name} ({len(rows)} ไฟล์)...")
            with cpk_path.open("rb") as source:
                for row in rows:
                    rel: Path = row["path"]
                    digest = write_file(source, row["offset"], row["stored_size"], row["extract_size"], temp_win / rel)
                    installed[rel.as_posix()] = {"sha256": digest, "size": row["extract_size"]}
                    done += 1
                    if done % 50 == 0 or done == total:
                        print(f"  {done}/{total}")
        print("ใส่ไฟล์ภาษาไทย...")
        for source in payload_files:
            rel = source.relative_to(payload_root())
            destination = temp_win / rel
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
            installed[rel.as_posix()] = {"sha256": sha256_file(destination), "size": destination.stat().st_size}
        backup_dir.mkdir(parents=True, exist_ok=True)
        moved_cpks: list[tuple[Path, Path]] = []
        moved_items: list[tuple[Path, Path]] = []
        redundant_cpks: list[Path] = []
        try:
            for name in CPKS:
                source = win_dir / name
                destination = backup_dir / name
                if reuse_existing_backup:
                    temporary_duplicate = backup_dir / f"{name}.installer_current"
                    os.replace(source, temporary_duplicate)
                    moved_cpks.append((temporary_duplicate, source))
                    redundant_cpks.append(temporary_duplicate)
                else:
                    os.replace(source, destination)
                    moved_cpks.append((destination, source))
            for item in temp_win.iterdir():
                destination = win_dir / item.name
                if destination.exists():
                    raise RuntimeError(f"Unexpected collision while installing: {destination}")
                os.replace(item, destination)
                moved_items.append((destination, item))
            manifest = {
                "app": APP_NAME,
                "version": APP_VERSION,
                "installed_at": time.strftime("%Y-%m-%dT%H:%M:%S%z"),
                "game": str(game),
                "cpks": CPKS,
                "files": installed,
            }
            manifest_path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8")
            for redundant in redundant_cpks:
                try:
                    redundant.unlink()
                except OSError as error:
                    print(f"คำเตือน: ลบ CPK สำเนาซ้ำไม่ได้ ({redundant.name}): {error}")
        except Exception:
            for installed_item, temporary_item in reversed(moved_items):
                if installed_item.exists() and not temporary_item.exists():
                    os.replace(installed_item, temporary_item)
            for backup, original in reversed(moved_cpks):
                if backup.exists() and not original.exists():
                    os.replace(backup, original)
            raise
    finally:
        if temp_dir.exists():
            shutil.rmtree(temp_dir)
    print("ติดตั้งสำเร็จ เปิดเกมจาก Steam ได้เลย")


def uninstall(game: Path) -> None:
    win_dir = game / "data" / "win"
    backup_dir = game / BACKUP_DIR_NAME
    manifest_path = backup_dir / MANIFEST_NAME
    if not manifest_path.is_file():
        raise FileNotFoundError("ไม่พบข้อมูลการติดตั้งม็อด")
    manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    for name, expected in CPKS.items():
        backup = backup_dir / name
        destination = win_dir / name
        if not backup.is_file() or backup.stat().st_size != expected["size"] or sha256_file(backup) != expected["sha256"]:
            raise RuntimeError(f"ไฟล์สำรอง {name} หายหรือเสียหาย ให้ใช้ Steam Verify integrity")
        if destination.exists():
            raise RuntimeError(f"มี {name} อยู่ใน data\\win แล้ว จึงยังไม่เขียนทับ")
    preserved: list[str] = []
    removed = 0
    candidate_directories: set[Path] = set()
    for rel_text, info in manifest["files"].items():
        rel = safe_archive_path("", rel_text)
        path = win_dir / rel
        candidate_directories.update(path.parents)
        if not path.is_file():
            continue
        if path.stat().st_size == info["size"] and sha256_file(path) == info["sha256"]:
            path.unlink()
            removed += 1
        else:
            preserved.append(rel_text)
    directories = sorted((p for p in candidate_directories if p != win_dir and win_dir in p.parents), key=lambda p: len(p.parts), reverse=True)
    for directory in directories:
        try:
            directory.rmdir()
        except OSError:
            pass
    for name in CPKS:
        backup = backup_dir / name
        destination = win_dir / name
        os.replace(backup, destination)
    manifest_path.unlink()
    try:
        backup_dir.rmdir()
    except OSError:
        pass
    print(f"ถอนการติดตั้งสำเร็จ ลบไฟล์ม็อด/loose {removed} ไฟล์")
    if preserved:
        print(f"เก็บไฟล์ที่ถูกแก้ไขหลังติดตั้งไว้ {len(preserved)} ไฟล์:")
        for rel in preserved[:20]:
            print(f"  {rel}")


def main() -> int:
    original_stdout, original_stderr = sys.stdout, sys.stderr
    for stream in (original_stdout, original_stderr):
        reconfigure = getattr(stream, "reconfigure", None)
        if reconfigure is not None:
            reconfigure(encoding="utf-8", errors="backslashreplace")
    parser = argparse.ArgumentParser(description=APP_NAME)
    parser.add_argument("--game", help="พาธโฟลเดอร์เกม")
    parser.add_argument("--uninstall", action="store_true", help="ถอนการติดตั้ง")
    parser.add_argument("--progress-file", help=argparse.SUPPRESS)
    parser.add_argument("--no-pause", action="store_true", help=argparse.SUPPRESS)
    parser.add_argument("--verify-payload", action="store_true",
                        help="ตรวจรายการและ SHA-256 ของ payload แล้วออก")
    args = parser.parse_args()
    progress_stream = None
    if args.progress_file:
        progress_path = Path(args.progress_file)
        progress_path.parent.mkdir(parents=True, exist_ok=True)
        progress_stream = progress_path.open("w", encoding="utf-8", buffering=1)
        sys.stdout = ProgressWriter(original_stdout, progress_stream)
        sys.stderr = ProgressWriter(original_stderr, progress_stream)
    exit_code = 0
    try:
        print(f"{APP_NAME} v{APP_VERSION}")
        if args.verify_payload:
            files = validate_payload()
            print(f"Payload verified: {len(files)} files, all SHA-256 hashes match")
        else:
            game = locate_game(args.game)
            print(f"เกม: {game}")
            if args.uninstall:
                uninstall(game)
            else:
                install(game)
    except Exception as error:
        exit_code = 1
        print(f"\nผิดพลาด: {error}", file=sys.stderr)
    if not args.no_pause and sys.stdin.isatty():
        input("\nกด Enter เพื่อปิด...")
    if progress_stream is not None:
        sys.stdout.flush()
        sys.stderr.flush()
        sys.stdout, sys.stderr = original_stdout, original_stderr
        progress_stream.close()
    return exit_code


if __name__ == "__main__":
    raise SystemExit(main())
