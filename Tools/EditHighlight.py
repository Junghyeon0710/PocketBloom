"""Unity Recorder 실녹화에서 주요 장면을 편집한다. Python 표준 라이브러리 + FFmpeg만 필요."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess


def run(args):
    result = subprocess.run(args, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True, encoding="utf-8", errors="replace")
    if result.returncode:
        raise RuntimeError(result.stderr[-3500:])
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--ffmpeg", default=shutil.which("ffmpeg"))
    parser.add_argument("--source", default="Recordings/PocketBloom/gameplay-raw.mp4")
    args = parser.parse_args()
    if not args.ffmpeg:
        parser.error("Install FFmpeg or pass --ffmpeg <executable>.")
    source = Path(args.source)
    if not source.is_file():
        parser.error("Record gameplay with Pocket Bloom > Media first.")
    output = Path("Docs/Media")
    work = Path("Recordings/PocketBloom/edit")
    output.mkdir(parents=True, exist_ok=True)
    work.mkdir(parents=True, exist_ok=True)
    # 원본 녹화의 프레임 기록에 맞춘 컷. 점수와 플레이 화면을 합성하지 않는다.
    cuts = [
        (0.15, 2.30, "한 칸씩, 나만의 작은 정원"),
        (2.65, 4.35, "36개 정원을 여행해요"),
        (4.70, 7.50, "끌어놓고 · 한 줄을 피우고"),
        (8.00, 10.90, "세 조각의 순서를 생각해요"),
        (11.75, 14.75, "빈 공간이 다시 꽃필 때"),
        (16.80, 19.60, "다음 한 수를 준비해요"),
        (20.65, 24.65, "목표 달성 · 다음 정원으로"),
        (25.10, 27.55, "새로운 목표에 도전해요"),
        (29.75, 31.65, "가로와 세로, 줄을 완성해요"),
        (34.00, 36.10, "플레이로 모으는 꽃 컬렉션"),
        (36.60, 39.45, "한국어와 영어 · 내 취향대로"),
        (39.90, 41.55, "Pocket Bloom · 오늘도 한 칸"),
    ]
    font = "Assets/PocketBloom/Art/NotoSansKR.ttf"
    segments = []
    timeline = []
    position = 0.0
    for index, (start, end, caption) in enumerate(cuts):
        duration = end - start
        caption_file = work / f"caption-{index:02d}.txt"
        caption_file.write_text(caption, encoding="utf-8")
        segment = work / f"clip-{index:02d}.mp4"
        vf = (
            "scale=864:1536:flags=lanczos,pad=1080:1920:108:220:color=0x103C3D,"
            f"drawtext=fontfile='{font}':text='Pocket Bloom':fontsize=76:fontcolor=0xFFF9E9:x=(w-tw)/2:y=55,"
            f"drawtext=fontfile='{font}':text='GAMEPLAY HIGHLIGHTS':fontsize=25:fontcolor=0xBFEAD6:x=(w-tw)/2:y=158,"
            f"drawtext=fontfile='{font}':textfile='{caption_file.as_posix()}':expansion=none:fontsize=43:fontcolor=0xFFF9E9:x=(w-tw)/2:y=1810,"
            f"fade=t=in:st=0:d=0.10,fade=t=out:st={duration-0.10:.3f}:d=0.10"
        )
        run([args.ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-ss", str(start), "-i", str(source),
             "-t", f"{duration:.6f}", "-vf", vf, "-af", f"volume=6,afade=t=in:d=0.04,afade=t=out:st={duration-0.04:.3f}:d=0.04",
             "-r", "30", "-c:v", "libx264", "-preset", "fast", "-crf", "21", "-pix_fmt", "yuv420p",
             "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-movflags", "+faststart", str(segment)])
        segments.append(segment)
        timeline.append(dict(source_start=start, source_end=end, output_start=round(position, 3), caption=caption))
        position += duration
        print(f"Edited {index+1}/{len(cuts)}", flush=True)
    concat_file = work / "concat.txt"
    concat_file.write_text("\n".join(f"file '{item.name}'" for item in segments), encoding="utf-8")
    movie = output / "pocket-bloom-highlight.mp4"
    run([args.ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-f", "concat", "-safe", "0", "-i", str(concat_file),
         "-c", "copy", "-movflags", "+faststart", str(movie)])
    gif = output / "pocket-bloom-highlight.gif"
    run([args.ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-i", str(movie), "-filter_complex",
         "fps=8,scale=320:-1:flags=lanczos,split[a][b];[a]palettegen=max_colors=96:stats_mode=diff[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle",
         "-loop", "0", str(gif)])
    run([args.ffmpeg, "-y", "-hide_banner", "-loglevel", "error", "-ss", "6", "-i", str(movie), "-frames:v", "1",
         str(output / "highlight-poster.jpg")])
    # 완성 영상·GIF 전체를 다시 디코딩해 손상된 출력이 아닌지 확인한다.
    decode = run([args.ffmpeg, "-hide_banner", "-i", str(movie), "-af", "volumedetect", "-f", "null", "-"])
    gif_decode = run([args.ffmpeg, "-hide_banner", "-i", str(gif), "-f", "null", "-"])
    (work / "video-decode.txt").write_text(decode.stderr, encoding="utf-8")
    (work / "gif-decode.txt").write_text(gif_decode.stderr, encoding="utf-8")
    facts = dict(source_scope="Unity Editor recording; automated real Input System touch events",
                 source_sha256=hashlib.file_digest(source.open("rb"), "sha256").hexdigest(),
                 requested_duration_seconds=round(position, 3), width=1080, height=1920, fps=30,
                 audio="Recorded procedural game music and effects; gain and short fades applied",
                 timeline=timeline,
                 outputs=[dict(file=p.as_posix(), bytes=p.stat().st_size, sha256=hashlib.file_digest(p.open("rb"), "sha256").hexdigest())
                          for p in (movie, gif)],
                 video_decode="passed", gif_decode="passed")
    (output / "HighlightEdit.json").write_text(json.dumps(facts, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print(json.dumps({"duration":round(position, 3), "movie_bytes":movie.stat().st_size, "gif_bytes":gif.stat().st_size}))


if __name__ == "__main__":
    main()
