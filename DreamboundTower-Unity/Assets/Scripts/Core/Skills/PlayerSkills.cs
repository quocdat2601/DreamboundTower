// File: PlayerSkills.cs
using System.Collections.Generic;
using UnityEngine;
using Presets; // Namespace chứa các ScriptableObject

public class PlayerSkills : MonoBehaviour
{
    [Header("Learned Skills")]
    public List<PassiveSkillData> passiveSkills = new List<PassiveSkillData>();
    public List<SkillData> activeSkills = new List<SkillData>();

    /// <summary>
    /// Xóa các skill cũ và học skill mới từ Race và Class đã chọn.
    /// </summary>
    public void LearnSkills(RacePresetSO race, ClassPresetSO charClass)
    {
        // Xóa tất cả skill cũ để đảm bảo danh sách luôn mới
        passiveSkills.Clear();
        activeSkills.Clear();

        // Thêm các skill bị động (Passive)
        if (race.passiveSkill != null)
        {
            passiveSkills.Add(race.passiveSkill);
        }
        if (charClass.passiveSkill != null)
        {
            passiveSkills.Add(charClass.passiveSkill);
        }

        // Thêm các skill chủ động (Active)
        if (race.activeSkill != null)
        {
            activeSkills.Add(race.activeSkill);
        }
        if (charClass.activeSkills != null)
        {
            activeSkills.AddRange(charClass.activeSkills);
        }

        Debug.Log($"Player has learned {passiveSkills.Count} passive skills and {activeSkills.Count} active skills.");
    }
}