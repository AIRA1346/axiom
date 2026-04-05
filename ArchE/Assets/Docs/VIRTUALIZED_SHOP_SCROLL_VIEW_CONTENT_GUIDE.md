# VirtualizedShopScrollView - Content 역할 및 설정 가이드

## 1. Content의 역할

`Content`는 **Scroll View 내부에서 실제로 스크롤되는 영역**입니다.

| 역할 | 설명 |
|------|------|
| **아이템 컨테이너** | `ShopItemSlotPrefab` 인스턴스(셀)들의 부모가 됩니다. |
| **스크롤 영역 정의** | `Content`의 높이가 Viewport보다 크면 ScrollRect가 스크롤바를 활성화하고 스크롤 기능을 제공합니다. |
| **동적 크기** | `VirtualizedShopScrollView`가 아이템 개수에 따라 Content 높이를 동적으로 설정합니다. |

---

## 2. Content를 만드는 방법

### 계층 구조 (표준 Unity Scroll View)

```
Scroll View (ScrollRect + VirtualizedShopScrollView)
├── Viewport (Mask)
│   └── Content  ← 여기가 셀들의 부모
├── Scrollbar Vertical (선택)
└── Scrollbar Horizontal (보통 비활성화, vertical 전용인 경우)
```

### Content GameObject 생성 단계

1. **Scroll View** 생성: `UI > Scroll View` (또는 기존 Scroll View 사용)
2. **Viewport** 안에 **Content**가 있어야 함 (Unity 기본 템플릿은 자동 생성)
3. Content가 없다면: Viewport 우클릭 → `Create Empty` → 이름을 `Content`로 변경

### Content RectTransform 권장 설정

| 항목 | 값 | 설명 |
|------|-----|------|
| Anchor | Min: (0, 1), Max: (1, 1) | 상단 가로 전체 스트레치 |
| Pivot | (0.5, 1) | 상단 중앙 (스크립트에서 설정 시 덮어씀) |
| Size Delta | 초기 (0, 300) 등 | **스크립트가 런타임에 덮어씀** |

**중요**: `VirtualizedShopScrollView.SetupScrollRect()`가 `Awake`에서 Content의 anchor, pivot, sizeDelta를 **직접 설정**합니다. 따라서 Inspector 초기값은 크게 중요하지 않습니다.

---

## 3. Layout Group은 필요 없음

일반 Scroll View처럼 **Vertical Layout Group을 붙이지 마세요.**

- `VirtualizedShopScrollView`는 **가상화** 방식을 사용합니다.
- 화면에 보이는 셀만 생성해 **수동으로 위치를 계산**하여 배치합니다.
- `SetupScrollRect()`에서 `VerticalLayoutGroup`이 있으면 **제거(Destroy)** 합니다.

---

## 4. VirtualizedShopScrollView 연결

1. **Scroll View** GameObject에 `VirtualizedShopScrollView` 컴포넌트 추가
2. Inspector에서 다음 참조 연결:
   - `Content`: Viewport의 자식 Content RectTransform
   - `Viewport`: ScrollRect의 Viewport
   - `Cell Prefab`: `ShopItemSlotPrefab`

`_content`가 비어 있으면 스크립트가 `ScrollRect.content`를 자동으로 사용합니다.

---

## 5. 구매 버튼이 안 눌릴 때 점검 사항

| 항목 | 확인 방법 |
|------|----------|
| EventSystem | 씬에 `EventSystem` GameObject 존재, `Standalone Input Module` 또는 `Input System UI Input Module` |
| Canvas | ShopPanel이 Canvas 자식인지, Canvas의 `Render Mode` / `Sort Order` |
| Raycast | ShopItemSlotPrefab 루트 Image의 `Raycast Target` 비활성화(버튼만 raycast) |
| Content 참조 | VirtualizedShopScrollView의 `_content`가 올바른 Content를 가리키는지 |
| EconomyManager / InventoryManager | 씬에 존재하고 초기화되었는지 |

---

## 6. 스크롤 위치 계산 버그 (수정됨)

스크롤 아래로 내리면 `content.anchoredPosition.y`가 **음수**가 됩니다.  
이전에는 `contentY = Max(0, anchoredPosition.y)`로 계산해 스크롤 위치를 반영하지 못했습니다.  
수정: `contentY = Max(0, -anchoredPosition.y)` 로 변경하여 올바른 스크롤 오프셋을 사용합니다.
