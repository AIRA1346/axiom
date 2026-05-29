using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>G.S.I: 장비·아이템 없이 기본 스탯만 유지합니다.</summary>
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
        return _baseStats.TryGetValue(type, out float baseValue) ? baseValue : 0f;
    }
}
