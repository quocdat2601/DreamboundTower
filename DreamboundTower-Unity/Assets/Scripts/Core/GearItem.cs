using System.Collections.Generic;
using UnityEngine;

public enum GearType 
{ 
    Weapon, 
    Helmet, 
    ChestArmor, 
    Pants, 
    Boots, 
    Amulet, 
    Ring 
}

[CreateAssetMenu(fileName = "NewGear", menuName = "RPG/Gear")]
public class GearItem : ScriptableObject
{
    public string itemName;
    public GearType gearType;
    public Sprite icon;
    
    [Header("Description")]
    [TextArea(3, 5)]
    public string description;

    [Header("Shop & Rarity")]
    public ItemRarity rarity;
    public int basePrice;

    [Header("Stats Bonus")]
    public int attackBonus;
    public int defenseBonus;
    public int hpBonus;
    public int intBonus;
    public int manaBonus;
    public int agiBonus;

    [Header("Weapon Scaling (Weapons only)")]
    [Tooltip("How this weapon scales damage (STR, INT, or Hybrid)")]
    public WeaponScalingType scalingType = WeaponScalingType.STR;

    [Header("Effects & Modifiers")]
    [Tooltip("Danh sách tất cả các hiệu ứng mà vật phẩm này mang lại.")]
    public List<StatModifierSO> modifiers;

    // SAU NÀY BẠN CÓ THỂ THÊM CÁC THUỘC TÍNH PHỨC TẠP HƠN
    // public float hpBonusPercent;
    // public List<Affix> affixes;
}