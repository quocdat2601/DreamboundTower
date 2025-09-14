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

    [Header("Stats Bonus")]
    public int attackBonus;
    public int defenseBonus;
    public int hpBonus;
}
