// File: Character.cs
using UnityEngine;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    [Header("Base Stats")]
    public int baseMaxHP = 100;
    public int baseAttackPower = 20; // Tương ứng với STR
    public int baseDefense = 0;      // Tương ứng với DEF
    public int baseMana = 50;        // <-- THÊM MỚI
    public int baseIntelligence = 10;// <-- THÊM MỚI (INT)
    public int baseAgility = 10;     // <-- THÊM MỚI (AGI)

    [Header("Current Stats")]
    public int maxHP;
    public int currentHP;
    public int attackPower;
    public int defense;
    public int mana;                 // <-- THÊM MỚI
    public int currentMana;                 // <-- THÊM MỚI
    public int intelligence;         // <-- THÊM MỚI
    public int agility;              // <-- THÊM MỚI

    // ... (các biến UI giữ nguyên) ...
    [Header("UI")]
    public Slider hpSlider;
    public Image hpFillImage;
    public Slider manaSlider;
    public Image manaFillImage;

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
        mana = baseMana;                 // <-- THÊM MỚI
        intelligence = baseIntelligence; // <-- THÊM MỚI
        agility = baseAgility;           // <-- THÊM MỚI
    }

    // ... (Các hàm còn lại của bạn giữ nguyên) ...
    public void AddGearBonus(GearItem gear)
    {
        if (gear == null) return;

        maxHP += gear.hpBonus;
        attackPower += gear.attackBonus;
        defense += gear.defenseBonus;

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

        maxHP = Mathf.Max(maxHP, baseMaxHP);
        attackPower = Mathf.Max(attackPower, baseAttackPower);
        defense = Mathf.Max(defense, baseDefense);

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
    public void UpdateHPUI()
    {
        // ✅ THÊM LẠI KHỐI CODE NÀY ĐỂ GIAO TIẾP VỚI UI TOÀN CỤC
        // Thông báo cho GameManager để cập nhật giao diện "bất tử"
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerHealthUI(currentHP, maxHP);
        }

        // Phần code cũ của bạn vẫn có thể hữu ích để cập nhật thanh máu trên đầu quái
        // (nếu bạn có thiết lập đó)
        float t = (float)currentHP / maxHP;
        if (hpSlider != null)
            hpSlider.value = t;
        if (hpFillImage != null)
            hpFillImage.fillAmount = t;
    }

    public System.Action<Character> OnDeath;

    void Die()
    {
        Debug.Log($"[BATTLE] {gameObject.name} has been defeated!");

        OnDeath?.Invoke(this);

        Destroy(gameObject);
    }
}