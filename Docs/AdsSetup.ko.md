# Pocket Bloom 광고 계정 연결 가이드

현재 Android APK는 광고가 꺼진 테스트 배포본입니다. 인앱결제는 없습니다. 사용자는 아직 광고 계정을 만들지 않았고, 지금은 실제 휴대폰 테스트가 어렵다고 확인했습니다. 아래 계정 준비는 단말 없이 할 수 있으며, 실제 광고 검증은 단말 확보 후 진행합니다.

## 1. 소유자 계정 만들기

[LevelPlay 가입 화면](https://platform.ironsrc.com/partners/identity/signup)에서 본인 소유 계정을 만들고 이메일 인증을 완료합니다. Unity 공식 문서에서 연결한 가입 주소입니다. 약관, 사업자 정보, 지급·세금 정보는 실제 운영 주체의 정보로 계정 소유자가 입력합니다. 대시보드에 추가 자료나 승인 요청이 나오면 해당 안내를 따릅니다. 계정 생성만으로 실제 광고 송출과 지급이 완료되지는 않습니다. [공식 계정 생성 안내](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/create-account)

## 2. Android 앱 등록

LevelPlay 대시보드의 `New App`에서 앱을 추가합니다. 아직 스토어에 공개하지 않았으므로 미출시 앱으로 등록합니다. 공식 문서는 미출시 앱을 임시 이름으로 추가하고 나중에 스토어 정보를 갱신하는 흐름을 지원합니다. 화면 구성이 달라졌다면 계정 내 앱 추가 안내를 기준으로 진행합니다. [공식 앱 등록 안내](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/add-app)

| 입력 항목 | 이 프로젝트의 값 |
|---|---|
| 앱 이름 | Pocket Bloom / 포켓 블룸 |
| 플랫폼 | Android |
| 패키지 이름 | `com.pocketbloom.garden` |
| 스토어 공개 상태 | 미출시 |
| 장르 | 2D 퍼즐 |
| 현재 버전 | 1.0.0 / version code 1 |
| 수익 방식 | 광고만 사용, 인앱결제 없음 |

패키지 이름은 현재 테스트 빌드의 값입니다. 출시 전 소유권·고유성을 확정합니다. 대상 연령·배포 지역은 아직 확정되지 않았습니다. COPPA 등 대상 이용자 질문에 임의의 답을 넣지 말고 실제 출시 대상과 일치시킵니다.

## 3. 광고 단위 두 개 준비

앱의 `Ad units`에서 아래 형식을 만듭니다. 이름은 이 프로젝트에서 구분하기 위한 권장값이며, SDK에 입력할 값은 이름이 아니라 생성된 **Ad unit ID**입니다. [공식 광고 단위 관리 안내](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/ad-units)

| 형식 | 권장 이름 | 게임에서 쓰이는 위치 |
|---|---|---|
| Rewarded | `revive_rewarded` | 실패 결과에서 이용자가 선택한 이어하기. 판당 1회, 중앙 네 줄 정리와 8수 추가 |
| Interstitial | `round_end_interstitial` | 판 종료 후 다음 판으로 넘어갈 때 |

보상 항목 이름은 `revive`, 보상 수량은 `1`로 맞춥니다. 게임은 성공한 보상 콜백을 받아 이어하기 1회를 적용합니다. 배너·Offerwall·구매 상품은 이 게임에 사용하지 않습니다.

전면 광고의 코드 제한은 최소 3판 간격, 세션 시작 후 180초, 광고 사이 180초이며, 보상 광고 종료 후에도 180초간 억제됩니다. 대시보드의 capping/pacing을 추가할 수 있습니다. 이 수치는 초기 설계값으로, 실제 이용자 데이터에 따른 조정은 추후 검증합니다.

## 4. 광고 네트워크 연결

Unity Ads 어댑터와 Android 의존성은 빌드에 포함되어 있지만 네트워크 계정은 연결되지 않았습니다. LevelPlay의 Unity Ads 네트워크 설정에서 공식 가이드의 자동 설정 또는 수동 Game ID/Placement ID 매핑을 완료해야 합니다. 서비스 계정 Secret이나 API Key가 필요한 항목은 소유자가 대시보드에 직접 입력합니다. 이를 게임의 `androidAppKey`와 혼동하지 않습니다. [Unity Ads 네트워크 연결 안내](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/networks/guides/unity-ads)

## 5. 프로젝트에 연결할 값

Unity Project 창에서 `Assets/PocketBloom/Resources/AdConfig.asset`을 선택합니다.

| Inspector 필드 | 가져올 값 | 현재 상태 |
|---|---|---|
| Android App Key | LevelPlay Android 앱의 App Key | 비어 있음 |
| Android Rewarded Id | LevelPlay 보상형 Ad unit ID | 비어 있음 |
| Android Interstitial Id | LevelPlay 전면형 Ad unit ID | 비어 있음 |
| Privacy Policy Url | 운영자가 공개한 실제 HTTPS 개인정보처리방침 | 비어 있음 |
| Enable Ads | 동의 흐름과 테스트 준비 후 활성화 | 꺼짐 |

이어 작업할 때 필요한 앱 식별값은 위의 App Key와 광고 단위 ID 두 개입니다. 로그인 비밀번호·서비스 계정 Secret·지급 정보·키스토어 비밀번호를 채팅이나 저장소에 넣을 필요는 없습니다.

## 6. 광고 활성화 전에 남은 구현

현재 설정 화면의 ‘개인정보와 광고’ 안내는 설명용입니다. **실제 동의 관리 화면(CMP)은 아직 연결되지 않았습니다.** 광고 설정만 켜거나 `InitializeAfterPrivacy(true, false, false)`처럼 값을 고정하여 초기화하지 않습니다.

선택한 CMP에서 실제 이용자 선택과 대상 연령 정보를 얻고, SDK 초기화 전에 반영해야 합니다. LevelPlay는 Google UMP와 Google Additional Consent를 지원하는 호환 CMP의 동의 전달을 안내합니다. 선택한 네트워크·지역·SDK 버전에 맞춰 동의 거부, 재선택, 초기화 순서를 구현하고 확인해야 합니다. 현재 `BloomAds.InitializeAfterPrivacy(...)`는 이를 연결할 진입점이며 CMP 구현 자체가 아닙니다. [공식 동의 설정 안내](https://docs.unity.com/en-us/grow/levelplay/sdk/unity/regulation-advanced-settings)

개인정보처리방침에는 확정된 운영자 연락처와 실제 SDK/네트워크 데이터 처리 내용을 반영해야 합니다. 확인되지 않은 내용을 완성된 정책으로 게시하지 않습니다.

## 7. 단말 확보 후 검증

1. 현재 테스트 APK로 설치·터치·저장 복원·백그라운드 복귀를 확인합니다.
2. 계정·동의 흐름을 연결한 새 테스트 빌드를 만듭니다. 기존 APK에는 나중에 입력한 설정이 자동 반영되지 않습니다.
3. LevelPlay `Test devices`에 단말을 등록하고 테스트 광고로 각 네트워크의 로드·표시를 확인합니다. [공식 통합 테스트 안내](https://docs.unity.com/en-us/grow/levelplay/platform/get-started/integration-testing)
4. 보상 완료·취소·표시 실패·오프라인 복귀·늦은 콜백·중복 콜백과 전면 광고 간격을 확인합니다. 현재 13개 코드 테스트 통과가 이 단말 검증을 대신하지 않습니다.
5. 소유자 서명으로 출시 AAB를 만들고 비공개 테스트를 거칩니다. 현재 APK는 debug 서명입니다.

단말 테스트를 마친 뒤 [출시 확인표](ReleaseChecklist.md)의 미완료 항목을 증거와 함께 갱신합니다.
