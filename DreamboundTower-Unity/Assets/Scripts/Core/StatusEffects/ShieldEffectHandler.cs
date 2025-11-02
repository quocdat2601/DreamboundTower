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
        
        // Apply reflect effect (only if reflectPercent > 0)
        // Reflect lasts 2 turns and is only active when character has shield (any shield)
        if (reflectPercent > 0f)
        {
            // Create reflect effect that lasts 2 turns
            // Reflect will only be active while character has any shield
            var reflectEffect = new ReflectEffect(Mathf.RoundToInt(reflectPercent * 100f), 2, shieldAmount);
            StatusEffectManager.Instance.ApplyEffect(target, reflectEffect);
            Debug.Log($"[SHIELD] Applied {reflectPercent * 100f}% reflect for 2 turns to {target.name} (active while shield exists)");
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
    /// Gets the reflect percentage for a character (only active when character has any shield)
    /// </summary>
    public static float GetReflectPercent(Character target)
    {
        if (target == null || StatusEffectManager.Instance == null) return 0f;
        
        var reflectEffect = StatusEffectManager.Instance.GetEffect(target, typeof(ReflectEffect)) as ReflectEffect;
        // Reflect is only active when character has any shield
        if (reflectEffect != null && HasShield(target))
        {
            return reflectEffect.intensity / 100f;
        }
        return 0f;
    }
    
    /// <summary>
    /// Reduces shield amount when damage is absorbed
    /// Reflect effect remains active while shield exists (checked dynamically)
    /// </summary>
    public static void ReduceShieldAmount(Character target, int damageAbsorbed)
    {
        if (target == null || StatusEffectManager.Instance == null) return;
        
        var shieldEffect = StatusEffectManager.Instance.GetEffect(target, typeof(ShieldEffect)) as ShieldEffect;
        if (shieldEffect != null)
        {
            int shieldBefore = shieldEffect.intensity;
            shieldEffect.intensity = Mathf.Max(0, shieldEffect.intensity - damageAbsorbed);
            
            // If total shield is depleted, remove shield effect
            // Note: Reflect effect will remain but will be inactive until shield is restored
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
