// File: PassiveSkillData.cs
using UnityEngine;
using System.Collections.Generic; // Rất quan trọng, cần để dùng List<>

[CreateAssetMenu(menuName = "DBT/Passive Skill")]
public class PassiveSkillData : ScriptableObject
{
    public string id = "passive_id";
    public string displayName = "Passive Name";
    public Sprite icon = null;

    [TextArea(2, 3)]
    public string description;

    // Một skill bị động bây giờ sẽ là một danh sách các hiệu ứng.
    public List<StatModifierSO> modifiers;
}