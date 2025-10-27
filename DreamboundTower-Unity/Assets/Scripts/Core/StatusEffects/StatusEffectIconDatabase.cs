using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Simple icon lookup for status effects
/// Maps effect names to their icon sprites
/// </summary>
[CreateAssetMenu(fileName = "StatusEffectIcons", menuName = "DBT/Status Effect Icons Database")]
public class StatusEffectIconDatabase : ScriptableObject
{
    [System.Serializable]
    public class StatusEffectIconEntry
    {
        public string effectName;
        public Sprite icon;
        public Color iconTint = Color.white;
    }
    
    [Tooltip("List of status effect names and their icons")]
    public List<StatusEffectIconEntry> icons = new List<StatusEffectIconEntry>();
    
    private Dictionary<string, StatusEffectIconEntry> iconCache;
    
    /// <summary>
    /// Gets icon data for a status effect by name
    /// </summary>
    public StatusEffectIconEntry GetIconData(string effectName)
    {
        if (iconCache == null)
        {
            BuildCache();
        }
        
        if (iconCache.TryGetValue(effectName, out StatusEffectIconEntry data))
        {
            return data;
        }
        
        return null;
    }
    
    private void BuildCache()
    {
        iconCache = new Dictionary<string, StatusEffectIconEntry>();
        
        foreach (var entry in icons)
        {
            if (!string.IsNullOrEmpty(entry.effectName))
            {
                iconCache[entry.effectName] = entry;
            }
        }
    }
    
    void OnValidate()
    {
        iconCache = null; // Rebuild cache when changed in editor
    }
}

