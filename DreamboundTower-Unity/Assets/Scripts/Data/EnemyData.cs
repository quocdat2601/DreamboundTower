using UnityEngine;

[CreateAssetMenu(menuName = "DBT/Enemy")]
public class EnemyData : ScriptableObject
{
    public string id = "enemy_basic";
    public string displayName = "Goblin";
    public Sprite portrait = null;

    // base stats are floor-1 multipliers applied by EnemyScaler
    public int baseHP = 40;   // interpreted as HP_stat * HP_UNIT if you store HP_stat, but here store actual base HP
    public int baseSTR = 6;
    public int baseDEF = 2;
    public int baseINT = 0;
    public int baseMana = 0;

    public bool isElite = false;
    public bool isBoss = false;

    // AI behavior tag (enum or string)
    public string aiBehavior = "Aggressive";
    public SkillData[] skills;  // enemy can have skills

    // drop table reference (simplify: ids or real SO ref)
    public string[] guaranteedDrops;       // item ids always dropped
    public string[] possibleDrops;         // item ids possible
    public float[] dropChances;            // parallel array, 0..1

    [TextArea(2, 4)]
    public string lore;
}
