# 범우주 표준 도서관(Universal Codex) 설정 가이드

## 1. 빠른 시작

1. **Tools > Codex > 1. Create Codex Cell Prefab** 실행
2. **Tools > Codex > 2. Create Codex Panel Prefab** 실행
3. **Tools > Codex > 3. Build Codex Database** 실행 → CSV 선택 (또는 수동 .txt 추가)
4. **Tools > Codex > 4. Add CodexManager to Scene** 실행
5. 메인 메뉴 씬에서:
   - `CodexPanelPrefab`을 Canvas 하위로 배치
   - `UIManager`의 `_codexPanel`에 연결
   - `MainMenuController`에 `_codexButton` 연결 (도서관 진입용 버튼)

## 2. 본문 추가 방법

### 방법 A: 멀티 시트 CSV 빌드 (권장)

**Tools > Codex > 3. Build All Codex Databases** 실행.

- `Assets/CodexCSVs/` 폴더 내 **모든 .csv 파일**을 자동 순회
- 각 **파일명**(예: `Bio.csv`)이 해당 시트의 **LargeCat(대분류)**로 자동 할당
- Clean Build: 기존 `CodexContents/` 내 .txt 전부 삭제 후 재생성
- ID 중복 시 에러 로그 및 빌드 중단

**CSV 필수 컬럼**: `Id`, `Title`, `LargeCat`, `MidCat`, `SmallCat`, `Summary`, `Content`
(LargeCat 비어 있으면 파일명으로 자동 보정)

**결과물**:
- `StreamingAssets/CodexIndex.bin` (바이너리 색인)
- `Resources/CodexContents/{Id}.txt` (본문 개별 파일)

### 방법 B: 수동 .txt 추가

`Assets/Resources/CodexContents/` 폴더에 텍스트 파일을 추가합니다.

**파일명 형식**: `KNO-대분류-중분류-소분류-일련번호.txt`

예시:
- `KNO-Norm-Basic-Def-000001.txt`
- `KNO-Tech-System-Intro-000003.txt`

**내용**:
- 첫 줄의 `# 제목` 또는 `## 제목`이 목차에 표시되는 제목으로 사용됩니다.
- TextMeshPro Rich Text 태그 지원: `<b>굵게</b>`, `<color=#333>색상</color>`, `<i>기울임</i>`

## 3. 지식 ID 체계

| 형식 | 예시 |
|------|------|
| KNO-대분류-중분류-소분류-000000 | KNO-Norm-Basic-Def-000001 |

- **대분류**: Norm(규범), Tech(기술), History(역사) 등
- **중분류**: Basic, System, Event 등
- **소분류**: Def, Scope, Intro 등
- **일련번호**: 6자리

## 4. UI 구조

- **Split View**: 좌측 35% 목차(Navigation), 우측 65% 본문(Viewer)
- **무채색 디자인**: Black, White, Gray 공식 문서 톤
- **가상화**: 10만 개 목차 항목 대응 Virtualized Scroll View
