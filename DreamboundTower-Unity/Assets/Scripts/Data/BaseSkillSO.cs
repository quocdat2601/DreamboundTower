// File: BaseSkillSO.cs
using UnityEngine;

// "abstract" có nghĩa là bạn không thể tạo asset trực tiếp từ lớp cha này
public abstract class BaseSkillSO : ScriptableObject
{
    public string id = "skill_id";
    public string displayName = "Skill Name";
    public Sprite icon = null;

    [TextArea(2, 3)]
    public string descriptionTemplate;
}