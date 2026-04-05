using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// ScrollRect 내 구매 버튼 클릭을 안정적으로 처리합니다.
/// Button.onClick이 ScrollRect 드래그와 충돌할 수 있어, IPointerClickHandler로 보강합니다.
/// </summary>
public sealed class ShopSlotPurchaseTrigger : MonoBehaviour, IPointerClickHandler
{
    public string ItemId { get; set; }
    public int Price { get; set; }
    public event Action<string, int> OnPurchaseClicked;

    public void SetData(string itemId, int price)
    {
        ItemId = itemId;
        Price = price;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (string.IsNullOrEmpty(ItemId) || Price <= 0) return;
        OnPurchaseClicked?.Invoke(ItemId, Price);
    }
}
