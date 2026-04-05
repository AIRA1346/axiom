using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "NewItem", menuName = "gsi/Item Data")]
public class ItemData : ScriptableObject
{
    [FormerlySerializedAs("item_id")]
    public string ItemId;

    [FormerlySerializedAs("item_name")]
    public string ItemName;

    public Sprite ItemIcon;

    [TextArea]
    [FormerlySerializedAs("description")]
    public string Description;

    [FormerlySerializedAs("tier")]
    public ItemTier Tier;

    [FormerlySerializedAs("MainType")]
    [FormerlySerializedAs("type")]
    [Category]
    public ItemMainCategory MainCategory;

    [FormerlySerializedAs("SubType")]
    [Category]
    public ItemMiddleCategory MiddleCategory;

    [Category]
    public ItemSubCategory SubCategory;

    [FormerlySerializedAs("default_slot")]
    public EquipSlot DefaultSlot;

    [FormerlySerializedAs("grip_type")]
    public WeaponGrip GripType;

    public int PurchasePrice;
    public int SalePrice;

    [FormerlySerializedAs("stat_modifiers")]
    public List<StatModifier> StatModifiers = new List<StatModifier>();
}
