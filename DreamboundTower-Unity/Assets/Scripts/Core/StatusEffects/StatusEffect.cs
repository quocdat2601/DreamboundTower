using UnityEngine;

namespace StatusEffects
{
    /// <summary>
    /// When the status effect should trigger
    /// </summary>
    public enum EffectTiming
    {
        None,
        StartOfTurn,
        EndOfTurn,
        OnDamage
    }

    /// <summary>
    /// Types of status effects that can be applied
    /// </summary>
    public enum StatusEffectType
    {
        Shield,
        Reflect,
        Stun,
        Burn,
        Poison,
        Bleed,
        HealBonus,
        Pounce
    }

    /// <summary>
    /// Base class for all status effects
    /// </summary>
    public abstract class StatusEffect
{
    public string effectName;
    public int duration;
    public int intensity;
    public EffectTiming timing;
    public bool isStackable;
    public bool isNegative;
    public bool isEventBased; // If true, duration decrements per battle node; if false, per turn
    
    public StatusEffect(string name, int duration, int intensity, EffectTiming timing, bool stackable = false, bool negative = false, bool eventBased = false)
    {
        this.effectName = name;
        this.duration = duration;
        this.intensity = intensity;
        this.timing = timing;
        this.isStackable = stackable;
        this.isNegative = negative;
        this.isEventBased = eventBased;
    }
    
    /// <summary>
    /// Called when the effect is first applied
    /// </summary>
    public virtual void OnApply(Character target) { }
    
    /// <summary>
    /// Called each turn when the effect ticks
    /// </summary>
    public virtual void OnTick(Character target) { }
    
    /// <summary>
    /// Called when the effect is removed
    /// </summary>
    public virtual void OnRemove(Character target) { }
    
    /// <summary>
    /// Refreshes the effect with new duration/intensity
    /// </summary>
    public virtual void Refresh(int newDuration, int newIntensity)
    {
        duration = newDuration;
        intensity = newIntensity;
    }
}

/// <summary>
/// Shield effect that absorbs damage
/// </summary>
public class ShieldEffect : StatusEffect
{
    public ShieldEffect(int shieldAmount, int duration) : base("Shield", duration, shieldAmount, EffectTiming.EndOfTurn, false, false)
    {
    }
    
    public override void OnApply(Character target) { }
    public override void OnTick(Character target) { }
    public override void OnRemove(Character target)
    {
        // When shield expires, reflect will remain but become inactive until shield is restored
        // Reflect manages its own 2-turn duration independently
    }
}

/// <summary>
/// Reflect effect that reflects damage back to attacker
/// </summary>
public class ReflectEffect : StatusEffect
{
    // Track the shield amount that has reflect (e.g., from Radiant Shield skill)
    // Reflect only works while this shield amount > 0
    public int radiantShieldAmount = 0;
    
    public ReflectEffect(int reflectPercent, int duration, int radiantShieldAmount = 0) : base("Reflect", duration, reflectPercent, EffectTiming.EndOfTurn, false, false)
    {
        this.radiantShieldAmount = radiantShieldAmount;
    }
    
    public override void OnApply(Character target) { }
    public override void OnTick(Character target) { }
    public override void OnRemove(Character target) { }
}

/// <summary>
/// Stun effect that prevents actions
/// </summary>
public class StunEffect : StatusEffect
{
    public StunEffect(int duration) : base("Stun", duration, 0, EffectTiming.StartOfTurn, false, true)
    {
    }
    
    public override void OnApply(Character target) { }
    public override void OnTick(Character target) { }
}

/// <summary>
/// Burn effect that deals damage over time
/// Scales with attacker's Intelligence (base + bonuses) - calculated once when applied
/// </summary>
public class BurnEffect : StatusEffect
{
    public BurnEffect(int baseDamage, int duration, Character attacker = null) : base("Burn", duration, baseDamage, EffectTiming.EndOfTurn, false, true)
    {
        // Calculate damage once when effect is created (scale with INT at that moment)
        // Damage is "locked in" and does NOT recalculate each turn
        if (attacker != null)
        {
            int intScaling = Mathf.RoundToInt(attacker.intelligence * 0.15f); // 15% of full INT (base + bonuses)
            this.intensity = baseDamage + intScaling;
        }
        else
        {
            this.intensity = baseDamage;
        }
    }
    
