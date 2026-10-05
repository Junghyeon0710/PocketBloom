# 실제 플레이 하이라이트

2026-10-05 Sunlit Garden UI로 다시 촬영했습니다. [선택 시안과 UI 적용 기록](../UIRefresh/README.md)을 참고하세요. 영상의 테두리·제목·한국어 자막도 새 UI와 같은 크림·녹색 팔레트로 맞췄습니다.

`pocket-bloom-highlight.mp4`는 Unity 6000.6.2f1의 실제 Game View를 Unity Recorder 5.1.7로 녹화한 뒤 FFmpeg로 편집한 영상입니다. 1080×1920, 30fps, 약 30초이며 실제 게임 음악·효과음을 포함합니다. README용 `pocket-bloom-highlight.gif`는 같은 영상의 무음 미리보기입니다.

새 촬영용 프로필에서 Input System의 터치 이벤트로 조각을 끌어놓았습니다. 첫 여행을 10수·380점으로 완료하고, 다음 단계·정원 컬렉션·언어 전환을 담았습니다. 촬영 뒤 기존 프로필과 저장 파일 3개 경로의 복원을 확인했습니다. 보드·점수·보상 값을 홍보용으로 합성하지 않았습니다. Android 실기기 녹화가 아니며 광고가 동작하는 장면은 포함하지 않습니다.

## 다시 촬영하고 편집하기

1. `Assets/PocketBloom/Scenes/PocketBloom.unity`를 열고 Play를 누릅니다.
2. `Pocket Bloom > Media > Record Gameplay Showcase (Play Mode)`를 실행합니다.
3. 녹화 상태가 `Temp/PocketBloomMedia/capture-status.json`에서 `completed`가 된 뒤 Play를 멈춥니다.
4. FFmpeg를 설치한 환경에서 아래 명령으로 편집합니다. 실행 파일 경로가 필요하면 `--ffmpeg`를 지정합니다.

```powershell
python Tools/EditHighlight.py --ffmpeg "C:/path/to/ffmpeg.exe"
```

원본은 Git에서 제외된 `Recordings/PocketBloom/gameplay-raw.mp4`에 저장됩니다. 결과는 이 폴더의 MP4·GIF·포스터와 `Screenshots`에 있습니다. `GameplayCapture.json`은 실제 입력의 진행 기록, `HighlightEdit.json`은 원본 컷 구간·자막·해시·디코딩 검사 기록입니다.

영상은 컷 편집, 짧은 페이드, 제목과 한국어 자막, 소리 크기 조정을 적용했습니다. 외부 음악·AI 생성 플레이 영상은 사용하지 않았습니다. Noto Sans KR 라이선스는 프로젝트에 동봉되어 있습니다.
