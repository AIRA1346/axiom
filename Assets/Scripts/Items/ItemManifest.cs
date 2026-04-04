using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ItemMetadata.bin 없이 로컬 개발할 때 사용하는 단일 매니페스트.
/// 런타임에는 Resources.Load("ItemManifest") 한 번만 호출하며 Resources.LoadAll을 쓰지 않습니다.
/// 본 실행·배포는 반드시 ItemMetadata.bin + 개별 LoadAsync 경로를 사용하세요.
/// </summary>
[CreateAssetMenu(fileName = "ItemManifest", menuName = "gsi/Item Manifest")]
public sealed class ItemManifest : ScriptableObject
{
    [SerializeField] private List<ItemData> _items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => _items;
}
