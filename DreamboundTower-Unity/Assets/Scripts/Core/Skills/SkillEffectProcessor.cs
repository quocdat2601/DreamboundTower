using UnityEngine;
using StatusEffects;
using Assets.Scripts.Data;

/// <summary>
/// Processes skill effects based on SkillData
/// This replaces the rigid keyword-based system
/// </summary>
public static class SkillEffectProcessor
{
    #region Public Methods
    
    /// <summary>
    /// Processes all effects for a skill
    /// </summary>
    /// <param name="skillData">The skill data containing effect information</param>
    /// <param name="caster">The character casting the skill</param>
    /// <param name="target">The target character (optional, defaults to caster for self-targeting skills)</param>
    public static void ProcessSkillEffects(SkillData skillData, Character caster, Character target = null)
    {
        if (skillData == null || caster == null) return;
        
        // Use target if provided, otherwise use caster (for self-targeting skills)
        Character actualTarget = target ?? caster;
        
        // Process all effect types
        ProcessDamageEffects(skillData, caster, actualTarget);
        ProcessHealingEffects(skillData, caster, actualTarget);
        ProcessShieldEffects(skillData, actualTarget);
        ProcessStatusEffects(skillData, actualTarget);
        ProcessBuffDebuffEffects(skillData, actualTarget);
        ProcessSpecialEffects(skillData, actualTarget);
        ProcessRecoilEffects(skillData, caster);
    }
    
    #endregion

    #region Private Methods - Damage Effects
    
    /// <summary>
    /// Processes damage effects including multi-hit functionality
    /// </summary>
    private static void ProcessDamageEffects(SkillData skillData, Character caster, Character target)
    {
        if (skillData.baseDamage <= 0) return;
        
        // Calculate base damage with stat scaling
        int scalingValue = GetScalingValue(caster, skillData.scalingStat);
        int scaledDamage = Mathf.RoundToInt(scalingValue * skillData.scalingPercent);
        int totalDamage = skillData.baseDamage + scaledDamage;
        
        // Apply Pounce effect bonus if active
        float pounceBonus = GetPounceBonus(caster);
        if (pounceBonus > 0)
        {
            totalDamage = Mathf.RoundToInt(totalDamage * (1f + pounceBonus / 100f));
            // Remove pounce effect after use
            StatusEffectManager.Instance?.RemoveEffect(caster, typeof(PounceEffect));
        }
        
        // Apply bonus damage to stunned targets
        if (skillData.bonusDamageToStunned > 0 && IsTargetStunned(target))
        {
            int bonusDamage = Mathf.RoundToInt(totalDamage * skillData.bonusDamageToStunned);
            totalDamage += bonusDamage;
            Debug.Log($"[SKILL EFFECT] Bonus damage to stunned target: +{bonusDamage} damage (total: {totalDamage})");
        }
        
        // Process multiple hits
        int hitCount = Mathf.Max(1, skillData.hitCount);
        int totalDamageDealt = 0;
        
        for (int i = 0; i < hitCount; i++)
        {
            // Apply damage
            int damageDealt = 0;
            if (skillData.isMagicDamage)
            {
                // Calculate magic damage with passive bonuses
                int magicDamage = caster.CalculateMagicDamage(totalDamage);
                damageDealt = target.TakeDamageWithShield(magicDamage, caster, isMagicalDamage: true);
            }
            else
            {
                // Calculate physical damage with passive bonuses
                int physicalDamage = caster.CalculatePhysicalDamage(totalDamage);
                damageDealt = target.TakeDamageWithShield(physicalDamage, caster);
            }
            
            totalDamageDealt += damageDealt;
            
            // Apply lifesteal for each hit if specified
            if (skillData.lifesteal && skillData.lifestealPercent > 0)
            {
                int healAmount = Mathf.RoundToInt(damageDealt * skillData.lifestealPercent);
                caster.RestoreHealth(healAmount);
            }
            
            // Apply chance-based stun after each hit
            if (skillData.stunChance > 0f)
            {
                float roll = Random.Range(0f, 1f);
                if (roll <= skillData.stunChance)
                {
                    var stunEffect = new StunEffect(skillData.statusEffectDuration);
                    StatusEffectManager.Instance?.ApplyEffect(target, stunEffect);
                    Debug.Log($"[SKILL EFFECT] Stun applied! Roll: {roll:F2} <= {skillData.stunChance:F2}");
                }
            }
        }
    }
    
    #endregion

    #region Private Methods - Healing Effects
    