    public override void OnTick(Character target)
    {
        if (target != null)
        {
            // Ensure intensity is at least 1 for visible damage
            int actualDamage = Mathf.Max(1, intensity);
            Debug.Log($"[BURN] Dealing {actualDamage} burn damage to {target.name} (intensity: {intensity})");
            
            // Only apply damage if intensity > 0
            if (intensity > 0)
            {
                // Show visual damage number for burn effect (orange/fire-like color)
                if (CombatEffectManager.Instance != null)
                {
                    CombatEffectManager.Instance.ShowStatusEffectDamage(target, actualDamage, new Color(1f, 0.5f, 0f)); // Orange
                }
                // Status effects like burn cannot be dodged (bypassDodge = true)
                // Burn is magical fire, so use Magic damage type
                // Suppress damage number display since we already show it above with custom color
                target.TakeDamage(actualDamage, null, DamageType.Magic, false, bypassDodge: true, suppressDamageNumber: true);
            }
            else
            {
                Debug.LogWarning($"[BURN] Burn effect on {target.name} has intensity 0, no damage dealt");
            }
        }
    }
}

/// <summary>
/// Poison effect that deals damage over time
/// Scales with target's Max HP (2.5% of max HP) - calculated once when applied
/// </summary>
public class PoisonEffect : StatusEffect
{
    public PoisonEffect(int baseDamage, int duration, bool eventBased = false, Character target = null) : base("Poison", duration, baseDamage, EffectTiming.EndOfTurn, false, true, eventBased)
    {
        // Calculate damage once when effect is created (scale with target's max HP at that moment)
        // Damage is "locked in" and does NOT recalculate each turn
        if (target != null)
        {
            // Scale damage with target's max HP (2.5% per turn)
            int poisonDamage = Mathf.Max(1, Mathf.RoundToInt(target.maxHP * 0.025f));
            this.intensity = poisonDamage;
        }
        else
        {
            // Fallback to base damage if target is not available
            this.intensity = baseDamage;
        }
    }
    
    public override void OnTick(Character target)
    {
        if (target != null)
        {
            Debug.Log($"[POISON] Dealing {intensity} poison damage to {target.name}");
            // Show visual damage number for poison effect (dark green)
            if (CombatEffectManager.Instance != null)
            {
                CombatEffectManager.Instance.ShowStatusEffectDamage(target, intensity, new Color(0f, 0.5f, 0f)); // Dark green
            }
            // Status effects like poison cannot be dodged (bypassDodge = true)
            // Suppress damage number display since we already show it above with custom color
            target.TakeDamage(intensity, null, DamageType.Physical, false, bypassDodge: true, suppressDamageNumber: true);
        }
    }
}

/// <summary>
/// Bleed effect that deals damage over time
/// Scales with actual physical damage dealt (12% of physical damage)
/// </summary>
public class BleedEffect : StatusEffect
{
    public BleedEffect(int damagePerTurn, int duration, bool eventBased = false) : base("Bleed", duration, damagePerTurn, EffectTiming.EndOfTurn, false, true, eventBased)
    {
        // Intensity is set to the scaled damage value when effect is created
    }
    
    public override void OnTick(Character target)
    {
        if (target != null)
        {
            Debug.Log($"[BLEED] Dealing {intensity} bleed damage to {target.name}");
            // Show visual damage number for bleed effect (using red color, similar to burn but darker)
            if (CombatEffectManager.Instance != null)
            {
                CombatEffectManager.Instance.ShowStatusEffectDamage(target, intensity, new Color(0.8f, 0f, 0f)); // Dark red
            }
            // Status effects like bleed cannot be dodged (bypassDodge = true)
            // Suppress damage number display since we already show it above with custom color
            target.TakeDamage(intensity, null, DamageType.Physical, false, bypassDodge: true, suppressDamageNumber: true);
        }
    }
}

/// <summary>
/// Healing bonus effect that increases healing effectiveness
/// </summary>
public class HealBonusEffect : StatusEffect
{
    public HealBonusEffect(int bonusPercent, int duration, bool eventBased = false) : base("Heal Bonus", duration, bonusPercent, EffectTiming.EndOfTurn, false, false, eventBased)
    {
    }
    
    public override void OnApply(Character target) { }
    public override void OnTick(Character target) { }
    public override void OnRemove(Character target) { }
}

/// <summary>
/// Pounce effect that enhances the next attack
/// </summary>
public class PounceEffect : StatusEffect
{
    public PounceEffect(int damageBonus, int duration) : base("Pounce", duration, damageBonus, EffectTiming.EndOfTurn, false, false)
    {
    }
    
    public override void OnApply(Character target) { }
    public override void OnTick(Character target) { }
    public override void OnRemove(Character target) { }
}

    /// <summary>
    /// Generic stat modifier effect that applies additive changes to core stats
    /// </summary>
    public class StatModifierEffect : StatusEffect
    {
        public int deltaSTR;
        public int deltaDEF;
        public int deltaINT;
        public int deltaAGI;
        public int deltaMANA;

        public StatModifierEffect(string name, int duration, int dStr = 0, int dDef = 0, int dInt = 0, int dAgi = 0, int dMana = 0, bool negative = false, bool eventBased = false)
            : base(name, duration, 0, EffectTiming.EndOfTurn, false, negative, eventBased)
        {
            deltaSTR = dStr;
            deltaDEF = dDef;
            deltaINT = dInt;
            deltaAGI = dAgi;
            deltaMANA = dMana;
        }

        public override void OnApply(Character target)
        {
            if (target == null) return;
            target.attackPower += deltaSTR;
            target.defense += deltaDEF;
            target.intelligence += deltaINT;
            target.agility += deltaAGI;
            target.mana += deltaMANA;
            target.UpdateDerivedStats(); // Update dodge chance if AGI changed
        }

        public override void OnRemove(Character target)
        {
            if (target == null) return;
            target.attackPower -= deltaSTR;
            target.defense -= deltaDEF;
            target.intelligence -= deltaINT;
            target.agility -= deltaAGI;
            target.mana -= deltaMANA;
            target.UpdateDerivedStats(); // Update dodge chance if AGI changed
            // Clamp not below base handled elsewhere, this is temporary effect
        }
    }
}
