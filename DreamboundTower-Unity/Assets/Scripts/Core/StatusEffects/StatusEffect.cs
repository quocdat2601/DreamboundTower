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
    public override void OnRemove(Character target) { }
}

/// <summary>
/// Reflect effect that reflects damage back to attacker
/// </summary>
public class ReflectEffect : StatusEffect
{
    public ReflectEffect(int reflectPercent, int duration) : base("Reflect", duration, reflectPercent, EffectTiming.EndOfTurn, false, false)
    {
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
/// </summary>
public class BurnEffect : StatusEffect
{
    public BurnEffect(int damagePerTurn, int duration) : base("Burn", duration, damagePerTurn, EffectTiming.EndOfTurn, false, true)
    {
    }
    
    public override void OnTick(Character target)
    {
        if (target != null)
        {
            Debug.Log($"[BURN] Dealing {intensity} burn damage to {target.name}");
            // Show visual damage number for burn effect
            if (CombatEffectManager.Instance != null)
            {
                CombatEffectManager.Instance.ShowStatusEffectDamage(target, intensity, Color.red);
            }
            // Status effects like burn cannot be dodged (bypassDodge = true)
            target.TakeDamage(intensity, null, DamageType.Physical, false, bypassDodge: true);
        }
    }
}

/// <summary>
/// Poison effect that deals damage over time
/// </summary>
public class PoisonEffect : StatusEffect
{
    public PoisonEffect(int damagePerTurn, int duration, bool eventBased = false) : base("Poison", duration, damagePerTurn, EffectTiming.EndOfTurn, false, true, eventBased)
    {
    }
    
    public override void OnTick(Character target)
    {
        if (target != null)
        {
            // Show visual damage number for poison effect
            if (CombatEffectManager.Instance != null)
            {
                CombatEffectManager.Instance.ShowStatusEffectDamage(target, intensity, Color.magenta);
            }
            // Status effects like poison cannot be dodged (bypassDodge = true)
            target.TakeDamage(intensity, null, DamageType.Physical, false, bypassDodge: true);
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