    /// <summary>
    /// Processes healing effects
    /// </summary>
    private static void ProcessHealingEffects(SkillData skillData, Character caster, Character target)
    {
        int healAmount = skillData.healAmount;
        
        // Add percentage-based healing
        if (skillData.healPercent > 0)
        {
            healAmount += Mathf.RoundToInt(target.maxHP * skillData.healPercent);
        }
        
        if (healAmount > 0)
        {
            // Apply healing effectiveness bonus if active
            float effectivenessBonus = GetHealingEffectivenessBonus(target);
            if (effectivenessBonus > 0)
            {
                int originalHeal = healAmount;
                healAmount = Mathf.RoundToInt(healAmount * (1f + effectivenessBonus));
                Debug.Log($"[SKILL EFFECT] Healing effectiveness bonus applied: {originalHeal} -> {healAmount} (+{effectivenessBonus * 100f}%)");
            }
            
            Debug.Log($"[SKILL EFFECT] Healing {target.name} for {healAmount} HP");
            target.RestoreHealth(healAmount);
        }
    }
    
    #endregion

    #region Private Methods - Shield Effects
    
    /// <summary>
    /// Processes shield effects
    /// </summary>
    private static void ProcessShieldEffects(SkillData skillData, Character target)
    {
        if (skillData.shieldDuration <= 0) return;
        
        int shieldAmount = skillData.shieldAmount;
        
        // Add percentage-based shield
        if (skillData.shieldPercent > 0)
        {
            shieldAmount += Mathf.RoundToInt(target.maxHP * skillData.shieldPercent);
        }
        
        if (shieldAmount > 0)
        {
            Debug.Log($"[SKILL EFFECT] Processing shield: {shieldAmount} amount, {skillData.shieldDuration} duration, {skillData.reflectPercent * 100f}% reflect");
            ShieldEffectHandler.ApplyShield(target, shieldAmount, skillData.shieldDuration, skillData.reflectPercent);
        }
    }
    
    #endregion

    #region Private Methods - Status Effects
    
    /// <summary>
    /// Processes status effects (apply and remove)
    /// </summary>
    private static void ProcessStatusEffects(SkillData skillData, Character target)
    {
        if (StatusEffectManager.Instance == null) return;
        
        // Apply status effects
        foreach (var statusName in skillData.statusEffectsToApply)
        {
            StatusEffect effect = CreateStatusEffect(statusName, skillData.statusEffectIntensity, skillData.statusEffectDuration);
            if (effect != null)
            {
                StatusEffectManager.Instance.ApplyEffect(target, effect);
            }
        }
        
        // Remove status effects
        foreach (var statusName in skillData.statusEffectsToRemove)
        {
            System.Type effectType = GetStatusEffectType(statusName);
            if (effectType != null)
            {
                StatusEffectManager.Instance.RemoveEffect(target, effectType);
            }
        }
    }
    
    #endregion

    #region Private Methods - Buff/Debuff Effects
    
    /// <summary>
    /// Processes buff/debuff effects
    /// Note: Stat modifiers are handled by PassiveSkillManager in BattleManager
    /// </summary>
    private static void ProcessBuffDebuffEffects(SkillData skillData, Character target)
    {
        // Note: Stat modifiers are handled by PassiveSkillManager in BattleManager
        // This method is kept for future implementation if needed
        // For now, stat modifiers are applied through the passive skill system
    }
    
    #endregion

    #region Private Methods - Special Effects
    
    /// <summary>
    /// Processes special effects like debuff removal and healing bonuses
    /// </summary>
    private static void ProcessSpecialEffects(SkillData skillData, Character target)
    {
        if (StatusEffectManager.Instance == null) return;
        
        // Remove all debuffs
        if (skillData.removeAllDebuffs)
        {
            Debug.Log($"[SKILL EFFECT] Removing all debuffs from {target.name}");
            RemoveAllDebuffs(target);
        }
        
        // Remove one debuff
        if (skillData.removeOneDebuff)
        {
            Debug.Log($"[SKILL EFFECT] Removing one debuff from {target.name}");
            RemoveOneDebuff(target);
        }
        
        // Apply healing effectiveness bonus
        if (skillData.healingEffectivenessBonus > 0)
        {
            var healBonusEffect = new HealBonusEffect(
                Mathf.RoundToInt(skillData.healingEffectivenessBonus * 100f), 
                skillData.healingEffectivenessDuration
            );
            StatusEffectManager.Instance.ApplyEffect(target, healBonusEffect);
            Debug.Log($"[SKILL EFFECT] Applied {skillData.healingEffectivenessBonus * 100f}% healing effectiveness bonus for {skillData.healingEffectivenessDuration} turns to {target.name}");
        }
    }
    
    #endregion

