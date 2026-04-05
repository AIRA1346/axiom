# Steam 출시 점검 체크리스트

G.S.I 게임의 PC(Steam) 출시를 위한 점검 문서입니다.

---

## 1. 필수 수정 사항 (Critical)

### 1.1 IntroScene 빌드 누락 (수정됨)
- **문제**: `IntroScene.unity`가 Build Settings에 포함되어 있지 않았음.
- **영향**: 빌드 실행 시 인트로(DB 로딩, ACCESS GRANTED 연출)가 건너뛰어지고 SampleScene에서 바로 시작.
- **조치**: Build Settings에 IntroScene을 **첫 번째 씬**으로 추가해야 함.

### 1.2 Steamworks SDK 미연동
- **상태**: Steamworks.NET 또는 Facepunch.Steamworks 패키지 미설치.
- **필요 시**: 업적(Achievements), Steam Cloud 저장, 플레이 타임 기록 등을 사용하려면 Steamworks 연동 필요.
- **권장**: Steam 출시 최소 요구사항은 Steam 클라이언트에서 게임 실행. 업적·클라우드 등은 선택 사항.

### 1.3 회사명 및 식별자
- **companyName**: 현재 `DefaultCompany` → 출시 시 회사/스튜디오명으로 변경 권장.
- **applicationIdentifier (Standalone)**: 현재 `com.UnityTechnologies.com.unity.template.urp-blank`
  - Steam App ID를 받은 후 `com.yourstudio.gsi` 형태로 변경 권장.
  - Steamworks 설정 시 Steam App ID(숫자) 별도 설정 필요.

---

## 2. Player Settings 권장값 (PC)

| 항목 | 현재값 | 권장값 | 설명 |
|------|--------|--------|------|
| defaultScreenWidth | 1024 | 1920 | PC 기본 해상도 |
| defaultScreenHeight | 768 | 1080 | |
| resizableWindow | 0 | **1** | 창 크기 조절 가능 (PC 사용자 선호) |
| runInBackground | 0 | **1** | Alt+Tab 시에도 게임 백그라운드 실행 |
| fullscreenMode | 1 (Fullscreen) | 3 (Exclusive Fullscreen) 또는 0 (Exclusive Fullscreen) | 창 모드 전환 허용 |
| allowFullscreenSwitch | 1 | 1 | Alt+Enter 등 풀스크린 전환 |
| bundleVersion | 0.1.0 | 출시에 맞게 설정 | |

### Unity 스플래시 화면
- **m_ShowUnitySplashScreen: 1**  
  - Unity Personal 무료 라이선스 사용 시 Unity 로고 표시 유지 필요.
  - Pro 라이선스면 0으로 숨김 가능.

---

## 3. 입력 및 조작 (PC)

- **마우스**: `InputManager`에서 `GetMouseButton(0)` 지원 → 클릭/드래그 정상 동작.
- **터치**: 터치 지원(모바일 포팅 시 유리).
- **키보드**: ESC로 메뉴 닫기/종료 등 **전역 단축키 미구현**.
  - 권장: 메인 메뉴 또는 상위 UI에서 `ESC` → 창 닫기/게임 종료 옵션 추가.
- **Application.Quit()**: 명시적 종료 호출 없음. 창 닫기(×)로만 종료 가능 → PC에서 동작함.

---

## 4. 저장 및 데이터

| 항목 | 상태 |
|------|------|
| EncyclopediaData.json | `Application.persistentDataPath` 사용 |
| ItemMetadata.bin | `StreamingAssets` (PC에서 `File.ReadAllBytes` 사용 가능) |
| CodexIndex.bin | `StreamingAssets` 동일 |

- **Windows 경로**: `%USERPROFILE%\AppData\LocalLow\[companyName]\[productName]\`
- **Steam Cloud**: 연동 시 별도 Steamworks API 사용 필요.

---

## 5. 디버깅 및 로그

- **Debug.Log**: 스크립트 다수에서 사용 중 (InputManager, GameManager, UIManager 등).
- **권장**: 릴리스 빌드에서는 `#if UNITY_EDITOR` 또는 로깅 레벨로 감싸서 콘솔 출력 최소화.
- **영향**: 성능 영향은 미미하나, 로그 과다 시 일부 환경에서 프레임 드랍 가능.

---

## 6. 품질 및 성능

- **품질 프리셋**: Standalone → `PC` 프리셋 사용 (m_CurrentQuality: 1).
- **목표**: .cursorrules에 120 FPS 유지 명시.
- **vSyncCount**: 0 (QualitySettings) → 수동 프레임 제한 또는 VSync 설정 확인 권장.

---

## 7. 빌드 설정 요약

### Scenes in Build (수정 후)
1. **IntroScene** (인덱스 0) — 인트로, DB 로딩, SampleScene 전환
2. **SampleScene** — 메인 메뉴 및 게임플레이

### 플랫폼
- **Active Build Target**: PC, Mac & Linux Standalone
- **Scripting Backend**: IL2CPP 또는 Mono (플랫폼별 설정 확인)
- **Architecture**: x86_64 (64비트)

---

## 8. Steam 출시 전 최종 확인

- [ ] Build Settings에 IntroScene이 첫 번째 씬으로 포함됨
- [ ] PC 빌드 성공 및 실행 테스트
- [ ] 인트로 → 메인 메뉴 → 각 메뉴 진입/복귀 정상 동작
- [ ] 인벤토리, 상점, 도감, 도서관, 제작, 장비 등 핵심 플로우 테스트
- [ ] 반응속도/조준 테스트 플레이 확인
- [ ] 창 크기 조절 시 UI 스케일링 확인
- [ ] Alt+Tab 후 복귀 시 정상 동작
- [ ] 저장/로드(도감 등) 정상 동작
- [ ] Steamworks 연동 시: App ID, 업적, Cloud 등 설정 완료

---

## 9. 참고 링크

- [Steamworks 문서](https://partner.steamgames.com/doc/home)
- [Unity Steam Build Guide](https://docs.unity3d.com/Manual/SteamBuild.html)
