using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using StatusEffects;

/// <summary>
/// Manages the display of status effect icons for a character
/// Spawns and updates status effect icons automatically
/// </summary>
public class StatusEffectDisplayManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Prefab for individual status effect icon")]
    public GameObject statusEffectIconPrefab;
    
    [Tooltip("Container for status effect icons (should have Horizontal Layout Group)")]
    public Transform iconContainer;
    
    [Tooltip("Database of status effect icons")]
    public StatusEffectIconDatabase iconDatabase;
    
    private Character targetCharacter;
    private Dictionary<string, StatusEffectIconUI> activeIcons = new Dictionary<string, StatusEffectIconUI>();
    
    private float lastUpdateTime = 0f;
    private const float UPDATE_INTERVAL = 0.2f; // Update every 0.2 seconds
    
    // Track what we've logged to avoid spam
    private HashSet<string> loggedMissingPrefabs = new HashSet<string>();
    private int lastEffectCount = -1;
    
    void OnEnable()
    {
        // Only auto-find character if not already set
        // This allows the character to be set manually for enemies
        if (targetCharacter == null)
        {
            AutoSetTargetCharacter();
        }
    }
    
    void Update()
    {
        if (Time.time - lastUpdateTime > UPDATE_INTERVAL)
        {
            RefreshDisplay();
            lastUpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Automatically finds and sets the player character
    /// Called automatically when this component is enabled (only if target not set)
    /// </summary>
    private void AutoSetTargetCharacter()
    {
        // Try to find player character from GameManager
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            Character playerChar = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerChar != null)
            {
                targetCharacter = playerChar;
                return;
            }
        }
        
        // Fallback: Search scene for player character
        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character character in characters)
        {
            if (character.isPlayer)
            {
                targetCharacter = character;
                return;
            }
        }
    }
    
    /// <summary>
    /// Sets the target character to display effects for
    /// </summary>
    public void SetTargetCharacter(Character character)
    {
        targetCharacter = character;
        RefreshDisplay();
    }
    
    /// <summary>
    /// Refreshes the display based on current active effects
    /// </summary>
    public void RefreshDisplay()
    {
        // Auto-find character if not set yet
        if (targetCharacter == null)
        {
            AutoSetTargetCharacter();
            if (targetCharacter == null) return; // Still no character found
        }
        
        if (StatusEffectManager.Instance == null)
        {
            Debug.LogWarning("[STATUS ICON] StatusEffectManager.Instance is null!");
            return;
        }
        
        if (iconDatabase == null)
        {
            Debug.LogWarning("[STATUS ICON] Icon Database is not assigned!");
            return;
        }
        
        // Get active effects from StatusEffectManager
        List<StatusEffect> activeEffects = StatusEffectManager.Instance.GetActiveEffects(targetCharacter);
        
        // Only log when effect count changes
        if (activeEffects.Count != lastEffectCount)
        {
            Debug.Log($"[STATUS ICON] Found {activeEffects.Count} active effects on {targetCharacter?.name}");
            lastEffectCount = activeEffects.Count;
        }
        
        HashSet<string> currentEffectNames = new HashSet<string>();
        
        foreach (var effect in activeEffects)
        {
            currentEffectNames.Add(effect.effectName);
        }
        
        // Remove icons for effects that no longer exist
        List<string> effectsToRemove = new List<string>();
        foreach (var kvp in activeIcons)
        {
            if (!currentEffectNames.Contains(kvp.Key))
            {
                effectsToRemove.Add(kvp.Key);
            }
        }
        
        foreach (string effectName in effectsToRemove)
        {
            if (activeIcons.TryGetValue(effectName, out StatusEffectIconUI icon))
            {
                Destroy(icon.gameObject);
                activeIcons.Remove(effectName);
            }
        }
        
        // Update or create icons for current effects
        foreach (var effect in activeEffects)
        {
            if (iconDatabase != null)
            {
                var iconData = iconDatabase.GetIconData(effect.effectName);

                // Debug: Log if icon not found (but skip Shield since it has alternative UI display)
                if (iconData == null && effect.effectName != "Shield" && !loggedMissingPrefabs.Contains(effect.effectName))
                {
                    Debug.LogWarning($"[STATUS ICON] No icon data found for effect: '{effect.effectName}'");
                    loggedMissingPrefabs.Add(effect.effectName);
                }
                else if (iconData != null && iconData.icon == null && !loggedMissingPrefabs.Contains(effect.effectName))
                {
                    Debug.LogWarning($"[STATUS ICON] Icon sprite is null for effect: '{effect.effectName}'");
                    loggedMissingPrefabs.Add(effect.effectName);
                }
                
                if (iconData != null && iconData.icon != null)
                {
                    bool iconExists = activeIcons.TryGetValue(effect.effectName, out StatusEffectIconUI existingIcon);
                    
                    if (iconExists)
                    {
                        // Update existing icon (silently)
                        existingIcon.Setup(effect, iconData.icon, iconData.iconTint);
                    }
                    else
                    {
                        // Create new icon
                        if (statusEffectIconPrefab != null && iconContainer != null)
                        {
                            Debug.Log($"[STATUS ICON] Creating icon for {effect.effectName}");
                            GameObject iconGO = Instantiate(statusEffectIconPrefab, iconContainer);
                            StatusEffectIconUI iconUI = iconGO.GetComponent<StatusEffectIconUI>();
                            
                            if (iconUI != null)
                            {
                                iconUI.Setup(effect, iconData.icon, iconData.iconTint);
                                activeIcons[effect.effectName] = iconUI;
                            }
                            else
                            {
                                Debug.LogError("[STATUS ICON] StatusEffectIconUI component not found on prefab!");
                            }
                        }
                        else
                        {
                            if (statusEffectIconPrefab == null && !loggedMissingPrefabs.Contains("prefab"))
                            {
                                Debug.LogWarning("[STATUS ICON] statusEffectIconPrefab is NULL!");
                                loggedMissingPrefabs.Add("prefab");
                            }
                            if (iconContainer == null && !loggedMissingPrefabs.Contains("container"))
                            {
                                Debug.LogWarning("[STATUS ICON] iconContainer is NULL!");
                                loggedMissingPrefabs.Add("container");
                            }
                        }
                    }
                }
            }
        }
        
        // Update all icon durations
        foreach (var kvp in activeIcons)
        {
            kvp.Value.UpdateDisplay();
        }
    }
}

