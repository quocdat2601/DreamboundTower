using System.Collections.Generic;
using UnityEngine;
using StatusEffects;

/// <summary>
/// Manages all status effects in battle
/// </summary>
public class StatusEffectManager : MonoBehaviour
{
    public static StatusEffectManager Instance { get; private set; }
    
    [Header("Status Effects")]
    [SerializeField] private List<Character> charactersWithEffects = new List<Character>();
    
    private Dictionary<Character, List<StatusEffect>> activeEffects = new Dictionary<Character, List<StatusEffect>>();
    
    #region Unity Lifecycle
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Applies a status effect to a character
    /// </summary>
    public void ApplyEffect(Character target, StatusEffect effect)
    {
        if (target == null || effect == null) return;
        
        // Initialize character's effect list if needed
        if (!activeEffects.ContainsKey(target))
        {
            activeEffects[target] = new List<StatusEffect>();
            if (!charactersWithEffects.Contains(target))
            {
                charactersWithEffects.Add(target);
            }
        }
        
        // Check if effect already exists and handle stacking
        var existingEffect = activeEffects[target].Find(e => e.GetType() == effect.GetType());
        if (existingEffect != null)
        {
            // For shield effects, stack the amounts instead of replacing
            if (effect is ShieldEffect)
            {
                existingEffect.intensity += effect.intensity; // Add shield amounts
                existingEffect.duration = Mathf.Max(existingEffect.duration, effect.duration); // Use longer duration
            }
            else
            {
                // For other effects, refresh (replace)
                existingEffect.Refresh(effect.duration, effect.intensity);
            }
        }
        else
        {
            activeEffects[target].Add(effect);
            effect.OnApply(target);
        }
    }
    
    /// <summary>
    /// Removes a specific effect from a character
    /// </summary>
    public void RemoveEffect(Character target, System.Type effectType)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return;
        
        var effect = activeEffects[target].Find(e => e.GetType() == effectType);
        if (effect != null)
        {
            effect.OnRemove(target);
            activeEffects[target].Remove(effect);
        }
    }
    
    /// <summary>
    /// Removes all effects from a character
    /// </summary>
    public void RemoveAllEffects(Character target)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return;
        
        foreach (var effect in activeEffects[target])
        {
            effect.OnRemove(target);
        }
        activeEffects[target].Clear();
        charactersWithEffects.Remove(target);
    }
    
    /// <summary>
    /// Processes all status effects at the start of a turn
    /// </summary>
    public void ProcessStartOfTurnEffects()
    {
        foreach (var character in charactersWithEffects.ToArray())
        {
            if (character == null) continue;
            
            ProcessCharacterEffects(character, StatusEffects.EffectTiming.StartOfTurn);
        }
    }
    
    /// <summary>
    /// Processes all status effects at the end of a turn
    /// </summary>
    public void ProcessEndOfTurnEffects()
    {
        foreach (var character in charactersWithEffects.ToArray())
        {
            if (character == null) continue;
            
            ProcessCharacterEffects(character, StatusEffects.EffectTiming.EndOfTurn);
        }
    }
    
    /// <summary>
    /// Checks if a character has a specific effect
    /// </summary>
    public bool HasEffect(Character target, System.Type effectType)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return false;
        return activeEffects[target].Exists(e => e.GetType() == effectType);
    }
    
    /// <summary>
    /// Gets a specific effect from a character
    /// </summary>
    public StatusEffect GetEffect(Character target, System.Type effectType)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return null;
        return activeEffects[target].Find(e => e.GetType() == effectType);
    }
    
    /// <summary>
    /// Gets all active effects on a character (for UI display)
    /// </summary>
    public List<StatusEffect> GetActiveEffects(Character target)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return new List<StatusEffect>();
        return new List<StatusEffect>(activeEffects[target]);
    }
    
    /// <summary>
    /// Returns total heal bonus percent from all HealBonusEffect on target (can be negative)
    /// </summary>
    public int GetHealBonusPercent(Character target)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return 0;
        int total = 0;
        foreach (var effect in activeEffects[target])
        {
            var healBonus = effect as StatusEffects.HealBonusEffect;
            if (healBonus != null)
            {
                total += healBonus.intensity;
            }
        }
        return total;
    }
    
    /// <summary>
    /// Decrements duration for event-based status effects on a character (called at end of battle node)
    /// Only affects effects marked as isEventBased=true
    /// </summary>
    public void DecrementEventBasedEffectsPerBattleNode(Character target)
    {
        if (target == null || !activeEffects.ContainsKey(target)) return;
        
        var effectsToRemove = new List<StatusEffect>();
        
        foreach (var effect in activeEffects[target])
        {
            // Only decrement event-based effects
            if (effect.isEventBased)
            {
                effect.duration--;
                
                if (effect.duration <= 0)
                {
                    effectsToRemove.Add(effect);
                }
            }
        }
        
        // Remove expired event-based effects
        foreach (var effect in effectsToRemove)
        {
            effect.OnRemove(target);
            activeEffects[target].Remove(effect);
        }
        
        // Clean up empty character entries
        if (activeEffects[target].Count == 0)
        {
            charactersWithEffects.Remove(target);
        }
    }
    
    #endregion
    
    #region Private Methods
    
    private void ProcessCharacterEffects(Character character, StatusEffects.EffectTiming timing)
    {
        if (!activeEffects.ContainsKey(character)) return;
        
        // Don't process effects on dead characters
        if (character.currentHP <= 0) return;
        
        var effectsToRemove = new List<StatusEffect>();
        
        foreach (var effect in activeEffects[character])
        {
            if (effect.timing == timing)
            {
                effect.OnTick(character);
                // Only decrement duration for non-event-based effects (combat effects)
                // Event-based effects decrement per battle node, not per turn
                if (!effect.isEventBased)
                {
                    effect.duration--;
                    
                    if (effect.duration <= 0)
                    {
                        effectsToRemove.Add(effect);
                    }
                }
            }
        }
        
        // Remove expired effects
        foreach (var effect in effectsToRemove)
        {
            effect.OnRemove(character);
            activeEffects[character].Remove(effect);
        }
        
        // Clean up empty character entries
        if (activeEffects[character].Count == 0)
        {
            charactersWithEffects.Remove(character);
        }
    }
    
    /// <summary>
    /// Removes all negative status effects from a character
    /// </summary>
    public void RemoveAllNegativeEffects(Character character)
    {
        if (!activeEffects.ContainsKey(character)) return;
        
        var effectsToRemove = new List<StatusEffect>();
        
        foreach (var effect in activeEffects[character])
        {
            if (effect.isNegative)
            {
                effectsToRemove.Add(effect);
            }
        }
        
        foreach (var effect in effectsToRemove)
        {
            effect.OnRemove(character);
            activeEffects[character].Remove(effect);
        }
        
        // Clean up empty character entries
        if (activeEffects[character].Count == 0)
        {
            charactersWithEffects.Remove(character);
        }
    }
    
    #endregion
}
