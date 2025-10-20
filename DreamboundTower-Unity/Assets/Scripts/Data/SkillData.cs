// File: SkillData.cs
using Assets.Scripts.Data;
using UnityEngine;
using System.Collections.Generic;
using StatusEffects;

/// <summary>
/// ScriptableObject that defines the properties and effects of an active skill
/// </summary>
[CreateAssetMenu(menuName = "DBT/Active Skill")]
public class SkillData : BaseSkillSO
{
    #region Basic Properties
    
    [Header("Basic Properties")]
    [Tooltip("Type of resource consumed by this skill")]
    public ResourceType resource = ResourceType.Mana;
    
    [Tooltip("Amount of resource consumed when using this skill")]
    public int cost = 0;
    
    [Tooltip("Cooldown turns before skill can be used again")]
    public int cooldown = 0;
    
    [Tooltip("Target type for this skill")]
    public TargetType target = TargetType.SingleEnemy;

    #endregion

    #region Visuals & Audio
    
    [Header("Visuals & Audio")]
    [Tooltip("Prefab containing visual effects (VFX) that will be created when using skill")]
    public GameObject vfxPrefab;

    #endregion

    #region Damage Effects
    
    [Header("Damage Effects")]
    [Tooltip("Base damage dealt by this skill")]
    public int baseDamage = 0;
    
    [Tooltip("Stat used for damage scaling")]
    public StatType scalingStat = StatType.STR;
    
    [Tooltip("Percentage multiplier for stat scaling")]
    [Range(0f, 5f)]
    public float scalingPercent = 1.0f;
    
    [Tooltip("Whether this skill deals magic damage")]
    public bool isMagicDamage = false;
    
    [Tooltip("Number of hits for multi-hit skills")]
    [Range(1, 10)]
    public int hitCount = 1;

    #endregion

    #region Healing Effects
    
    [Header("Healing Effects")]
    [Tooltip("Fixed amount of healing")]
    public int healAmount = 0;
    
    [Tooltip("Percentage of max HP to heal")]
    [Range(0f, 1f)]
    public float healPercent = 0f;
    
    [Tooltip("Whether this skill heals based on damage dealt")]
    public bool lifesteal = false;
    
    [Tooltip("Percentage of damage dealt that heals the caster")]
    [Range(0f, 1f)]
    public float lifestealPercent = 0f;

    #endregion

    #region Shield Effects
    
    [Header("Shield Effects")]
    [Tooltip("Fixed amount of shield")]
    public int shieldAmount = 0;
    
    [Tooltip("Percentage of max HP as shield")]
    [Range(0f, 1f)]
    public float shieldPercent = 0f;
    
    [Tooltip("Duration of shield effect in turns")]
    public int shieldDuration = 0;
    
    [Tooltip("Percentage of damage reflected back to attacker")]
    [Range(0f, 1f)]
    public float reflectPercent = 0f;

    #endregion

    #region Status Effects
    
    [Header("Status Effects")]
    [Tooltip("List of status effects to apply")]
    public List<string> statusEffectsToApply = new List<string>();
    
    [Tooltip("List of status effects to remove")]
    public List<string> statusEffectsToRemove = new List<string>();
    
    [Tooltip("Duration of applied status effects")]
    public int statusEffectDuration = 0;
    
    [Tooltip("Intensity of applied status effects")]
    public int statusEffectIntensity = 0;

    #endregion

    #region Buff/Debuff Effects
    
    [Header("Buff/Debuff Effects")]
    [Tooltip("List of stat modifiers to apply")]
    public List<StatModifierSO> statModifiers = new List<StatModifierSO>();
    
    [Tooltip("Duration of applied buffs")]
    public int buffDuration = 0;

    #endregion

    #region Special Effects
    
    [Header("Special Effects")]
    [Tooltip("Whether to remove all debuffs from target")]
    public bool removeAllDebuffs = false;
    
    [Tooltip("Whether to remove one debuff from target")]
    public bool removeOneDebuff = false;
    
    [Tooltip("Percentage bonus to healing effectiveness")]
    [Range(0f, 1f)]
    public float healingEffectivenessBonus = 0f;
    
    [Tooltip("Duration of healing effectiveness bonus")]
    public int healingEffectivenessDuration = 0;

    #endregion

    #region Recoil Effects
    
    [Header("Recoil Effects")]
    [Tooltip("Percentage of caster's MaxHP as recoil damage")]
    [Range(0f, 1f)]
    public float recoilDamagePercent = 0f;

    #endregion

    #region Chance Effects
    
    [Header("Chance Effects")]
    [Tooltip("Chance to apply stun effect")]
    [Range(0f, 1f)]
    public float stunChance = 0f;
    
    [Tooltip("Bonus damage multiplier against stunned targets")]
    [Range(0f, 1f)]
    public float bonusDamageToStunned = 0f;

    #endregion
}