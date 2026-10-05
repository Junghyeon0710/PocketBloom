# 재현 가능한 배치 테스트와 빌드

Unity 6000.6.2f1과 Android Build Support/SDK/NDK/OpenJDK가 설치된 Windows 환경 기준입니다. 이 명령은 해당 프로젝트를 연 Unity 편집기가 없을 때 실행합니다. 실행 중인 빌드가 있다면 새로 시작하기 전에 그 프로세스의 완료 여부를 확인합니다.

PowerShell에서 프로젝트 루트로 이동한 뒤 실행합니다. `unity run`과 `unity test`가 배치 모드와 종료 플래그를 관리하므로 별도로 `-batchmode`나 `-quit`을 추가하지 않습니다.

## 규칙과 광고 보상 순서 테스트

```powershell
unity test 'D:\Unity Projects\Codex' --mode EditMode --filter PocketBloom.Tests --output 'D:\Unity Projects\Codex\Docs\Validation\EditModeTests-latest.xml' --format json -- -nographics -buildTarget Android -logFile 'D:\Unity Projects\Codex\Logs\PocketBloom-Tests.log'
```

NUnit XML의 `total`, `passed`, `failed`와 각 테스트 결과를 확인합니다. 현재 13개 테스트는 규칙 8개와 보상 콜백 처리 5개입니다. 실제 광고 네트워크나 단말을 대신하는 테스트는 아닙니다.

## Android 테스트 APK

```powershell
unity run 'D:\Unity Projects\Codex' --format json -- -nographics -buildTarget Android -executeMethod PocketBloom.Editor.BloomProjectBuilder.BuildAndroid -logFile 'D:\Unity Projects\Codex\Logs\PocketBloom-Android-Batch.log'
```

출력: `Builds/Android/PocketBloom-test.apk`. 성공 여부는 CLI 종료 코드와 `Docs/Validation/Build-Android.json`의 **이번 실행 시각**을 함께 확인합니다. 오래된 성공 보고서를 현재 빌드 결과로 취급하지 않습니다.

## Windows 미리보기

```powershell
unity run 'D:\Unity Projects\Codex' --format json -- -nographics -buildTarget Win64 -executeMethod PocketBloom.Editor.BloomProjectBuilder.BuildWindows -logFile 'D:\Unity Projects\Codex\Logs\PocketBloom-Windows-Batch.log'
```

출력: `Builds/Windows/PocketBloom.exe`와 같은 폴더의 데이터/엔진 파일 전체입니다. EXE 하나만 복사하면 실행되지 않습니다. `*BackUpThisFolder_ButDontShipItWithYourGame` 폴더는 배포 ZIP에서 제외합니다.

## APK 정적 검사

Python 3.11 이상에서 다음을 실행합니다.

```powershell
python Tools/InspectApk.py Builds/Android/PocketBloom-test.apk Docs/Validation/ApkInspection.json
```

Android SDK Build Tools의 `aapt2 dump badging`, `apksigner verify --verbose`, `zipalign -c -P 16 -v 4`로 패키지·서명·ZIP 정렬도 별도로 검사합니다. 정적 검사와 빌드 성공은 실제 휴대폰 설치·플레이·광고 검증을 대신하지 않습니다.

출시 AAB는 사용자 소유 광고 계정, 개인정보 동의 제공자, 서명과 실기기 검증을 완료한 뒤 생성합니다. 현재 테스트 APK를 스토어 출시본으로 간주하지 않습니다.
