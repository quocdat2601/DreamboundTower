// File: Character.cs
using UnityEngine;
using UnityEngine.UI;
using System;
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
    // Nó sẽ gửi đi thông tin về kẻ tấn công.
    public event Action<Character> OnDamaged;
    // Sự kiện này sẽ gửi đi 2 giá trị: currentHP và maxHP
    public event Action<int, int> OnHealthChanged;
    // Sự kiện này đã có, dùng cho Resurrect
    public System.Action<Character> OnDeath;

    // ... (các biến UI giữ nguyên) ...
    [Header("UI")]
    public Slider hpSlider;
    public Image hpFillImage;
    public Slider manaSlider;
    public Image manaFillImage;

    private void Awake()
    {
        //ResetToBaseStats();
        //currentHP = maxHP;
        //UpdateHPUI();
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
        // Truyền "this" với tư cách là kẻ tấn công (attacker)
        target.TakeDamage(attackPower, this);
    }

    // ✅ SỬA LẠI: Hàm TakeDamage giờ nhận thêm tham số "attacker"
    public void TakeDamage(int damage, Character attacker)
    {
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHP -= actualDamage;
        if (currentHP < 0) currentHP = 0;

        UpdateHPUI(); // Phát tín hiệu cho UI

        // Phát tín hiệu cho các gimmick như CounterAttack
        OnDamaged?.Invoke(attacker);

        Debug.Log($"[BATTLE] {gameObject.name} took {actualDamage} damage from {(attacker != null ? attacker.name : "an unknown source")}");

        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void UpdateHPUI()
    {
        // NHIỆM VỤ 1: PHÁT SÓNG TÍN HIỆU RA BÊN NGOÀI
        // Bất kỳ ai đăng ký lắng nghe sẽ nhận được tín hiệu này.
        OnHealthChanged?.Invoke(currentHP, maxHP);

        // NHIỆM VỤ 2 (TÙY CHỌN): CẬP NHẬT UI CỤC BỘ
        // Nếu bạn có thanh máu trên đầu quái, nó vẫn sẽ hoạt động.
        float t = (float)currentHP / maxHP;
        if (hpSlider != null)
            hpSlider.value = t;
        if (hpFillImage != null)
            hpFillImage.fillAmount = t;
    }

    // Tương tự, nếu bạn có hàm Heal(int amount), cũng hãy gọi event ở cuối
    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        // ✅ PHÁT SÓNG TÍN HIỆU
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }


    void Die()
    {
        Debug.Log($"[BATTLE] {gameObject.name} has been defeated!");

        OnDeath?.Invoke(this);
        // Chỉ phát tín hiệu rằng nó đã chết.
        // BattleManager sẽ lắng nghe tín hiệu này và quyết định có nên Destroy() hay không,
        // sau khi các gimmick như Resurrect đã có cơ hội hành động.
        //Destroy(gameObject);
    }

    /// <summary>
    /// Hồi máu cho nhân vật một lượng bằng % máu tối đa.
    /// </summary>
    /// <param name="percentage">Tỷ lệ phần trăm để hồi, ví dụ 0.5f cho 50%</param>
    public void HealPercentage(float percentage)
    {
        int healAmount = Mathf.FloorToInt(maxHP * percentage);
        currentHP += healAmount;
        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }

        Debug.Log($"[REST] Healed for {healAmount} HP ({percentage * 100}%). New HP: {currentHP}/{maxHP}");

        // Phát sóng tín hiệu để UI tự cập nhật
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    /// <summary>
    /// Hồi đầy lại Mana của nhân vật.
    /// </summary>
    public void RestoreFullMana()
    {
        currentMana = mana; // 'mana' là maxMana
        Debug.Log($"[REST] Mana restored to full. New Mana: {currentMana}/{mana}");

        // Tương tự, nếu bạn có event cho Mana, hãy gọi nó ở đây
        // OnManaChanged?.Invoke(currentMana, mana);
    }

    /// <summary>
    /// Xóa bỏ tất cả các hiệu ứng trạng thái xấu đang có trên nhân vật.
    /// </summary>
    public void RemoveAllNegativeStatusEffects()
    {
        // PHẦN NÀY LÀ GIẢ ĐỊNH CHO TƯƠNG LAI
        // Khi bạn có hệ thống status effect (ví dụ: một List<StatusEffect> activeEffects),
        // bạn sẽ viết logic ở đây để xóa các hiệu ứng có hại.
        // Ví dụ:
        // activeEffects.RemoveAll(effect => effect.isNegative == true);

        Debug.Log("[REST] All negative status effects have been purified.");
        // Gọi event để cập nhật UI status effect nếu có
    }
}