using UnityEngine;
using Presets; // Cần dùng EnemyGimmick

[System.Serializable]
public class GimmickDescriptionEntry
{
    public EnemyGimmick gimmick; // Enum (ví dụ: Bony)
    public string gimmickName;   // Tên (ví dụ: "Rắn Chắc")
    [TextArea(2, 4)]
    public string description;   // Mô tả (ví dụ: "Giảm 25% sát thương...")
    public Sprite icon;         // (Tùy chọn, để sau này dùng)
}