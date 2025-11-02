using Presets;
using System.Collections.Generic;

[System.Serializable]
public class PlayerData
{
    public string selectedRaceId; // Sẽ lưu ID của Race, ví dụ "race_human"
    public string selectedClassId; // Sẽ lưu ID của Class, ví dụ "class_warrior"

    // Sau này sẽ thêm:
    public StatBlock currentStats;
    public List<string> itemIds;
    public List<string> inventoryItemIds; // ✅ THÊM DÒNG NÀY: Đồ trong kho

    public int gold;
    public int steadfastDurability;

    public int currentHP;
    public int currentMana;
    public float totalTimePlayed = 0f;
    public PlayerData()
    {
        itemIds = new List<string>();
        inventoryItemIds = new List<string>();
    }
    /// <summary>
    /// Tạo một bản sao sâu (deep copy) của PlayerData.
    /// </summary>
    public PlayerData Clone()
    {
        PlayerData copy = new PlayerData();
        copy.selectedRaceId = this.selectedRaceId;
        copy.selectedClassId = this.selectedClassId;

        copy.currentStats = new StatBlock
        {
            HP = this.currentStats.HP,
            STR = this.currentStats.STR,
            DEF = this.currentStats.DEF,
            INT = this.currentStats.INT,
            MANA = this.currentStats.MANA,
            AGI = this.currentStats.AGI
        };

        // Lists là class, cần tạo mới
        copy.itemIds = new List<string>(this.itemIds);
        copy.inventoryItemIds = new List<string>(this.inventoryItemIds);

        // Các kiểu giá trị (int, float) được copy trực tiếp
        copy.gold = this.gold;
        copy.steadfastDurability = this.steadfastDurability;
        copy.currentHP = this.currentHP;
        copy.currentMana = this.currentMana;
        copy.totalTimePlayed = this.totalTimePlayed;

        return copy;
    }
}