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
    public int gold;
    public int steadfastDurability;

    public int currentHP;
    public int currentMana;
}