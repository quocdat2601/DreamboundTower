using UnityEngine;

namespace Presets
{
	[CreateAssetMenu(fileName = "ClassPreset", menuName = "Presets/ClassPreset", order = 1)]
	public class ClassPresetSO : ScriptableObject
	{
		[Header("Identity")]
		public string id;
		public string displayName;

		[Header("Passive")]
		public string passiveName;
		[TextArea]
		public string passiveDescription;

		[Header("Active Skills")]
		public string[] activeNames;
		public int[] activeCooldowns;
		public int[] activeManaCosts;
		[TextArea]
		public string[] activeDescriptions;
	}
}



