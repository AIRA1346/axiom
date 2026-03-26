using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 도서관 목차 셀 클릭 처리. ScrollRect 내에서 안정적으로 동작합니다.
/// </summary>
public sealed class CodexEntryCell : MonoBehaviour, IPointerClickHandler
{
    public CodexMetadata Data { get; private set; }
    public event Action<CodexMetadata> OnClicked;

    public void SetData(CodexMetadata data)
    {
        Data = data;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnClicked?.Invoke(Data);
    }
}