    #region Private Methods - Recoil Effects
    
    /// <summary>
    /// Processes recoil damage effects
    /// </summary>
    private static void ProcessRecoilEffects(SkillData skillData, Character caster)
    {
        if (skillData.recoilDamagePercent <= 0) return;
        
        int recoilDamage = Mathf.RoundToInt(caster.maxHP * skillData.recoilDamagePercent);
        Debug.Log($"[SKILL EFFECT] Applying recoil damage: {recoilDamage} HP ({skillData.recoilDamagePercent * 100f}% MaxHP) to {caster.name}");
        
        caster.TakeDamage(recoilDamage, caster);
    }
    
    #endregion

    #region Helper Methods
    
    /// <summary>
    /// Gets the scaling stat value from a character
    /// </summary>
    private static int GetScalingValue(Character character, StatType statType)
    {
        switch (statType)
        {
            case StatType.STR: return character.attackPower;
            case StatType.INT: return character.intelligence;
            case StatType.AGI: return character.agility;
            default: return character.attackPower;
        }
    }
    
    /// <summary>
    /// Gets healing effectiveness bonus from target
    /// </summary>
    private static float GetHealingEffectivenessBonus(Character target)
    {
        if (StatusEffectManager.Instance == null) return 0f;
        
        var healBonusEffect = StatusEffectManager.Instance.GetEffect(target, typeof(HealBonusEffect)) as HealBonusEffect;
        return healBonusEffect?.intensity / 100f ?? 0f;
    }
    
    /// <summary>
    /// Gets Pounce effect bonus from caster
    /// </summary>
    private static float GetPounceBonus(Character caster)
    {
        if (StatusEffectManager.Instance == null) return 0f;
        
        var pounceEffect = StatusEffectManager.Instance.GetEffect(caster, typeof(PounceEffect)) as PounceEffect;
        return pounceEffect?.intensity ?? 0f;
    }
    
    /// <summary>
    /// Checks if target is stunned
    /// </summary>
    private static bool IsTargetStunned(Character target)
    {
        if (StatusEffectManager.Instance == null) return false;
        return StatusEffectManager.Instance.HasEffect(target, typeof(StunEffect));
    }
    
    #endregion

    #region Status Effect Creation
    
    /// <summary>
    /// Creates a status effect from string name
    /// </summary>
    private static StatusEffect CreateStatusEffect(string statusName, int intensity, int duration)
    {
        switch (statusName.ToLower())
        {
            case "shield": return new ShieldEffect(intensity, duration);
            case "reflect": return new ReflectEffect(intensity, duration);
            case "stun": return new StunEffect(duration);
            case "burn": return new BurnEffect(intensity, duration);
            case "poison": return new PoisonEffect(intensity, duration);
            case "healbonus": return new HealBonusEffect(intensity, duration);
            case "pounce": return new PounceEffect(intensity, duration);
            default: return null;
        }
    }
    
    /// <summary>
    /// Gets status effect type from string name
    /// </summary>
    private static System.Type GetStatusEffectType(string statusName)
    {
        switch (statusName.ToLower())
        {
            case "shield": return typeof(ShieldEffect);
            case "reflect": return typeof(ReflectEffect);
            case "stun": return typeof(StunEffect);
            case "burn": return typeof(BurnEffect);
            case "poison": return typeof(PoisonEffect);
            case "healbonus": return typeof(HealBonusEffect);
            case "pounce": return typeof(PounceEffect);
            default: return null;
        }
    }
    
    #endregion

    #region Debuff Removal Methods
    
    /// <summary>
    /// Removes all debuffs from target
    /// </summary>
    private static void RemoveAllDebuffs(Character target)
    {
        // Remove all negative status effects
        var debuffTypes = new System.Type[] 
        { 
            typeof(BurnEffect), 
            typeof(PoisonEffect), 
            typeof(StunEffect) 
        };
        
        foreach (var debuffType in debuffTypes)
        {
            StatusEffectManager.Instance.RemoveEffect(target, debuffType);
        }
    }
    
    /// <summary>
    /// Removes one debuff from target
    /// </summary>
    private static void RemoveOneDebuff(Character target)
    {
        // Remove the first negative status effect found
        var debuffTypes = new System.Type[] 
        { 
            typeof(BurnEffect), 
            typeof(PoisonEffect), 
            typeof(StunEffect) 
        };
        
        foreach (var debuffType in debuffTypes)
        {
            if (StatusEffectManager.Instance.HasEffect(target, debuffType))
            {
                StatusEffectManager.Instance.RemoveEffect(target, debuffType);
                break; // Only remove one
            }
        }
    }
    
    #endregion
}