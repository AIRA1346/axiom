using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class EquipmentManager : MonoBehaviour
{
    [System.Serializable]
    public class EquipSaveData
    {
        [FormerlySerializedAs("entries")]
        public List<EquipSaveEntry> Entries = new List<EquipSaveEntry>();
    }

    [System.Serializable]
    public class EquipSaveEntry
    {
        [FormerlySerializedAs("slot")]
        public EquipSlot Slot;

        [FormerlySerializedAs("item_id")]
        public string InstanceId;
    }

    private const string EquipSaveKey = "gsi_equipped_items";

    public static EquipmentManager Instance { get; private set; }

    public event Action OnEquipmentChanged;

    private readonly Dictionary<EquipSlot, ItemInstance> _equippedItems = new Dictionary<EquipSlot, ItemInstance>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        LoadEquipment();
    }

    public void EquipItem(ItemInstance newInstance, EquipSlot targetSlot)
    {
        if (newInstance == null || string.IsNullOrWhiteSpace(newInstance.ItemId))
        {
            return;
        }

        if (ItemDatabase.Instance == null)
        {
            return;
        }

        ItemData newItemData = ItemDatabase.Instance.GetItem(newInstance.ItemId);

        if (newItemData == null || newItemData.MainCategory != ItemMainCategory.Equipment)
        {
            return;
        }

        if (!CanEquipToSlot(newItemData, targetSlot))
        {
            return;
        }

        foreach (KeyValuePair<EquipSlot, ItemInstance> pair in _equippedItems)
        {
            if (pair.Key != targetSlot
                && pair.Value != null
                && pair.Value.InstanceId == newInstance.InstanceId)
            {
                UnequipItem(pair.Key);
                break;
            }
        }

        if (newItemData.GripType == WeaponGrip.TwoHanded && targetSlot == EquipSlot.Weapon)
        {
            UnequipItem(EquipSlot.SubWeapon);
        }

        if (targetSlot == EquipSlot.SubWeapon)
        {
            ItemInstance mainHandInstance = GetEquippedItem(EquipSlot.Weapon);
            ItemData mainHandItemData = mainHandInstance != null
                ? ItemDatabase.Instance.GetItem(mainHandInstance.ItemId)
                : null;

            if (mainHandItemData != null && mainHandItemData.GripType == WeaponGrip.TwoHanded)
            {
                UnequipItem(EquipSlot.Weapon);
            }
        }

        _equippedItems[targetSlot] = newInstance;
        OnEquipmentChanged?.Invoke();
        SaveEquipment();
    }

    public void UnequipItem(EquipSlot targetSlot)
    {
        if (_equippedItems.ContainsKey(targetSlot))
        {
            _equippedItems.Remove(targetSlot);
            OnEquipmentChanged?.Invoke();
            SaveEquipment();
        }
    }

    public ItemInstance GetEquippedItem(EquipSlot slot)
    {
        return _equippedItems.TryGetValue(slot, out ItemInstance equippedItem) ? equippedItem : null;
    }

    public bool IsEquipped(string instanceId)
    {
        foreach (ItemInstance instance in _equippedItems.Values)
        {
            if (instance != null && instance.InstanceId == instanceId)
            {
                return true;
            }
        }

        return false;
    }

    public bool CanEquipToSlot(ItemData item, EquipSlot slot)
    {
        if (item == null || item.MainCategory != ItemMainCategory.Equipment)
        {
            return false;
        }

        if (item.DefaultSlot == slot)
        {
            return true;
        }

        if (item.DefaultSlot == EquipSlot.Weapon
            && item.GripType == WeaponGrip.OneHanded
            && slot == EquipSlot.SubWeapon)
        {
            return true;
        }

        return false;
    }

    public void EquipFromInventory(string instanceId, EquipSlot targetSlot)
    {
        if (ItemDatabase.Instance == null || InventoryManager.Instance == null)
        {
            return;
        }

        ItemInstance instance = InventoryManager.Instance.GetItemInstance(instanceId);

        if (instance == null)
        {
            return;
        }

        ItemData data = ItemDatabase.Instance.GetItem(instance.ItemId);

        if (data == null)
        {
            return;
        }

        if (instance.Count <= 0)
        {
            return;
        }

        if (!CanEquipToSlot(data, targetSlot))
        {
            return;
        }

        EquipItem(instance, targetSlot);
    }

    public void DebugForceEquip(ItemInstance item, EquipSlot slot)
    {
        EquipItem(item, slot);

        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.Equipment)
        {
            OnEquipmentChanged?.Invoke();
        }
    }

    private void SaveEquipment()
    {
        EquipSaveData saveData = new EquipSaveData();

        foreach (KeyValuePair<EquipSlot, ItemInstance> pair in _equippedItems)
        {
            if (pair.Value == null || string.IsNullOrWhiteSpace(pair.Value.InstanceId))
            {
                continue;
            }

            saveData.Entries.Add(new EquipSaveEntry
            {
                Slot = pair.Key,
                InstanceId = pair.Value.InstanceId
            });
        }

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(EquipSaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadEquipment()
    {
        _equippedItems.Clear();

        if (ItemDatabase.Instance == null)
        {
            return;
        }

        string json = PlayerPrefs.GetString(EquipSaveKey, string.Empty);

        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        EquipSaveData saveData = JsonUtility.FromJson<EquipSaveData>(json);

        if (saveData == null || saveData.Entries == null)
        {
            return;
        }

        foreach (EquipSaveEntry entry in saveData.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.InstanceId) || InventoryManager.Instance == null)
            {
                continue;
            }

            ItemInstance instance = InventoryManager.Instance.GetItemInstance(entry.InstanceId);
            ItemData data = instance != null
                ? ItemDatabase.Instance.GetItem(instance.ItemId)
                : null;

            if (data == null || !CanEquipToSlot(data, entry.Slot))
            {
                continue;
            }

            _equippedItems[entry.Slot] = instance;
        }

        OnEquipmentChanged?.Invoke();
    }
}
