# ItemMetadata.bin 빌드 파이프라인

## 개요

`ItemMetadata.bin`은 10만 개 이상의 아이템 메타데이터를 빠르게 로드하기 위한 바이너리 파일입니다.  
StreamingAssets에 배치되어 런타임에 `ItemMetadataBinaryLoader`가 비동기로 로드합니다.

## 자동 실행 시점

| 시점 | 트리거 | 담당 |
|------|--------|------|
| **플레이어 빌드 전** | File > Build Settings > Build | `ItemMetadataBuildPipeline` (IPreprocessBuildWithReport) |
| **아이템 임포트 후** | Tools > Item Importer > Import Items | `ItemImporter` → `ItemMetadataBuilder.Build()` |
| **수동** | Tools > Build Item Metadata Database | `ItemMetadataBuilder` |

## 출력 경로

- `Assets/StreamingAssets/ItemMetadata.bin`  
- 빌드 시 자동으로 패키지에 포함됨

## 수동 빌드

- **메뉴**: `Tools` > `Build Item Metadata Database`  
- **단축**: `Assets` 컨텍스트 메뉴 또는 `GSI` 메뉴에서 동일 항목

## CI/배치 모드

`-batchMode`로 빌드할 때 진행률 UI 없이 자동 실행됩니다.  
빌드 실패 시 `BuildFailedException`으로 빌드가 중단됩니다.

## 데이터 소스

- `Assets/Resources` 내 모든 `ItemData` 에셋(YAML) 스캔
- ItemId, ItemName, Main/Middle/SubCategory, Tier, ResourcePath, PurchasePrice, SalePrice 추출
