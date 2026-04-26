# Steam 출시 점검 체크리스트

G.S.I 게임의 PC(Steam) 출시를 위한 점검 문서입니다.

---

## 1. 필수 수정 사항 (Critical)

### 1.1 씬 빌드 순서 (현재 상태 점검)
- **Build Settings** (`EditorBuildSettings.asset`): `IntroScene` → `LobbyScene` → `ShopScene` → `InventoryScene` → `AltarOfVerityScene` → `GSIScene` 순으로 포함됨.
- **조치**: 씬 추가/제거 시 **인트로가 인덱스 0**인지, 중복 가이드가 없는지 출시 직전에 다시 확인.

### 1.2 Steamworks: 패키지 + 스크립팅 심볼
- **패키지**: `com.rlabrecque.steamworks.net`(Steamworks.NET)이 `Packages/manifest.json`에 있음.
- **우리 코드** (`SteamworksService`): `STEAMWORKS_ENABLED`가 정의돼 있을 때만 `SteamAPI.Init` 등 **실제 초기화**를 수행함.
- **프로젝트 설정**: `STEAMWORKS_NET` + **`STEAMWORKS_ENABLED`** 를 **Editor** / **Standalone** 용 `scriptingDefineSymbols`에 포함해 둠(레포 기준). 비활성화는 `Tools → GSI → Steam → Disable STEAMWORKS_ENABLED`로 동일.
- **로컬 개발**: 프로젝트 루트(GSI)에 `steam_appid.txt`(또는 `.example` 참고) — Steam 클라이언트 실행 후 테스트. **출시(Steam) 빌드**에서는 Valve 권고대로 depot에 `steam_appid`를 잘못 넣지 않도록 주의.
- **구현됨(레포 기준)**: Steam **Remote Storage** + 부팅 병합 / 종료 푸시(`GsiSteamCloudSync`), **Rich Presence** 상태 문자열(`GsiSteamRichPresence` — Friends / 오버레이). 업적·리더보드는 제품에서 필요할 때 Partner 설정.

### 1.3 회사명 및 식별자 (현재)
- **companyName**: `RunicAtelier` (`ProjectSettings`)
- **productName**: `The Axiom`
- **applicationIdentifier (Standalone 등)**: `com.runicatelier.gsi`
- **bundleVersion**: 출시에 맞게 갱신(레포 기준 `0.1.1` 이상; 빌드마다 `ProjectSettings`에서 확인).
- **Steam App ID(숫자)**: Partner 사이트에서 별도 발급; 스토어 depots / SDK 설정에 사용.

---

## 2. Player Settings 권장값 (PC)

| 항목 | 현재값 | 권장값 | 설명 |
|------|--------|--------|------|
| defaultScreenWidth | 1920 | 1920 | PC 기본 해상도 |
| defaultScreenHeight | 1080 | 1080 | |
| resizableWindow | **1** | **1** | 창 크기 조절 |
| runInBackground | **1** | **1** | Alt+Tab 시에도 백그라운드 실행 |
| fullscreenMode | **3** (Exclusive Fullscreen) | 3 또는 0 (Exclusive) | `ProjectSettings` 기준; 출시 직전 한 번 더 확인 |
| allowFullscreenSwitch | 1 | 1 | Alt+Enter 등 |
| bundleVersion | (상단 §1.3) | 출시에 맞게 | |

### Unity 스플래시 화면
- **m_ShowUnitySplashScreen: 1**  
  - Unity Personal 무료 라이선스 사용 시 Unity 로고 표시 유지 필요.
  - Pro 라이선스면 0으로 숨김 가능.

---

## 3. 입력 및 조작 (PC)

