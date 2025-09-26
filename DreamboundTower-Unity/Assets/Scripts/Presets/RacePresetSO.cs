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

		[Header("Passive")]
		public string passiveName;
		[TextArea]
		public string passiveDescription;

		[Header("Active Skill")]
		public string activeName;
		public int activeCooldown;
		public int activeManaCost;
		[TextArea]
		public string activeDescription;
	}
}



