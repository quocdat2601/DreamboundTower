using UnityEngine;
using Assets.Scripts.Data;

/// <summary>
/// Handles conditional passive skill effects that depend on game state
/// </summary>
public class ConditionalPassiveManager : MonoBehaviour
{
    [Header("Conditional Passive Bonuses")]
    [Tooltip("Physical damage bonus when HP is below 50%")]
    public float lowHpPhysicalDamageBonus = 0f; // For Bloodlust Surge
    [Tooltip("Damage reduction when HP is at or below 35%")]
    public float lowHpDamageReduction = 0f; // For Divine Resilience
    [Tooltip("Lifesteal when HP is at or below 35%")]
    public float lowHpLifesteal = 0f; // For Divine Resilience
    [Tooltip("Damage reduction only against non-boss enemies")]
    public float nonBossDamageReduction = 0f; // For Iron Will
    [Tooltip("Physical damage bonus against low DEF enemies")]
    public float lowDefDamageBonus = 0f; // For Evasive Instinct
    [Tooltip("Mana regeneration per turn")]
    public float manaRegenPerTurn = 0f; // For Divine Resonance

    private Character character;

    void Awake()
    {
        character = GetComponent<Character>();
    }

    /// <summary>
    /// Calculates physical damage with conditional bonuses against a specific target
    /// </summary>
    public int CalculatePhysicalDamage(int baseDamage, Character target)
    {
        float bonusMultiplier = 1f;
        
        // Apply low HP physical damage bonus (Bloodlust Surge)
        if (GetHpPercentage() < 0.5f && lowHpPhysicalDamageBonus > 0f)
        {
            bonusMultiplier += lowHpPhysicalDamageBonus;
            Debug.Log($"[CONDITIONAL PASSIVE] Low HP physical damage bonus applied: +{lowHpPhysicalDamageBonus * 100f}%");
        }
        
        // Apply low DEF damage bonus (Evasive Instinct)
        if (target != null && lowDefDamageBonus > 0f && target.defense < 10) // Low DEF threshold
        {
            bonusMultiplier += lowDefDamageBonus;
            Debug.Log($"[CONDITIONAL PASSIVE] Low DEF damage bonus applied: +{lowDefDamageBonus * 100f}% (target DEF: {target.defense})");
        }
        
        return Mathf.RoundToInt(baseDamage * bonusMultiplier);
    }

    /// <summary>
    /// Applies conditional damage reduction to incoming damage
    /// </summary>
    public int ApplyConditionalDamageReduction(int damage, Character attacker)
    {
        int reducedDamage = damage;
        
        // Apply low HP damage reduction (Divine Resilience)
        if (GetHpPercentage() <= 0.35f && lowHpDamageReduction > 0f)
        {
            int lowHpReduction = Mathf.RoundToInt(damage * lowHpDamageReduction);
            reducedDamage -= lowHpReduction;
            Debug.Log($"[CONDITIONAL PASSIVE] Low HP damage reduction: {damage} -> {reducedDamage} (-{lowHpReduction})");
        }
        
        // Apply non-boss damage reduction (Iron Will)
        if (attacker != null && !attacker.IsBoss() && nonBossDamageReduction > 0f)
        {
            int nonBossReduction = Mathf.RoundToInt(damage * nonBossDamageReduction);
            reducedDamage -= nonBossReduction;
            Debug.Log($"[CONDITIONAL PASSIVE] Non-boss damage reduction: {damage} -> {reducedDamage} (-{nonBossReduction})");
        }
        
        return Mathf.Max(1, reducedDamage); // Minimum 1 damage
    }

    /// <summary>
    /// Applies conditional lifesteal
    /// </summary>
    public int ApplyConditionalLifesteal(int damage)
    {
        if (GetHpPercentage() <= 0.35f && lowHpLifesteal > 0f)
        {
            int healAmount = Mathf.RoundToInt(damage * lowHpLifesteal);
            if (healAmount > 0)
            {
                character.RestoreHealth(healAmount);
                Debug.Log($"[CONDITIONAL PASSIVE] Low HP Lifesteal: {healAmount} HP restored from {damage} damage");
                
                // Show green healing number for conditional lifesteal
                if (CombatEffectManager.Instance != null)
                {
                    Vector3 uiPosition = CombatEffectManager.Instance.GetCharacterUIPosition(character);
                    CombatEffectManager.Instance.ShowHealingNumber(uiPosition, healAmount);
                }
            }
            return healAmount;
        }
        return 0;
    }

    /// <summary>
    /// Regenerates mana per turn (for Divine Resonance)
    /// </summary>
    public void RegenerateManaPerTurn()
    {
        if (manaRegenPerTurn > 0f)
        {
            int regenAmount = Mathf.RoundToInt(character.mana * manaRegenPerTurn);
            character.RegenerateMana(regenAmount);
            Debug.Log($"[CONDITIONAL PASSIVE] Mana regen per turn: {regenAmount} mana restored");
        }
    }

    /// <summary>
    /// Gets current HP as a percentage (0.0 to 1.0)
    /// </summary>
    private float GetHpPercentage()
    {
        if (character.maxHP <= 0) return 0f;
        return (float)character.currentHP / character.maxHP;
    }

    /// <summary>
    /// Resets all conditional passive bonuses
    /// </summary>
    public void ResetAllConditionalBonuses()
    {
        lowHpPhysicalDamageBonus = 0f;
        lowHpDamageReduction = 0f;
        lowHpLifesteal = 0f;
        nonBossDamageReduction = 0f;
        lowDefDamageBonus = 0f;
        manaRegenPerTurn = 0f;
    }
}
