using UnityEngine;

public enum ItemType { Weapon, Armor, Consumable, Relic, Trinket }
public enum Rarity { Common, Rare, Epic, Mythic }

[CreateAssetMenu(menuName = "DBT/Item")]
public class ItemData : ScriptableObject
{
    public string id = "item_sword";
    public string displayName = "Rusty Sword";
    public ItemType itemType = ItemType.Weapon;
    public Rarity rarity = Rarity.Common;
    public Sprite icon = null;

    // stat bonuses stored as simple ints for now
    public int bonusHP = 0;
    public int bonusSTR = 0;
    public int bonusDEF = 0;
    public int bonusINT = 0;
    public int bonusMANA = 0;
    public int bonusAGI = 0;

    // additional mod (percentage)
    public float percentDamageBonus = 0f;

    // if weapon, base damage (optional)
    public int baseWeaponDamage = 0;

    public int sellPrice = 1;  // fill via formula in editor or runtime

    [TextArea(2, 3)]
    public string description;
}
