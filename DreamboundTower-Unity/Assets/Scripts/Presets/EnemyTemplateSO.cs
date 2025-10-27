using System.Collections.Generic;
using UnityEngine;

namespace Presets
{
	public enum EnemyKind { Normal, Elite, Boss }

    [System.Flags]
    public enum EnemyGimmick
    {
        // Giá trị phải là lũy thừa của 2 (0, 1, 2, 4, 8, ...)
        None = 0,      // 0
        Resurrect = 1 << 0, // 1
        SplitOnDamage = 1 << 1, // 2
        CounterAttack = 1 << 2, // 4
        Ranged = 1 << 8,
		Enrage = 1 << 16,
		Bony = 1 << 17,
		Thornmail = 1 << 18,
        Regenerator = 1 << 19

    }

    [CreateAssetMenu(fileName = "EnemyTemplate", menuName = "Presets/EnemyTemplate", order = 2)]
	public class EnemyTemplateSO : ScriptableObject
	{
        [Header("Classification")]
        public EnemyKind kind;
        public EnemyGimmick gimmick;

		[Header("Combat Details")]
		[Tooltip("Lượng mana quái vật hồi lại vào đầu mỗi lượt của nó.")]
		public int manaRegenPerTurnn;

        [Tooltip("Danh sách các kỹ năng mà loài quái vật này có thể sử dụng.")]
        public List<SkillData> skills;

        [Header("Visuals")]
        public List<Sprite> sprites;

        [Header("Base @ Floor 1")]
		public StatBlock baseStatsAtFloor1;

		[Header("Multipliers (Elite/Boss)")]
		public float hpMultiplier = 1f;
		public float strMultiplier = 1f;	
		public float defMultiplier = 1f;

		[Header("Growth")]
		public float baseRate = 0.012f;
		public int endlessAfterFloor = 100;
		public float endlessBonus = 0.002f;

		public StatBlock GetStatsAtFloor(int floor)
		{
			float rate = baseRate + (floor > endlessAfterFloor ? endlessBonus : 0f);
			float scale = Mathf.Pow(1f + rate, Mathf.Max(0, floor - 1));
			StatBlock s;
			s.HP = Mathf.RoundToInt(baseStatsAtFloor1.HP * scale * hpMultiplier);
			s.STR = Mathf.RoundToInt(baseStatsAtFloor1.STR * scale * strMultiplier);
			s.DEF = Mathf.RoundToInt(baseStatsAtFloor1.DEF * scale * defMultiplier);
			s.MANA = Mathf.RoundToInt(baseStatsAtFloor1.MANA * scale);
			s.INT = Mathf.RoundToInt(baseStatsAtFloor1.INT * scale);
			s.AGI = Mathf.RoundToInt(baseStatsAtFloor1.AGI * scale);
			return s;
		}
	}
}

