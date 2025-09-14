using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Character system for managing player stats and health
/// 
/// USAGE:
/// - Manages base and current character stats (HP, Attack, Defense)
/// - Handles damage calculation and health management
/// - Updates UI elements (HP bars, stat displays)
/// - Integrates with equipment system for stat bonuses
/// 
/// SETUP:
/// 1. Attach to player GameObject
/// 2. Set base stats in inspector
/// 3. Assign UI elements (HP slider, stat displays)
/// 4. System automatically manages stat calculations
/// </summary>
public class Character : MonoBehaviour
{
    [Header("Base Stats")]
    public int baseMaxHP = 100;
    public int baseAttackPower = 20;
    public int baseDefense = 0;
    
    [Header("Current Stats")]
    public int maxHP;
    public int currentHP;
    public int attackPower;
    public int defense;

    [Header("UI")]
    public Slider hpSlider;         // Optional: assign if you want a slider
    public Image hpFillImage;       // Optional: assign if using Image (filled)

    private void Awake()
    {
        ResetToBaseStats();
        currentHP = maxHP;
        UpdateHPUI();
    }
    
    public void ResetToBaseStats()
    {
        maxHP = baseMaxHP;
        attackPower = baseAttackPower;
        defense = baseDefense;
    }
    
    public void AddGearBonus(GearItem gear)
    {
        if (gear == null) return;
        
        maxHP += gear.hpBonus;
        attackPower += gear.attackBonus;
        defense += gear.defenseBonus;
        
        // Ensure current HP doesn't exceed new max HP
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
        
        UpdateHPUI();
        Debug.Log($"[EQUIP] Applied gear bonus from {gear.itemName}: +{gear.hpBonus} HP, +{gear.attackBonus} ATK, +{gear.defenseBonus} DEF");
    }
    
    public void RemoveGearBonus(GearItem gear)
    {
        if (gear == null) return;
        
        maxHP -= gear.hpBonus;
        attackPower -= gear.attackBonus;
        defense -= gear.defenseBonus;
        
        // Ensure stats don't go below base values
        maxHP = Mathf.Max(maxHP, baseMaxHP);
        attackPower = Mathf.Max(attackPower, baseAttackPower);
        defense = Mathf.Max(defense, baseDefense);
        
        // Ensure current HP doesn't exceed new max HP
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
        
        UpdateHPUI();
        Debug.Log($"[EQUIP] Removed gear bonus from {gear.itemName}: -{gear.hpBonus} HP, -{gear.attackBonus} ATK, -{gear.defenseBonus} DEF");
    }

    public void Attack(Character target)
    {
        if (target == null) return;
        target.TakeDamage(attackPower);
    }

    public void TakeDamage(int damage)
    {
        // Calculate damage after defense
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHP -= actualDamage;
        if (currentHP < 0) currentHP = 0;

        UpdateHPUI();
        
        Debug.Log($"[BATTLE] {gameObject.name} took {actualDamage} damage (defense reduced {damage} to {actualDamage})");

        if (currentHP <= 0)
        {
            Die();
        }
    }


    void UpdateHPUI()
    {
        float t = (float)currentHP / maxHP;

        if (hpSlider != null)
            hpSlider.value = t;

        if (hpFillImage != null)
            hpFillImage.fillAmount = t;
    }

    // Event for when character dies
    public System.Action<Character> OnDeath;
    
    void Die()
    {
        Debug.Log($"[BATTLE] {gameObject.name} has been defeated!");
        
        // Trigger death event before destroying
        OnDeath?.Invoke(this);
        
        Destroy(gameObject);
    }
}
