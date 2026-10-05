# Google Play 등록 자료 초안

스토어 계정 업로드 전 사용할 로컬 자료입니다. 실제 스토어 등록·승인이나 출시 완료를 의미하지 않습니다.

| 파일 | 용도 | 검사 기준 |
|---|---|---|
| `feature-graphic.png` | 소개 배너 | 1024×500, 24-bit PNG, 알파 없음 |
| `app-icon.png` | 스토어 아이콘 | 512×512, 32-bit PNG, 1024KB 이하 |
| `ko-KR/*.txt` | 한국어 제목·짧은 소개·전체 소개 | 각각 30·80·4000자 이내 |
| `en-US/*.txt` | 영어 제목·짧은 소개·전체 소개 | 각각 30·80·4000자 이내 |
| `Source/FeatureGraphic-master.png` | 배너 생성 원본 | ImageGen 원본 보존 |
| `Source/FeatureGraphic.prompt.txt` | 제작 프롬프트 | 내장 ImageGen 사용, 기존 정원 아트 참조 |

규격은 [Google Play 미리보기 소재 안내](https://support.google.com/googleplay/android-developer/answer/9866151?hl=en)와 [앱 등록 안내](https://support.google.com/googleplay/android-developer/answer/9859152?hl=en)를 기준으로 확인했습니다. 업로드 시점의 요구사항과 실제 파일을 다시 확인합니다.

배너는 정원 테마를 보여 주는 삽화이며 실제 게임 스크린샷이 아닙니다. 본문은 현재 구현된 기능을 설명합니다. 검증되지 않은 순위·수익·다운로드 수를 표현하지 않았습니다. 광고가 포함된 출시 빌드를 업로드할 때에는 Play Console의 광고 포함 항목과 데이터 보안 내용을 실제 동작에 맞춰 작성해야 합니다.

생성 도구의 배너 원본은 요청 크기와 다른 1794×876이어서 원본을 보존하고 `Tools/ExportStoreAssets.ps1`로 업로드 크기·색상 형식만 변환했습니다. 기존 아이콘 원본도 유지했습니다. 재현 명령:

```powershell
powershell -NoProfile -File Tools/ExportStoreAssets.ps1
```

결과는 `Docs/Validation/StoreAssets.json`에 크기·형식·해시·문자 수와 함께 기록됩니다.

## 아직 필요한 항목

- 출시명과 패키지 이름의 소유권·고유성 확정
- 최종 Android 빌드의 실제 단말 스크린샷. 추천 노출용 가이드에 맞추려면 세로 1080×1920 게임 화면을 최소 3장 준비
- 개발자 표시 이름, 문의 주소, 실제 개인정보처리방침 URL
- 광고 계정·동의 흐름 연결 및 단말 검증
- 서명된 AAB, 스토어 입력과 요구 테스트, 최종 문구 확인

기존 에디터 캡처는 `Docs/Validation`에 있습니다. 이를 Android 단말에서 촬영한 이미지로 표시하지 않습니다.
