// PassiveSkillManager.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;

/// <summary>
/// Manages passive skills and applies their modifiers to the player character
/// </summary>
public class PassiveSkillManager : MonoBehaviour
{
    #region Variables
    
    [Header("Passive Skills")]
    [Tooltip("Reference to the player character to apply modifiers to")]
    public Character playerCharacter;
    
    [Tooltip("Reference to PlayerSkills component to get learned passive skills")]
    public PlayerSkills playerSkills;
    
    // Track applied modifiers for cleanup
    private List<StatModifierSO> appliedModifiers = new List<StatModifierSO>();
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // Auto-find components if not assigned
        if (playerCharacter == null)
        {
            playerCharacter = FindFirstObjectByType<Character>();
        }
        
        if (playerSkills == null)
        {
            playerSkills = FindFirstObjectByType<PlayerSkills>();
        }
    }
    
    void Start()
    {
        // Try to find PlayerSkills from the player character if not found
        if (playerSkills == null && playerCharacter != null)
        {
            playerSkills = playerCharacter.GetComponent<PlayerSkills>();
        }
        
        // Apply passive skills when the manager starts
        ApplyAllPassiveSkills();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Applies all learned passive skills to the player character
    /// </summary>
    public void ApplyAllPassiveSkills()
    {
        if (playerCharacter == null || playerSkills == null)
        {
            Debug.LogWarning("[PASSIVE] Cannot apply passive skills - missing references");
            return;
        }
        
        // Only apply if not already applied
        if (appliedModifiers.Count > 0)
        {
            Debug.Log("[PASSIVE] Passive skills already applied, skipping");
            return;
        }
        
        // Apply each passive skill
        foreach (var passiveSkill in playerSkills.passiveSkills)
        {
            if (passiveSkill != null)
            {
                ApplyPassiveSkill(passiveSkill);
            }
        }
        
        Debug.Log($"[PASSIVE] Applied {playerSkills.passiveSkills.Count} passive skills to player");
        
        // Update UI after applying passive skills
        if (playerCharacter != null)
        {
            playerCharacter.UpdateHPUI();
            playerCharacter.UpdateManaUI();
        }
    }
    
    /// <summary>
    /// Applies a single passive skill to the player character
    /// </summary>
    public void ApplyPassiveSkill(PassiveSkillData passiveSkill)
    {
        if (passiveSkill == null || playerCharacter == null)
        {
            Debug.LogWarning("[PASSIVE] Cannot apply passive skill - null reference");
            return;
        }
        
        Debug.Log($"[PASSIVE] Applying passive skill: {passiveSkill.displayName}");
        
        // Apply each modifier in the passive skill
        foreach (var modifier in passiveSkill.modifiers)
        {
            if (modifier != null)
            {
                ApplyStatModifier(modifier);
                appliedModifiers.Add(modifier);
            }
        }
    }
    
    /// <summary>
    /// Removes all passive skill modifiers from the player character
    /// </summary>
    public void ClearAllPassiveModifiers()
    {
        if (playerCharacter == null) return;
        
        // Remove each applied modifier
        foreach (var modifier in appliedModifiers)
        {
            if (modifier != null)
            {
                RemoveStatModifier(modifier);
            }
        }
        
        appliedModifiers.Clear();
    }
    
    /// <summary>
    /// Refreshes passive skills (useful when player learns new skills)
    /// </summary>
    public void RefreshPassiveSkills()
    {
        ApplyAllPassiveSkills();
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Applies a single stat modifier to the player character
    /// </summary>
    private void ApplyStatModifier(StatModifierSO modifier)
    {
        if (modifier == null || playerCharacter == null) return;
        
        float modifierValue = modifier.value;
        
        // Apply modifier based on type
        switch (modifier.type)
        {
            case ModifierType.StatBonus:
                ApplyAdditiveModifier(modifier.targetStat, modifierValue);
                break;
                
            case ModifierType.StatBonusPercent:
                ApplyMultiplicativeModifier(modifier.targetStat, modifierValue);
                break;
        }
        
    }
    
    /// <summary>
    /// Removes a single stat modifier from the player character
    /// </summary>
    private void RemoveStatModifier(StatModifierSO modifier)
    {
        if (modifier == null || playerCharacter == null) return;
        
        float modifierValue = -modifier.value; // Reverse the modifier
        
        // Remove modifier based on type
        switch (modifier.type)
        {
            case ModifierType.StatBonus:
                ApplyAdditiveModifier(modifier.targetStat, modifierValue);
                break;
                
            case ModifierType.StatBonusPercent:
                ApplyMultiplicativeModifier(modifier.targetStat, modifierValue);
                break;
        }
        
    }
    
    /// <summary>
    /// Applies an additive modifier (flat stat bonus)
    /// </summary>
    private void ApplyAdditiveModifier(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.HP:
                playerCharacter.maxHP += Mathf.RoundToInt(value);
                break;
            case StatType.STR:
                playerCharacter.attackPower += Mathf.RoundToInt(value);
                break;
            case StatType.DEF:
                playerCharacter.defense += Mathf.RoundToInt(value);
                break;
            case StatType.MANA:
                playerCharacter.mana += Mathf.RoundToInt(value);
                break;
            case StatType.INT:
                playerCharacter.intelligence += Mathf.RoundToInt(value);
                break;
            case StatType.AGI:
                playerCharacter.agility += Mathf.RoundToInt(value);
                break;
            case StatType.ManaRegenPercent:
                // TODO: Implement mana regeneration system
                break;
            case StatType.PhysicalDamagePercent:
                // TODO: Implement physical damage bonus system
                break;
            case StatType.MagicDamagePercent:
                // TODO: Implement magic damage bonus system
                break;
            case StatType.LifestealPercent:
                // TODO: Implement lifesteal system
                break;
            case StatType.DodgeChance:
                // TODO: Implement dodge system
                break;
            case StatType.DamageReduction:
                // TODO: Implement damage reduction system
                break;
        }
    }
    
    /// <summary>
    /// Applies a multiplicative modifier (percentage bonus)
    /// </summary>
    private void ApplyMultiplicativeModifier(StatType statType, float percentageValue)
    {
        // If the value is already a percentage (like 10 for 10%), use it directly
        // If the value is a decimal (like 0.1 for 10%), multiply by 100
        float actualPercentage = percentageValue < 1.0f ? percentageValue * 100.0f : percentageValue;
        float multiplier = 1.0f + (actualPercentage / 100.0f);
        
        Debug.Log($"[PASSIVE] Applied StatBonusPercent modifier: {statType} +{actualPercentage}% (original value: {percentageValue})");
        
        switch (statType)
        {
            case StatType.HP:
                playerCharacter.maxHP = Mathf.RoundToInt(playerCharacter.maxHP * multiplier);
                break;
            case StatType.STR:
                playerCharacter.attackPower = Mathf.RoundToInt(playerCharacter.attackPower * multiplier);
                break;
            case StatType.DEF:
                playerCharacter.defense = Mathf.RoundToInt(playerCharacter.defense * multiplier);
                break;
            case StatType.MANA:
                playerCharacter.mana = Mathf.RoundToInt(playerCharacter.mana * multiplier);
                break;
            case StatType.INT:
                playerCharacter.intelligence = Mathf.RoundToInt(playerCharacter.intelligence * multiplier);
                break;
            case StatType.AGI:
                playerCharacter.agility = Mathf.RoundToInt(playerCharacter.agility * multiplier);
                break;
            case StatType.ManaRegenPercent:
                // TODO: Implement mana regeneration system
                break;
            case StatType.PhysicalDamagePercent:
                // TODO: Implement physical damage bonus system
                break;
            case StatType.MagicDamagePercent:
                // TODO: Implement magic damage bonus system
                break;
            case StatType.LifestealPercent:
                // TODO: Implement lifesteal system
                break;
            case StatType.DodgeChance:
                // TODO: Implement dodge system
                break;
            case StatType.DamageReduction:
                // TODO: Implement damage reduction system
                break;
        }
    }
    
    #endregion
}
