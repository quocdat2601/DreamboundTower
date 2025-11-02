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
}