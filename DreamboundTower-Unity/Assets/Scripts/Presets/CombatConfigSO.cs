using UnityEngine;

namespace Presets
{
	[CreateAssetMenu(fileName = "CombatConfig", menuName = "Presets/CombatConfig", order = 3)]
	public class CombatConfigSO : ScriptableObject
	{
		[Header("Resolved for next combat")]
		public EnemyKind enemyKind;
		public StatBlock enemyStats;
		public int absoluteFloor;
		public string enemyArchetypeId; // optional hook for visuals
	}
}


