# 상점(Shop) 기능 셋업 가이드

## 1. 에셋

### ShopItemSlotPrefab
- 메뉴: **Tools > Create Shop Item Slot Prefab**
- 경로: `Assets/Prefabs/ShopItemSlotPrefab.prefab`

### ShopConfig
- 메뉴: **GSI > Create Shop Config**
- 경로: `Assets/Config/ShopConfig.asset`
- 응시권(TicketItemId) 설정. **구매 가능 상품은 ItemDatabase의 PurchasePrice>0인 모든 아이템**이 자동 표시됨.

### Category Dropdowns (도감/상점 공용)
- 대/중/소분류·티어 TMP_Dropdown을 포함한 프리팹
- 도감(Encyclopedia)과 상점(Shop) 패널에 동일하게 배치하여 사용

---

## 2. Shop 패널 UI 구조

```
ShopPanel
├── Header (기초 골드 표시)
├── TicketSlotRoot (응시권 고정 상품)
├── Category Dropdowns (대/중/소/티어 TMP_Dropdown)
├── 페이지네이션 (이전/다음/페이지 텍스트)
├── ScrollView (VirtualizedShopScrollView)
├── ResultText
└── ExitButton
```

---

## 3. ShopController Inspector 할당

| 필드 | 할당 대상 |
|------|-----------|
| Shop Config | ShopConfig 에셋 |
| Ticket Slot Root | 응시권 영역 부모 |
| Main/Middle/Sub/Tier Dropdown | TMP_Dropdown (Category Dropdowns 프리팹) |
| Virtualized Scroll View | VirtualizedShopScrollView |
| Prev/Next Page Button, Page Text | 페이지네이션 UI |
| Shop Token Text | 기초 골드 |
| Exit Button | 나가기 |

---

## 4. 동작 방식

- **PurchasePrice > 0**인 모든 아이템이 상점에 표시됨
- **대/중/소분류·티어** 필터로 도감처럼 탐색
- **페이지네이션**: 1000개 단위 (도감과 동일)
