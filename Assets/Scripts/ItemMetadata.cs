using System;
using UnityEngine;

/// <summary>
/// 아이템의 가벼운 메타데이터. ID, 이름, 카테고리, 티어, 가격 정보를 포함합니다.
/// 10만 개를 한꺼번에 로드하지 않고 메타데이터만 먼저 로드합니다.
/// </summary>
[Serializable]
public struct ItemMetadata
{
    public string ItemId;
    public string ItemName;
    public ItemMainCategory MainCategory;
    public ItemMiddleCategory MiddleCategory;
    public ItemSubCategory SubCategory;
    public ItemTier Tier;
    public string ResourcePath;

    /// <summary>상점 구매 가격 (0이면 비구매 가능)</summary>
    public int PurchasePrice;

    /// <summary>판매 가격 (0이면 비판매 가능)</summary>
    public int SalePrice;

    /// <summary>
    /// Resources.LoadAsync에 사용할 경로 (Resources 폴더 기준).
    /// </summary>
    public string GetResourcePath()
    {
        if (!string.IsNullOrEmpty(ResourcePath))
        {
            return ResourcePath;
        }

        return $"items/{MainCategory}/{MiddleCategory}/{SubCategory}/{Tier}/{ItemId}";
    }
}
