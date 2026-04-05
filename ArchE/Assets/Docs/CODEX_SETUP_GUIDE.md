# ARCHÉ 도서관(Codex) · ADDS 설정 가이드

ADDS(ARCHÉ Decimal Deepening System) 10단계 심화 위계와 바이너리 색인 `CodexIndex.bin`을 사용합니다.

## 1. 빠른 시작

1. **Tools > Codex > 1. Create Codex Cell Prefab**
2. **Tools > Codex > 2. Create Codex Panel Prefab**
3. `Assets/CodexCSVs/`에 CSV 배치 후 **Tools > Codex > 3. Build All Codex Databases**
4. **Tools > Codex > 4. Add CodexManager to Scene** (필요 시)
5. 씬에서 `CodexPanelPrefab` 배치, `UIManager`의 Codex 패널·`MainMenuController` 도서관 버튼 연결

## 2. CSV 스펙

| 열 | 설명 |
|----|------|
| Id | `KNO-[Lv1]-[Lv2]-[6자리]` (예: `KNO-BIO-NAT-000001`) |
| Title | Lv.10 공식 제목 (본문 .txt에는 넣지 않음) |
| Lv1 ~ Lv9 | ADDS 위계 문자열 |
| Summary | 목차용 요약 |
| Content | 순수 본문만 (제목/헤더 라인 불필요) |

빌더는 **Id의 위계1·위계2가 Lv1·Lv2 열과 일치하는지** 교차 검증합니다.

## 3. 산출물

- `StreamingAssets/CodexIndex.bin` — 헤더 `CDXI` + Version(2, ADDS) + Count + 레코드(Id, Title, Summary, Lv1~Lv9, SortOrder, HasBody)
- `Resources/CodexContents/{Id}.txt` — Content만 저장 (Clean Build 시 기존 .txt 전부 삭제 후 재생성)

## 4. 런타임

- `CodexManager`는 **색인만** `CodexIndex.bin`에서 로드합니다. (구식 Resources-only 색인 폴백 없음)
- 본문은 항목 선택 시 `LoadContentAsync`로만 로드합니다.
- 좌측 목차는 **플랫 리스트 + 접기/펼치기 + Depth 들여쓰기**이며 `VirtualizedCodexScrollView`로 가상화됩니다.

## 5. 구글 시트

**Tools > Codex Importer Settings** (또는 **Tools/Codex Importer Settings**)에서 URL 목록을 지정한 뒤 Import합니다. 시트에도 위와 동일한 열이 있어야 합니다.
