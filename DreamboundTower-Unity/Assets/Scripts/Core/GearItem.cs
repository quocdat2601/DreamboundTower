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

    [Header("Description")]
    [Tooltip("Mô tả hiệu ứng đặc biệt hoặc passive của vật phẩm.")]
    [TextArea(3, 5)] // Giúp ô nhập liệu trong Inspector lớn hơn, dễ gõ hơn
    public string description;

    [Header("Effects & Modifiers")]
    [Tooltip("Danh sách tất cả các hiệu ứng mà vật phẩm này mang lại.")]
    public List<StatModifierSO> modifiers;

    // SAU NÀY BẠN CÓ THỂ THÊM CÁC THUỘC TÍNH PHỨC TẠP HƠN
    // public float hpBonusPercent;
    // public List<Affix> affixes;
}
