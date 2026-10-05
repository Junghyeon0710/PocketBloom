# 검증 범위

2026-10-04 기준. 각 결과 파일은 해당 범위만 증명합니다.

| 증거 | 확인한 범위 | 확인하지 못한 범위 |
|---|---|---|
| EditModeTests-latest.xml | 전체 13개 통과: 규칙 8개와 광고 보상 순서 5개. 36단계 승리 경로, 합법 배치 10,000수 | 실제 이용자 난이도·재미·유지율, 실제 SDK 콜백 |
| PlayFlow.json | 실제 게임 코드의 결과·다음 단계·도구·저장 파일 읽기 | 앱 프로세스 재시작 후 저장 복원 |
| TouchDrag.json | 가상 Touchscreen → Input System → 실제 UI 이벤트 → 첫 줄 제거 | 물리적 Android 터치·손가락 가림·단말 지연 |
| Layouts.json | 한/영 주요 화면 × 세 화면 비율, 30조합 | 실기기 노치·접근성 설정 |
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
