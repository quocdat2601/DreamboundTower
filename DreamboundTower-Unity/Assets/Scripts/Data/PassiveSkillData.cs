// File: PassiveSkillData.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "DBT/Passive Skill")]
public class PassiveSkillData : BaseSkillSO // Kế thừa từ BaseSkillSO
{
    // Lớp này chỉ có thêm danh sách modifier, các trường khác đã có từ lớp cha
    public List<StatModifierSO> modifiers;
}