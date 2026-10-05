"""Noto Sans KR 가변 글꼴을 재현 가능한 600 굵기의 정적 글꼴로 변환한다.

Requires fonttools==4.66.1. 원본 글꼴과 OFL 라이선스를 보존한다.
"""
from pathlib import Path
from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont

source = Path("Assets/PocketBloom/Art/NotoSansKR.ttf")
font = TTFont(source)
font = instantiateVariableFont(font, {"wght": 600}, inplace=True)
names = {1: "Pocket Bloom Sans", 2: "SemiBold", 3: "PocketBloomSans-SemiBold-1.0",
         4: "Pocket Bloom Sans SemiBold", 6: "PocketBloomSans-SemiBold",
         16: "Pocket Bloom Sans", 17: "SemiBold"}
for record in font["name"].names:
    if record.nameID in names:
        record.string = names[record.nameID].encode(record.getEncoding())
target = source.with_name("PocketBloomSans-SemiBold.ttf")
font.save(target)
print(f"Created {target}; weight={font['OS/2'].usWeightClass}; static={'fvar' not in font}")
