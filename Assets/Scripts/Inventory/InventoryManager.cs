using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores persistent item counts and equipped cosmetic selections for the G.S.I project.
/// This manager only handles inventory quantities and equipped aim-target data.
/// </summary>
public sealed class InventoryManager : MonoBehaviour
{
    private const string InventoryDataKey = "GSIInventoryData";
    private const string EquippedAimTargetKey = "GSIEquippedAimTarget";
    private const string DefaultAimTargetId = "Aim_Default";

    public static InventoryManager Instance { get; private set; }

    public string EquippedAimTarget { get; private set; } = DefaultAimTargetId;

    private readonly List<ItemInstance> _inventory = new List<ItemInstance>();

    [Serializable]
    private sealed class InventorySaveData
    {
        public List<ItemInstance> Entries = new List<ItemInstance>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadInventoryData();
    }

    /// <summary>
    /// Adds one or more items to the persistent inventory.
    /// </summary>
    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return;
        }

        var db = ItemDatabase.Instance;
        ItemData data = db != null ? db.GetItem(itemId) : null;
        var meta = db?.GetMetadata(itemId);
        bool isEquipment = data?.MainCategory == ItemMainCategory.Equipment
            || (meta.HasValue && meta.Value.MainCategory == ItemMainCategory.Equipment);

        if (EncyclopediaManager.Instance != null)
        {
            EncyclopediaManager.Instance.UnlockItem(itemId);
        }

        if (isEquipment)
        {
            for (int i = 0; i < amount; i++)
            {
                _inventory.Add(new ItemInstance
                {
                    ItemId = itemId,
                    Count = 1,
                    EnhanceLevel = 0
                });
            }
        }
        else
        {
            ItemInstance existingInstance = _inventory.Find(instance => instance != null && instance.ItemId == itemId);

            if (existingInstance != null)
            {
                existingInstance.Count += amount;
            }
            else
            {
                _inventory.Add(new ItemInstance
                {
                    ItemId = itemId,
                    Count = amount,
                    EnhanceLevel = 0
                });
            }
        }

        SaveInventoryData();
    }

    /// <summary>
    /// Removes one or more items when enough copies exist in the inventory.
    /// </summary>
    public bool RemoveItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount <= 0)
        {
            return false;
        }

        if (GetItemCount(itemId) < amount)
        {
            return false;
        }

        int remainingAmount = amount;

        for (int i = _inventory.Count - 1; i >= 0 && remainingAmount > 0; i--)
        {
            ItemInstance instance = _inventory[i];

            if (instance == null || instance.ItemId != itemId)
            {
                continue;
            }

            int deductedAmount = Mathf.Min(instance.Count, remainingAmount);
            instance.Count -= deductedAmount;
            remainingAmount -= deductedAmount;

            if (instance.Count <= 0)
            {
                _inventory.RemoveAt(i);
            }
        }

        SaveInventoryData();
        return true;
    }

    /// <summary>
    /// Returns the stored quantity of a specific item.
    /// </summary>
    public int GetItemCount(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return 0;
        }

        int totalCount = 0;

        foreach (ItemInstance instance in _inventory)
        {
            if (instance != null && instance.ItemId == itemId)
            {
                totalCount += instance.Count;
            }
        }

        return totalCount;
    }

    /// <summary>
    /// Returns a copy of the current item-count data for read-only UI usage.
    /// </summary>
    public Dictionary<string, int> GetItemCountsSnapshot()
    {
        Dictionary<string, int> itemCounts = new Dictionary<string, int>();

        foreach (ItemInstance instance in _inventory)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.ItemId) || instance.Count <= 0)
            {
                continue;
            }

            if (itemCounts.ContainsKey(instance.ItemId))
            {
                itemCounts[instance.ItemId] += instance.Count;
            }
            else
            {
                itemCounts[instance.ItemId] = instance.Count;
            }
        }

        return itemCounts;
    }

    public IEnumerable<ItemInstance> GetAllInstances()
    {
        return _inventory;
    }

    public ItemInstance GetItemInstance(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
        {
            return null;
        }

        return _inventory.Find(instance => instance != null && instance.InstanceId == instanceId);
    }

    public bool RemoveItemByInstance(string instanceId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(instanceId) || amount <= 0)
        {
            return false;
        }

        for (int i = 0; i < _inventory.Count; i++)
        {
            ItemInstance instance = _inventory[i];

            if (instance == null || instance.InstanceId != instanceId)
            {
                continue;
            }

            if (instance.Count < amount)
            {
                return false;
            }

            instance.Count -= amount;

            if (instance.Count <= 0)
            {
                _inventory.RemoveAt(i);
            }

            SaveInventoryData();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Equips an owned aim-target cosmetic, or the default target, for future use.
    /// </summary>
    public void EquipAimTarget(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return;
        }

        if (itemId != DefaultAimTargetId && GetItemCount(itemId) <= 0)
        {
            return;
        }

        EquippedAimTarget = itemId;
        SaveInventoryData();
    }

    /// <summary>
    /// Loads persistent item counts and the equipped aim target selection.
    /// </summary>
    private void LoadInventoryData()
    {
        _inventory.Clear();

        string json = PlayerPrefs.GetString(InventoryDataKey, string.Empty);

        if (!string.IsNullOrEmpty(json))
        {
            InventorySaveData saveData = JsonUtility.FromJson<InventorySaveData>(json);

            if (saveData != null && saveData.Entries != null)
            {
                foreach (ItemInstance entry in saveData.Entries)
                {
                    if (entry != null && !string.IsNullOrWhiteSpace(entry.ItemId) && entry.Count > 0)
                    {
                        if (string.IsNullOrWhiteSpace(entry.InstanceId))
                        {
                            entry.InstanceId = Guid.NewGuid().ToString();
                        }

                        _inventory.Add(entry);
                    }
                }
            }
        }

        EquippedAimTarget = PlayerPrefs.GetString(EquippedAimTargetKey, DefaultAimTargetId);
    }

    /// <summary>
    /// Saves item counts and the equipped aim target selection immediately.
    /// </summary>
    private void SaveInventoryData()
    {
        InventorySaveData saveData = new InventorySaveData();

        foreach (ItemInstance instance in _inventory)
        {
            if (instance == null || string.IsNullOrWhiteSpace(instance.ItemId) || instance.Count <= 0)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(instance.InstanceId))
            {
                instance.InstanceId = Guid.NewGuid().ToString();
            }

            saveData.Entries.Add(instance);
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(InventoryDataKey, json);
        PlayerPrefs.SetString(EquippedAimTargetKey, EquippedAimTarget);
        PlayerPrefs.Save();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