- **마우스**: `InputManager`에서 `GetMouseButton(0)` 지원 → 클릭/드래그 정상 동작.
- **터치**: 터치 지원(모바일 포팅 시 유리).
- **키보드**: `GlobalSettingsOverlay`에서 **ESC** 또는 게임패드 **Start**로 설정 패널 열기/닫기(인트로 씬은 제외).
- **게임 종료**: 설정 메뉴 **Quit to desktop** / 한글 **게임 종료** — 릴리스 빌드에서 `Application.Quit()` 호출(에디터에선 재생 모드만 종료).

---

## 4. 저장 및 데이터

| 항목 | 상태 |
|------|------|
| EncyclopediaData.json | `Application.persistentDataPath` 사용 |
| ItemMetadata.bin | `StreamingAssets` (PC에서 `File.ReadAllBytes` 사용 가능) |
| CodexIndex.bin | `StreamingAssets` 동일 |

- **Windows 경로**: `%USERPROFILE%\AppData\LocalLow\[companyName]\[productName]\`
- **Steam Cloud**: `GsiSteamCloudSync` + Remote Storage(Partner에서 클라우드·쿼터 설정).

---

## 5. 디버깅 및 로그

- **Debug.Log**: `GameManager` 등 일부 **개발용 로그**는 `GsiLog.Dev` + `[Conditional("UNITY_EDITOR","DEVELOPMENT_BUILD")]`로 릴리스 플레이어에서 호출이 제거됨.
- **권장**: 추가 디버그 출력도 동일 패턴 또는 명시적 `#if` 사용.
- **영향**: 릴리스 콘솔 잡음·경미한 I/O 감소.

---

## 6. 품질 및 성능

- **품질 프리셋**: Standalone → `PC` 프리셋 사용 (m_CurrentQuality: 1).
- **목표**: .cursorrules에 120 FPS 유지 명시.
- **vSyncCount**: 0 (QualitySettings) → 수동 프레임 제한 또는 VSync 설정 확인 권장.

---

## 7. 빌드 설정 요약

### Scenes in Build (현재)
1. **IntroScene** (0) — 부트/인트로  
2. **LobbyScene** — 메인 허브  
3. **ShopScene** / **InventoryScene** / **AltarOfVerityScene**  
4. **GSIScene** — G.S.I 시설(시험 등)

### 플랫폼
- **Active Build Target**: PC, Mac & Linux Standalone
- **Scripting Backend**: Standalone 기본(Mono 등) — 프로젝트 `ProjectSettings`에서 확인
- **Architecture**: x86_64 (64비트) 권장
- **Steam**: `STEAMWORKS_NET` + `STEAMWORKS_ENABLED` (Editor/Standalone)

---

## 8. Steam 출시 전 최종 확인

- [ ] Build Settings에 **IntroScene이 첫 씬(0)** 인지 확인
- [ ] PC 빌드 성공 및 실행 테스트
- [ ] 인트로 → 로비 → G.S.I/상점 등 **핵심 루프** 진입·복귀
- [ ] G.S.I 시험(연습/통합 등) **스모크 테스트**
- [ ] `STEAMWORKS_ENABLED` 켜진 상태에서 **Steam 클라이언트 + steam_appid.txt**로 런치 점검(선택: CI에선 심볼 끄기)
- [ ] 창 크기/Alt+Tab UI 확인
- [ ] 로컬 저장(PlayerPrefs 등) 이슈 없음
- [ ] Partner: App ID, depots, **출시물에 잘못된 steam_appid.txt 포함 여부** 점검; 업적·Cloud는 쓸 경우만 설정 완료

---

## 9. 보안·비동기 대전 (싱글 + 매칭 결과 비교)

- 상세: `Assets/Docs/SECURITY_ASYNC_STEAM.md`
- Steam Cloud(Remote Storage) 사용 시 Partner에서 **클라우드 활성화·쿼터** 설정.

## 10. 참고 링크

- [Steamworks 문서](https://partner.steamgames.com/doc/home)
- [Unity Steam Build Guide](https://docs.unity3d.com/Manual/SteamBuild.html)
