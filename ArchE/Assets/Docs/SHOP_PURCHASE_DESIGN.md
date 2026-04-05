# 상점 아이템 구매 시스템 설계안

## 1. 현재 프로젝트 상태 요약

| 구분 | 내용 |
|------|------|
| **EconomyManager** | `Tokens`, `SpendTokens()`, `AddTokens()` |
| **InventoryManager** | `AddItem()`, `RemoveItem()`, `GetItemCount()` |
| **ItemData** | `PurchasePrice`, `SalePrice` 필드 이미 존재 |
| **ShopController** | 응시권 구매(100토큰), 가챠(500토큰 랜덤 아이템) |
| **InventoryController** | `SalePrice` 기준 판매 UI (참고용) |

---

## 2. 설계 옵션 비교

### 옵션 A: PurchasePrice > 0 자동 상점
- **방식**: `ItemData.PurchasePrice > 0`인 아이템만 상점에 표시
- **장점**: 별도 설정 없음, ItemData만 수정
- **단점**: UseMetadataOnly 모드에서 PurchasePrice는 `ItemData` 로드 시에만 알 수 있음 (메타데이터에 없음)

### 옵션 B: ShopConfig ScriptableObject (추천)
- **방식**: 상점 진열 품목을 `[ItemId, 가격(옵션)]` 리스트로 관리
- **장점**: 상점 구성을 완전 제어, 고정/이벤트 상품 관리 용이
- **단점**: ShopConfig 에셋 생성·관리 필요

### 옵션 C: ItemMetadata에 PurchasePrice 추가
- **방식**: 메타데이터에 가격 포함 → 10만 개 대응 상점 필터 가능
- **장점**: 대량 아이템 상점 대응
- **단점**: ItemMetadata/Builder/BinaryLoader 수정 필요

---

## 3. 추천 설계 (옵션 B 기반)

### 3.1 데이터 구조

```
ShopConfig.asset (ScriptableObject)
├── List<ShopEntry>
│   ├── string ItemId
│   ├── int Price (0이면 ItemData.PurchasePrice 사용)
│   └── int StockLimit (0 = 무제한, >0 = 제한 수)
```

- **ShopEntry**: 상점에 진열할 개별 상품
- **Price**: 0이면 `ItemData.PurchasePrice` 사용, 그 외는 오버라이드
- **StockLimit**: 0 = 무제한, N = N개만 구매 가능 (선택)

### 3.2 구매 플로우

```
[상점 UI] 구매 클릭
    → EconomyManager.SpendTokens(price)
    → 성공 시 InventoryManager.AddItem(itemId, 1)
    → UI 갱신 (토큰, 결과 텍스트)
```

### 3.3 UI 구조 (CraftingUIController 패턴 참고)

```
ShopController 확장
├── _itemContainer (Transform)      ← 상품 슬롯 부모
├── _shopItemPrefab (GameObject)   ← 슬롯 프리팹
├── _shopTokenText (이미 있음)
├── _resultText (이미 _gachaResultText)
└── ShopConfig 참조
```

- **슬롯 내용**: 아이콘, 이름, 티어 접두사, 가격, [구매] 버튼
- **OnEnable/Refresh**: ShopConfig 순회 → 슬롯 생성
- **구매 시**: 토큰 차감 → 인벤토리 추가 → 결과 메시지

### 3.4 파일 변경 계획

| 파일 | 변경 내용 |
|------|----------|
| `ShopConfig.cs` (신규) | ScriptableObject, List\<ShopEntry\> |
| `ShopController.cs` | 상품 목록 UI, 구매 로직 추가 |
| `ShopItemSlotPrefab` | 상품 슬롯 프리팹 (또는 기존 슬롯 활용) |

---

## 4. ItemMetadata 확장 시 (옵션 C)

메타데이터에 PurchasePrice를 넣을 경우:
- `ItemMetadata`: `int PurchasePrice` 추가
- `ItemMetadataBuilder`: YAML에서 `PurchasePrice` 파싱
- `ItemMetadataBinaryLoader`: 바이너리 읽기/쓰기에 `PurchasePrice` 포함
- 상점: `PurchasePrice > 0` 필터로 전체 품목 표시 가능 (10만 개 대응)

---

## 5. 구현 순서 제안

1. **ShopConfig** ScriptableObject 생성
2. **ShopController**에 상품 목록 표시·구매 로직 추가
3. Shop 패널에 `itemContainer` + `shopItemPrefab` 배치
4. ShopConfig 에셋 생성 후 구매 가능 아이템 등록
5. (선택) StockLimit, 구매 제한 등 확장

---

## 6. 참고: 기존 패턴

- **CraftingUIController**: `_recipeContainer` + `_recipeSlotPrefab` + Instantiate + Refresh
- **InventoryController**: `_itemContainer` + `_itemSlotPrefab` + 판매 시 `SpendTokens` 대신 `AddTokens` (역방향)
- **ShopController**: 응시권/가챠는 이미 `EconomyManager.SpendTokens` + `InventoryManager.AddItem` 사용

이 패턴을 그대로 따라 상품 구매 UI를 추가하면 기존 코드와 일관성을 유지할 수 있습니다.
