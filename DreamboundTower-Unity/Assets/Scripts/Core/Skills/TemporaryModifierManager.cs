using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;

/// <summary>
/// Manages temporary stat modifiers from skills (buffs/debuffs)
/// Tracks modifiers and removes them after duration expires
/// </summary>
public class TemporaryModifierManager : MonoBehaviour
{
    /// <summary>
    /// Represents a temporary modifier applied by a skill
    /// </summary>
    private class TemporaryModifier
    {
        public StatModifierSO modifier;
        public int remainingTurns;
        public Character target;
        
        public TemporaryModifier(StatModifierSO mod, int duration, Character targetCharacter)
        {
            modifier = mod;
            remainingTurns = duration;
            target = targetCharacter;
        }
    }
    
    private List<TemporaryModifier> activeModifiers = new List<TemporaryModifier>();
    private Character character;
    
    void Awake()
    {
        character = GetComponent<Character>();
    }
    
    /// <summary>
    /// Applies a temporary stat modifier from a skill
    /// </summary>
    public void ApplyTemporaryModifier(StatModifierSO modifier, int duration)
    {
        if (modifier == null || character == null) return;
        
        // Apply the modifier using Character's gear modifier methods
        character.ApplyGearModifier(modifier);
        
        // Track the modifier for removal
        activeModifiers.Add(new TemporaryModifier(modifier, duration, character));
        
        Debug.Log($"[TEMPORARY MODIFIER] Applied {modifier.name} to {character.name} for {duration} turns");
    }
    
    /// <summary>
    /// Removes a temporary stat modifier
    /// </summary>
    public void RemoveTemporaryModifier(StatModifierSO modifier)
    {
        if (modifier == null || character == null) return;
        
        // Remove the modifier using Character's gear modifier removal
        character.RemoveGearModifier(modifier);
        
        // Remove from tracking list
        activeModifiers.RemoveAll(m => m.modifier == modifier);
        
        Debug.Log($"[TEMPORARY MODIFIER] Removed {modifier.name} from {character.name}");
    }
    
    /// <summary>
    /// Processes all temporary modifiers, decrements duration, and removes expired ones
    /// Call this at the end of each turn
    /// Note: Modifiers with duration 0 are permanent for the battle and will not be removed
    /// </summary>
    public void ProcessTemporaryModifiers()
    {
        if (character == null) return;
        
        var modifiersToRemove = new List<TemporaryModifier>();
        
        foreach (var tempMod in activeModifiers)
        {
            // If duration was 0 (permanent for battle), skip decrementing
            if (tempMod.remainingTurns == 0)
            {
                continue; // Permanent modifier, don't remove
            }
            
            tempMod.remainingTurns--;
            
            if (tempMod.remainingTurns <= 0)
            {
                modifiersToRemove.Add(tempMod);
            }
        }
        
        // Remove expired modifiers
        foreach (var tempMod in modifiersToRemove)
        {
            RemoveTemporaryModifier(tempMod.modifier);
        }
    }
    
    /// <summary>
    /// Clears all temporary modifiers (useful for battle end)
    /// </summary>
    public void ClearAllTemporaryModifiers()
    {
        var allModifiers = new List<StatModifierSO>();
        foreach (var tempMod in activeModifiers)
        {
            allModifiers.Add(tempMod.modifier);
        }
        
        foreach (var modifier in allModifiers)
        {
            RemoveTemporaryModifier(modifier);
        }
        
        activeModifiers.Clear();
    }
    
    /// <summary>
    /// Gets the Character component (for SkillEffectProcessor access)
    /// </summary>
    public Character GetCharacter()
    {
        return character;
    }
    
    /// <summary>
    /// Gets or creates TemporaryModifierManager for a character
    /// </summary>
    public static TemporaryModifierManager GetOrAdd(Character target)
    {
        if (target == null) return null;
        
        var manager = target.GetComponent<TemporaryModifierManager>();
        if (manager == null)
        {
            manager = target.gameObject.AddComponent<TemporaryModifierManager>();
        }
        
        return manager;
    }
}

