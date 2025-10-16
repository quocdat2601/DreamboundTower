using System.Collections.Generic;
using UnityEngine;

namespace Presets
{
	public enum EnemyKind { Normal, Elite, Boss }

	[CreateAssetMenu(fileName = "EnemyTemplate", menuName = "Presets/EnemyTemplate", order = 2)]
	public class EnemyTemplateSO : ScriptableObject
	{
		public EnemyKind kind;

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



