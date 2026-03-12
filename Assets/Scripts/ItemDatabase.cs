using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance { get; private set; }

    private readonly Dictionary<string, ItemData> _itemDict = new Dictionary<string, ItemData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadItemData();
    }

    public ItemData GetItem(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        return _itemDict.TryGetValue(itemId, out ItemData data) ? data : null;
    }

    public IEnumerable<ItemData> GetAllItems()
    {
        return _itemDict.Values;
    }

    private void LoadItemData()
    {
        _itemDict.Clear();

        ItemData[] loadedItems = Resources.LoadAll<ItemData>("items");

        foreach (ItemData item in loadedItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId))
            {
                continue;
            }

            _itemDict[item.ItemId] = item;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
