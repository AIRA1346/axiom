# 오픈월드 개발 우선순위 (실행 로그)

한 번에 한 항목씩 진행하고, 완료 시 체크합니다.

## P0 — 기반

- [x] **1. 플레이어 도보/달리기 m/s 스펙 반영** — `OpenWorldPlayerMotor` 기본값을 `md/OpenWorldSpec.md` (걷기 1.4, 달리기 6.5)에 맞춤. (느린 걷기 Ctrl은 스펙 외 0.95 m/s 초안)
- [ ] **2. 말 탑승·비행** — 입력·상태 전환 + 스펙 속도 (말 걷기 1.8 / 달리기 10.0, 비행 12 등)
- [x] **3. 월드 섹터 좌표** — `WorldSectorGrid`(512m) + `OpenWorldSectorDebug`(HUD·씬 뷰 타일 윤곽). 플레이어 스폰 시 컴포넌트 자동 부착.

## P1 — 스트리밍·콘텐츠

- [x] **4. 섹터 스트리밍 스텁** — `OpenWorldSectorStreamingStub`: 섹터 변경 시 5×5(기본 halfExtent=2) `SectorStub_x_z` 생성/삭제 + 콘솔 로그. `WorldSectorStreamingRoot` 아래에 배치.
- [x] **5. Addressables** — `OpenWorldSectorAddressablesSample` + 주소 `World_Sector_0_0` 샘플 프리팹. 메뉴 `Register Sample Sector Addressable` 로 등록. (0,0) 섹터 스텁에만 인스턴스.

## P2 — 연출·내비

- [ ] **6. URP** — 포그·동화 톤 초기값 (`OpenWorldSpec` 거리대)
- [ ] **7. 내비·비행** — 지상 NavMesh 범위, 비행 볼륨/고도 제한 초안

---

세부 수치·톤은 `OpenWorldSpec.md` 를 기준으로 하고, 플레이 테스트 후 조정합니다.
