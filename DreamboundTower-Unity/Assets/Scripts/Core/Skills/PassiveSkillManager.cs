// PassiveSkillManager.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;
using StatusEffects;

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
    public List<StatModifierSO> appliedModifiers = new List<StatModifierSO>();
    
    // Cache ConditionalPassiveManager to avoid repeated GetComponent calls
    private ConditionalPassiveManager conditionalPassiveManager;
    
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
        
        // Cache ConditionalPassiveManager reference
        if (playerCharacter != null)
        {
            conditionalPassiveManager = playerCharacter.GetComponent<ConditionalPassiveManager>();
            if (conditionalPassiveManager == null)
            {
                Debug.LogWarning("[PASSIVE] ConditionalPassiveManager not found on player character in Awake()");
            }
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
            return;
        }
        
        // Refresh ConditionalPassiveManager reference in case it was added after Awake
        if (conditionalPassiveManager == null && playerCharacter != null)
        {
            conditionalPassiveManager = playerCharacter.GetComponent<ConditionalPassiveManager>();
        }
        
        // Apply each passive skill
        foreach (var passiveSkill in playerSkills.passiveSkills)
        {
            if (passiveSkill != null)
            {
                ApplyPassiveSkill(passiveSkill);
            }
        }
        
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
        
        // Reset mana regeneration bonus
        playerCharacter.ResetAllPassiveBonuses();
        
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
            case ModifierType.Additive:
                ApplyAdditiveModifier(modifier.targetStat, modifierValue);
                break;
                
            case ModifierType.Multiplicative:
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
            case ModifierType.Additive:
                ApplyAdditiveModifier(modifier.targetStat, modifierValue);
                break;
                
            case ModifierType.Multiplicative:
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
            case StatType.LowHpPhysicalDamageBonus:
                // Add low HP physical damage bonus (additive)
                if (conditionalPassiveManager != null)
                {
                    conditionalPassiveManager.lowHpPhysicalDamageBonus += value;
                }
                break;
            case StatType.LowHpDamageReduction:
                // Add low HP damage reduction (additive)
                if (conditionalPassiveManager != null)
                {
                    conditionalPassiveManager.lowHpDamageReduction += value;
                }
                break;
            case StatType.LowHpLifesteal:
                // Add low HP lifesteal (additive)
                if (conditionalPassiveManager != null)
                {
                    conditionalPassiveManager.lowHpLifesteal += value;
                }
                break;
            case StatType.NonBossDamageReduction:
                // Add non-boss damage reduction (additive)
                if (conditionalPassiveManager != null)
                {
                    conditionalPassiveManager.nonBossDamageReduction += value;
                }
                break;
            case StatType.LowDefDamageBonus:
                // Add low DEF damage bonus (additive)
                if (conditionalPassiveManager != null)
                {
                    conditionalPassiveManager.lowDefDamageBonus += value;
                }
                break;
            case StatType.ManaRegenPerTurn:
                // Add mana regen per turn (additive)
                if (conditionalPassiveManager != null)
                {
                    conditionalPassiveManager.manaRegenPerTurn += value;
                }
                break;
            case StatType.MagicDamagePercent:
                // Add magic damage bonus (additive)
                playerCharacter.magicDamageBonus += value;
                break;
            case StatType.AdaptiveSpiritBonus:
                // Add +2 to all base stats (Adaptive Spirit)
                int bonusPoints = Mathf.RoundToInt(value);
                playerCharacter.baseMaxHP += bonusPoints * 10; // 1 point = 10 HP
                playerCharacter.baseAttackPower += bonusPoints;
                playerCharacter.baseDefense += bonusPoints;
                playerCharacter.baseMana += bonusPoints * 5; // 1 point = 5 mana
                playerCharacter.baseIntelligence += bonusPoints;
                playerCharacter.baseAgility += bonusPoints;
                
                // Recalculate current stats
                playerCharacter.ResetToBaseStats();
                break;
            case StatType.MagicDamageFlat:
                // Add flat magic damage bonus (additive)
                playerCharacter.magicDamageFlat += Mathf.RoundToInt(value);
                break;
            case StatType.PhysicalDamageFlat:
                // Add flat physical damage bonus (additive)
                playerCharacter.physicalDamageFlat += Mathf.RoundToInt(value);
                break;
            default:
                Debug.LogWarning($"[PASSIVE] Unhandled StatType in ApplyAdditiveModifier: {statType} ({(int)statType})");
                break;
        }
    }
    
    /// <summary>
    /// Applies a multiplicative modifier (percentage bonus)
    /// </summary> 
    private void ApplyMultiplicativeModifier(StatType statType, float percentageValue)
    {
        // If the value is already a percentage (like 10 for 10%), use it directly
        // If the value is a decimal (like 0.1 for 10% or 1.0 for 100%), multiply by 100
        // Values <= 1.0 are treated as decimals (0.5 = 50%, 1.0 = 100%)
        // Values > 1.0 are treated as percentages (10 = 10%, 100 = 100%)
        float actualPercentage = percentageValue <= 1.0f ? percentageValue * 100.0f : percentageValue;
        float multiplier = 1.0f + (actualPercentage / 100.0f);
        
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
                // Add mana regeneration bonus (multiplicative)
                playerCharacter.manaRegenBonus += actualPercentage / 100f; // Convert percentage to decimal
                break;
            case StatType.PhysicalDamagePercent:
                // Add physical damage bonus (multiplicative)
                playerCharacter.physicalDamageBonus += actualPercentage / 100f;
                break;
            case StatType.MagicDamagePercent:
                // Add magic damage bonus (multiplicative)
                playerCharacter.magicDamageBonus += actualPercentage / 100f;
                break;
            case StatType.LifestealPercent:
                // Add lifesteal percentage (multiplicative)
                playerCharacter.lifestealPercent += actualPercentage / 100f;
                break;
            case StatType.DodgeChance:
                // Add dodge chance bonus (multiplicative)
                playerCharacter.dodgeBonusFromGearPassives += actualPercentage / 100f;
                playerCharacter.UpdateDerivedStats(); // Recalculate total dodge
                break;
            case StatType.DamageReduction:
                // Add damage reduction (multiplicative)
                playerCharacter.damageReduction += actualPercentage / 100f;
                break;
            case StatType.CritChance:
                // Add critical chance (multiplicative)
                playerCharacter.criticalChance += actualPercentage / 100f;
                break;
            case StatType.CritDamagePercent:
                // Add critical damage multiplier (multiplicative)
                // If value is 1.0 (100%), add 1.0x multiplier (total becomes 2.5x base 1.5x)
                playerCharacter.critDamageMultiplier += actualPercentage / 100f;
                break;
            case StatType.ReflectDamagePercent:
                // Add reflect damage percentage (multiplicative)
                playerCharacter.gearReflectDamagePercent += actualPercentage / 100f;
                break;
            default:
                Debug.LogWarning($"[PASSIVE] Unhandled StatType in ApplyMultiplicativeModifier: {statType} ({(int)statType})");
                break;
        }
    }
    
    #endregion
}
