using UnityEngine;

namespace Presets
{
    [CreateAssetMenu(fileName = "RacePreset", menuName = "Presets/RacePreset", order = 0)]
    public class RacePresetSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Stats")]
        public StatBlock baseStats;

        [Header("Skills")]
        public PassiveSkillData passiveSkill;
        public SkillData activeSkill;

        [Header("Character Portraits")]
        public Sprite clericSprite;
        public Sprite mageSprite;
        public Sprite rogueSprite;
        public Sprite warriorSprite;
    }
}