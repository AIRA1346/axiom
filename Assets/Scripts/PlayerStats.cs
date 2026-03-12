using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    private readonly Dictionary<StatType, float> _baseStats = new Dictionary<StatType, float>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            _baseStats[type] = 0f;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public float GetTotalStat(StatType type)
    {
        float totalValue = _baseStats.TryGetValue(type, out float baseValue) ? baseValue : 0f;

        if (EquipmentManager.Instance == null)
        {
            return totalValue;
        }

        foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
        {
            ItemInstance equippedInstance = EquipmentManager.Instance.GetEquippedItem(slot);
            ItemData equippedItem = equippedInstance != null && ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItem(equippedInstance.ItemId)
                : null;

            if (equippedItem == null || equippedItem.StatModifiers == null)
            {
                continue;
            }

            foreach (StatModifier modifier in equippedItem.StatModifiers)
            {
                if (modifier != null && modifier.Stat == type)
                {
                    totalValue += modifier.Value;
                }
            }
        }

        return totalValue;
    }
}
