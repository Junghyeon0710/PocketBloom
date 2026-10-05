# 검증 범위

2026-10-05 Sunlit Garden UI의 삽화 중앙 정렬·공통 여백·설정 열 배치와 목표 달성 팝업을 보완한 뒤 화면·터치·플레이 흐름·촬영과 빌드를 갱신했습니다. 규칙 13개와 콘텐츠 전수 검사는 이전 2026-10-04 기록이며 이번 변경에서 퍼즐 규칙을 수정하지 않았습니다. 각 결과 파일은 해당 범위만 증명합니다.

최신 `VisualLayout.json`은 한/영 × 세 화면 비율 × 화면·팝업 12종의 72조합을 검사했습니다. 잘린 글자·부족한 텍스트 높이·화면 밖 버튼·삽화 중심 오차가 0개였으며, 성공 카드의 화면 중앙 배치와 움직임 줄이기 설정에서의 최종 점수 표시를 확인했습니다. 마지막 단계·일일 성공 팝업은 표시용 상태 fixture로 구분합니다. `Layouts.json`의 30조합은 이전 기본 화면 검사이며 최신 팝업 보완 검사는 `VisualLayout.json`을 기준으로 봅니다.

실제 플레이 프레임에서 가상 터치를 전달해 1수·1줄·95점을 확인했고, 정상 규칙으로 10수·380점의 첫 여행 완료·다음 단계·중복 보상 방지·무료 도구·저장 읽기·언어 전환을 다시 통과했습니다. 촬영 입력은 에디터 백그라운드에서도 처리하도록 촬영 중에만 설정하며 끝나면 복원합니다. `TouchDrag.json`, `PlayFlow.json`, `Docs/Media/GameplayCapture.json`이 근거입니다. 새 팝업을 담은 사진과 영상은 정상 플레이 결과이며 표시용 fixture를 사용하지 않았습니다.

최종 Windows와 Android 빌드는 2026-10-05에 오류 0개로 완료했습니다. 에디터 검사 중 임시로 바뀐 실행·빠른 플레이 설정은 원래 값으로 복원하고 최종 빌드했습니다. `UISessionRestore.json`은 저장 파일 세 경로의 바이트 복원을 기록합니다. 최신 APK 크기와 SHA256은 `ApkInspection.json`에 있으며 debug v2 서명·ARM64 ELF·ZIP의 16KB 정렬 검사를 통과했습니다.

| 증거 | 확인한 범위 | 확인하지 못한 범위 |
|---|---|---|
| EditModeTests-latest.xml | 전체 13개 통과: 규칙 8개와 광고 보상 순서 5개. 36단계 승리 경로, 합법 배치 10,000수 | 실제 이용자 난이도·재미·유지율, 실제 SDK 콜백 |
| PlayFlow.json | 실제 게임 코드의 결과·다음 단계·도구·저장 파일 읽기 | 앱 프로세스 재시작 후 저장 복원 |
| TouchDrag.json | 가상 Touchscreen → Input System → 실제 UI 이벤트 → 첫 줄 제거 | 물리적 Android 터치·손가락 가림·단말 지연 |
| VisualLayout.json | 한/영 화면·팝업 12종 × 세 화면 비율, 72조합. 삽화 메시 중심·텍스트 높이·버튼 경계·결과 중심·동작 감소 표시 | 실기기 노치·접근성 설정. 마지막 단계·일일 결과는 표시 fixture |
| Layouts.json | 이전 한/영 기본 화면 × 세 화면 비율, 30조합 | 최신 팝업 보완·실기기 |
| PrivacyDialog.json / privacy-ko.png | 540×960 에디터 한국어 개인정보 안내 화면의 문구·버튼·잘림 없음 | 정책 URL 버튼, 실제 CMP와 광고, 단말 |
| Build-StandaloneWindows64.json | Windows 플레이어 빌드 성공 | 패키지 실행 후 직접 조작 |
| Build-Android.json | Android IL2CPP ARM64 APK 빌드 성공 | 실제 단말 설치·실행 |
| ApkInspection.json / ApkZipAlignment.txt | ARM64 네이티브 ELF와 ZIP의 16KB 정렬 | 단말 성능·발열·메모리 |
| ApkSignature.txt / ApkBadging.txt | debug APK v2 서명, min API 26 / target API 36, 권한 목록 | 출시용 개발자 서명·스토어 승인 |
| AndroidManifest-merged.xml | 실제 최종 병합 Manifest, AD_ID 포함 | 모든 광고 네트워크의 실행 동작 |
| StoreAssets.json | 스토어 배너·아이콘 크기와 PNG 형식, 한·영 제목·소개 문자 수. 내보낸 이미지 직접 확인 | 단말 스크린샷·스토어 업로드·출시명 고유성 |

`home-ko.png`, `journey-ko.png`, `game-ko.png`, `collection-ko.png`, `settings-ko.png`는 에디터에서 실행한 실제 게임 화면입니다. `PointerDrag.json`은 결과가 불확실했던 별도 마우스 주입 시도이며 통과 증거에 포함하지 않습니다.

현재 광고 계정이 미설정되어 광고 초기화는 꺼져 있습니다. 실제 광고 표시·보상·수익, 개인정보 동의, Android 실기기와 스토어 승인은 별도 확인이 필요합니다. 광고 SDK가 컴파일되는 것만으로 수익화가 완료된 것은 아닙니다.

Windows 창 자동 실행을 시도했으나 Computer Use 도구가 `Computer Use app approval timed out`을 반환하여 패키지 직접 조작 검증은 미완료입니다.

처음의 GUI 빌드 대기는 이전 기록입니다. 해당 프로세스가 종료된 뒤 별도 배치 빌드를 실행하여 Android APK를 생성했습니다. `AndroidBuildWait.json`은 이전 시점의 기록이며 최신 빌드 결과가 아닙니다. APK 실제 파일 크기는 `ApkInspection.json`을 기준으로 합니다. Unity BuildReport의 `bytes` 값이 실제 APK 크기와 일치하지 않아 배포 크기 표시에는 사용하지 않았습니다.

병합 보고서에서 AD_ID는 Google Ads Identifier/Unity Ads, WAKE_LOCK·RECEIVE_BOOT_COMPLETED·FOREGROUND_SERVICE는 AndroidX WorkManager 2.7.0 의존성에서 추가됨을 확인했습니다. 최종 네트워크와 데이터 수집 범위는 출시 전 실기기·개인정보 검토에 포함해야 합니다.

광고 보상 순서 검증은 실제 게임 어댑터가 사용하는 `BloomRewardLedger`에 대한 테스트입니다. 닫힘 이후 보상, 다른 광고 표시 중 이전 보상, 판 교체, 표시 실패, 중복 보상을 확인했습니다. 실제 네트워크의 이벤트 식별자와 표시 동작은 단말에서 추가 확인해야 합니다. 보상과 닫힘 콜백의 순서는 [Unity 공식 문서](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/rewarded-ad-integration-package)의 비동기 이벤트 규칙을 따릅니다.
