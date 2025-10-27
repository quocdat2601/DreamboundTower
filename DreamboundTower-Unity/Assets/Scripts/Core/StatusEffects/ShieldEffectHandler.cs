using UnityEngine;
using StatusEffects;

/// <summary>
/// Handles shield-specific logic and interactions
/// This keeps shield logic separate from the Character class
/// </summary>
public static class ShieldEffectHandler
{
    /// <summary>
    /// Applies shield and reflect effects to a character
    /// </summary>
    public static void ApplyShield(Character target, int shieldAmount, int duration, float reflectPercent = 0f)
    {
        if (target == null || StatusEffectManager.Instance == null) 
        {
            Debug.LogWarning("[SHIELD] Cannot apply shield - target or StatusEffectManager is null");
            return;
        }
        
        // Apply shield effect
        if (shieldAmount > 0)
        {
            var shieldEffect = new ShieldEffect(shieldAmount, duration);
            StatusEffectManager.Instance.ApplyEffect(target, shieldEffect);
            Debug.Log($"[SHIELD] Applied {shieldAmount} shield for {duration} turns to {target.name}");
        }
        
        // Apply reflect effect
        if (reflectPercent > 0f)
        {
            var reflectEffect = new ReflectEffect(Mathf.RoundToInt(reflectPercent * 100f), duration);
            StatusEffectManager.Instance.ApplyEffect(target, reflectEffect);
            Debug.Log($"[SHIELD] Applied {reflectPercent * 100f}% reflect for {duration} turns to {target.name}");
        }
    }
    
    /// <summary>
    /// Gets the current shield amount for a character
    /// </summary>
    public static int GetShieldAmount(Character target)
    {
        if (target == null || StatusEffectManager.Instance == null) return 0;
        
        var shieldEffect = StatusEffectManager.Instance.GetEffect(target, typeof(ShieldEffect)) as ShieldEffect;
        return shieldEffect?.intensity ?? 0;
    }
    
    /// <summary>
    /// Gets the remaining shield turns for a character
    /// </summary>
    public static int GetShieldTurns(Character target)
    {
        if (target == null || StatusEffectManager.Instance == null) return 0;
        
        var shieldEffect = StatusEffectManager.Instance.GetEffect(target, typeof(ShieldEffect)) as ShieldEffect;
        return shieldEffect?.duration ?? 0;
    }
    
    /// <summary>
    /// Gets the reflect percentage for a character
    /// </summary>
    public static float GetReflectPercent(Character target)
    {
        if (target == null || StatusEffectManager.Instance == null) return 0f;
        
        var reflectEffect = StatusEffectManager.Instance.GetEffect(target, typeof(ReflectEffect)) as ReflectEffect;
        return (reflectEffect?.intensity ?? 0) / 100f;
    }
    
    /// <summary>
    /// Reduces shield amount when damage is absorbed
    /// </summary>
    public static void ReduceShieldAmount(Character target, int damageAbsorbed)
    {
        if (target == null || StatusEffectManager.Instance == null) return;
        
        var shieldEffect = StatusEffectManager.Instance.GetEffect(target, typeof(ShieldEffect)) as ShieldEffect;
        if (shieldEffect != null)
        {
            shieldEffect.intensity = Mathf.Max(0, shieldEffect.intensity - damageAbsorbed);
            if (shieldEffect.intensity <= 0)
            {
                StatusEffectManager.Instance.RemoveEffect(target, typeof(ShieldEffect));
            }
        }
    }
    
    /// <summary>
    /// Checks if a character has any shield
    /// </summary>
    public static bool HasShield(Character target)
    {
        return GetShieldAmount(target) > 0;
    }
    
    /// <summary>
    /// Checks if a character has reflect
    /// </summary>
    public static bool HasReflect(Character target)
    {
        return GetReflectPercent(target) > 0f;
    }
}
