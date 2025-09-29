using UnityEngine;

public enum ResourceType { None, Mana }
public enum StatType { STR, INT, DEF, HP, MANA, AGI, None }
public enum TargetType { SingleEnemy, AllEnemies, Self, Ally, AllAlly }

[CreateAssetMenu(menuName = "DBT/Skill")]
public class SkillData : ScriptableObject
{
    public string id = "skill_id";
    public string displayName = "Skill Name";
    public Sprite icon = null;

    public ResourceType resource = ResourceType.Mana;
    public int cost = 0;            // mana cost (if ResourceType.Mana)
    public int baseDamage = 10;     // SkillBase
    public StatType scalingStat = StatType.STR;
    [Range(0f, 5f)]
    public float scalingPercent = 1.0f; // e.g., 1.0 means 100% -> formula uses (1 + stat/100)

    public int cooldown = 0;       // cooldown in turns
    public TargetType target = TargetType.SingleEnemy;
    public bool isAoE = false;

    // Status/effect arrays - reference to other data SOs
    public string[] applyStatusIds; // e.g. "stun", "burn"
    public float procChance = 1f;   // chance to apply status (0..1)

    [TextArea(2, 3)]
    public string description;
}
