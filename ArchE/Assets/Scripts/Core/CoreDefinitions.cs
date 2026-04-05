using UnityEngine.Serialization;

/// <summary>
/// Defines the shared item tiers used across cosmetic and equipment systems.
/// This file contains fixed definitions and is not auto-generated.
/// </summary>
public enum ItemTier
{
    Tier1,
    Tier2,
    Tier3,
    Tier4,
    Tier5,
    Tier6,
    Tier7,
    Tier8,
    Tier9,
    Tier10
}

public enum StatType
{
    Hp,
    Mp,
    Sp,
    PhysAtk,
    MagAtk,
    Accuracy,
    CritRate,
    StatusInflict,
    PhysDef,
    MagDef,
    Evasion,
    GuardRate,
    StatusResist,
    Speed,
    Luck
}

public enum EquipSlot
{
    None,
    Consumable1,
    Weapon,
    SubWeapon,
    Shield,
    Head,
    Body,
    Legs,
    Hands,
    Feet,
    Shoulders,
    Cape,
    Ring,
    Necklace,
    Earring,
    Bracelet,
    Waist,
    Charm
}

public enum WeaponGrip
{
    None,
    OneHanded,
    TwoHanded
}

[System.Serializable]
public class StatModifier
{
    [FormerlySerializedAs("stat")]
    public StatType Stat;

    [FormerlySerializedAs("value")]
    public float Value;
}
