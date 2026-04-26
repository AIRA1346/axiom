# G.S.I — 스팀·비동기 대전 보안 정리

싱글 개발 + **실시간이 아닌** 매칭 후 동일 시험·결과 비교·승패 판정을 전제로 한 위협 모델과 대응 방향입니다.

---

## 1. Steam Cloud(Remote Storage)로 한 일

- `GsiSteamCloudSync`가 **베스트 기록·통합 시험 텍스트/JSON·연습 급수·골드/응시권** 등을 JSON 스냅샷으로 **Steam Remote Storage**에 올리고, 더 최신 스냅샷이 있으면 부팅 시 `PlayerPrefs`에 병합합니다.
- **스팀 파트너 사이트**에서 해당 App ID에 **Steam Cloud를 켜고** 할당량을 설정해야 합니다.
- **한계**: 클라이언트가 만든 파일이므로, **결심적인 PC에서의 메모리/프로세스 조작**까지 막지는 못합니다. 다만 **다른 PC 간 세이브 동기화**와 **일반적인 파일만 수정**하는 수준에는 도움이 됩니다.

---

## 2. 비동기 1:1 대전 — 조작을 “최대한” 막으려면

| 위협 | 클라이언트만으로 | 권장 |
|------|------------------|------|
| 점수·총점 JSON 위조 | 막기 어렵음 | **서버**가 양쪽 제출을 받고, **Steam 티켓(Web API)** 등으로 계정 검증 후 저장·승패 결정 |
| 매칭 중복·스푸핑 | 불가 | **백엔드**에 `AsyncMatchId` 발급, 상대만 해당 id로 제출 허용 |
| 리플레이 없이 “같은 시험” 증명 | 클라이언트 주장만 | 시드·시험 버전·규칙 해시를 **서버가 고정**하고 클라는 그대로 실행 결과만 보고 |

**`VersusAsyncScorePayload`**에 `SubmitterSteamId`, `AsyncMatchId` 필드를 넣었습니다.  
백엔드는 `SubmitterSteamId`를 **Steam OpenID / 세션 티켓**과 대조하고, `AsyncMatchId`로 대전 단위를 묶으면 됩니다.

현재 **HTTP 전송**(`VersusAsyncBackendSettings`)이 비어 있으면 `VersusAsyncNullTransport`로 동작합니다. 공정한 랭킹·승패를 위해서는 **최소한의 API 서버**를 두는 것을 권장합니다.

---

## 3. 스팀 생태만으로는 부족한 이유

- **Steam Leaderboards**도 점수는 **클라이언트가 API로 올립니다**. 공정성이 중요하면 **서버 검증** 또는 **신뢰할 수 있는 단일 소스**가 필요합니다.
- **VAC / Easy Anti-Cheat**는 주로 실시간·대규모 치트 대응용이며, **비동기 점수 조작**만을 위해 켜는 경우는 드뭅니다(정책·도입 비용).

---

## 4. 출시 직전 체크 (요약)

- [ ] Steam Cloud 켜짐 + 할당량 + 스냅샷 크기(현재 상한 약 900KB) 내인지
- [ ] 공정한 비동기 대전이 제품 요구면: **매칭·결과 API** + Steam ID 검증 설계
- [ ] `VersusAsyncBackendSettings`에 상용 `BaseUrl`·인증(배럴/키) 설정
- [ ] (선택) 점수 제출 **재시도·아웃박스**(`VersusAsyncOutbox`)가 실패 시나리오에서도 안전한지

---

## 5. 코드 참조

- `GsiSteamCloudSync`, `SteamRemoteStorageGsi` — Cloud 스냅샷
- `VersusAsyncScorePayload` — 비동기 대전 페이로드 필드
- `VersusAsyncBridge.CreateUnifiedExamPayload` — Steam ID 주입
