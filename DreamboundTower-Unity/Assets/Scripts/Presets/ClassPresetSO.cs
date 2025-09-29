using UnityEngine;

namespace Presets
{
    [CreateAssetMenu(fileName = "ClassPreset", menuName = "Presets/ClassPreset", order = 1)]
    public class ClassPresetSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Skills")]
        public PassiveSkillData passiveSkill;
        public SkillData[] activeSkills; // Một mảng các SkillData SO
    }
}