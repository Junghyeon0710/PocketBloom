"""Read-only package inspection; this does not prove Android runtime behavior."""
import argparse
import hashlib
import json
import struct
import zipfile
from datetime import datetime, timezone
from pathlib import Path

parser = argparse.ArgumentParser()
parser.add_argument("apk", type=Path)
parser.add_argument("output", type=Path)
args = parser.parse_args()
native = []
with zipfile.ZipFile(args.apk) as archive:
    for entry in archive.infolist():
        if not entry.filename.startswith("lib/") or not entry.filename.endswith(".so"):
            continue
        data = archive.read(entry)
        if data[:4] != b"\x7fELF" or data[5] != 1:
            raise ValueError("Expected a little-endian ELF library: " + entry.filename)
        is64 = data[4] == 2
        phoff = struct.unpack_from("<Q" if is64 else "<I", data, 32 if is64 else 28)[0]
        entsize, count = struct.unpack_from("<HH", data, 54 if is64 else 42)
        segments = []
        for index in range(count):
            offset = phoff + index * entsize
            if struct.unpack_from("<I", data, offset)[0] != 1:
                continue
            file_offset, virtual_address = struct.unpack_from("<QQ" if is64 else "<II", data, offset + (8 if is64 else 4))
            alignment = struct.unpack_from("<Q" if is64 else "<I", data, offset + (48 if is64 else 28))[0]
            segments.append({"alignment": alignment, "compatible16KB": alignment >= 16384 and (virtual_address - file_offset) % 16384 == 0})
        native.append({"path": entry.filename, "bytes": len(data), "machine": struct.unpack_from("<H", data, 18)[0], "loadSegments": segments})

report = {
    "utc": datetime.now(timezone.utc).isoformat(),
    "apk": str(args.apk.resolve()),
    "bytes": args.apk.stat().st_size,
    "sha256": hashlib.file_digest(args.apk.open("rb"), "sha256").hexdigest(),
    "abis": sorted({item["path"].split("/")[1] for item in native}),
    "allNativeELF16KBCompatible": bool(native) and all(item["loadSegments"] and all(seg["compatible16KB"] for seg in item["loadSegments"]) for item in native),
    "nativeLibraries": native,
    "scope": "Static ELF inspection only. ZIP alignment, installation, ads and physical-device behavior require separate verification.",
}
args.output.parent.mkdir(parents=True, exist_ok=True)
args.output.write_text(json.dumps(report, indent=2), encoding="utf-8")
print(json.dumps({key: value for key, value in report.items() if key != "nativeLibraries"}, indent=2))
