using UnityEngine;

/// <summary>
/// 상점 설정. PurchasePrice&gt;0인 모든 아이템이 상점에 표시되며, 응시권 등 고정 상품 ID만 설정합니다.
/// </summary>
[CreateAssetMenu(fileName = "ShopConfig", menuName = "ARCHÉ/Shop Config")]
public sealed class ShopConfig : ScriptableObject
{
    [Header("고정 상품")]
    [Tooltip("응시권 아이템 ID. 가격은 해당 아이템의 PurchasePrice에서 읽음.")]
    public string TicketItemId = EconomyManager.TicketItemId;
}
