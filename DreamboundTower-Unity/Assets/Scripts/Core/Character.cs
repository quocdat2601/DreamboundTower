// File: Character.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;

/// <summary>
/// Core character class handling stats, combat, and visual effects
/// </summary>
public class Character : MonoBehaviour
{
    #region Base Stats
    [Header("Base Stats")]
    public int baseMaxHP = 100;
    public int baseAttackPower = 20; // Tương ứng với STR
    public int baseDefense = 0;      // Tương ứng với DEF
    public int baseMana = 50;        // <-- THÊM MỚI
    public int baseIntelligence = 10;// <-- THÊM MỚI (INT)
    public int baseAgility = 10;     // <-- THÊM MỚI (AGI)
    #endregion

    #region Current Stats
    [Header("Current Stats")]
    public int maxHP;
    public int currentHP;
    public int attackPower;
    public int defense;
    public int mana;                 // <-- THÊM MỚI
    public int currentMana;                 // <-- THÊM MỚI
    public int intelligence;         // <-- THÊM MỚI
    public int agility;              // <-- THÊM MỚI
    #endregion

    #region Events
    // Nó sẽ gửi đi thông tin về kẻ tấn công.
    public event Action<Character> OnDamaged;
    // Sự kiện này sẽ gửi đi 2 giá trị: currentHP và maxHP
    public event Action<int, int> OnHealthChanged;
    // Sự kiện này đã có, dùng cho Resurrect
    public System.Action<Character> OnDeath;
    #endregion

    #region UI Components
    [Header("UI")]
    public Slider hpSlider;
    public Image hpFillImage;
    public Slider manaSlider;
    public Image manaFillImage;
    #endregion
    
    #region Visual Effects
    [Header("Visual Effects")]
    [Tooltip("UI Image component for the character (for hit effects)")]
    public Image characterImage;
    
    [Tooltip("Is this character the player? (affects screen effects)")]
    public bool isPlayer = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        //ResetToBaseStats();
        //currentHP = maxHP;
        //UpdateHPUI();
    }
    #endregion

    #region Stats Management
    public void ResetToBaseStats()
    {
        maxHP = baseMaxHP;
        attackPower = baseAttackPower;
        defense = baseDefense;
        mana = baseMana;                 // <-- THÊM MỚI
        intelligence = baseIntelligence; // <-- THÊM MỚI
        agility = baseAgility;           // <-- THÊM MỚI
    }
    #endregion

    #region Equipment System
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
    #endregion

    #region Combat System
    public void Attack(Character target)
    {
        if (target == null) return;
        
        // Play attack animation
        PlayAttackAnimation();
        
        // Trigger hit effect at target position
        if (CombatEffectManager.Instance != null)
        {
            CombatEffectManager.Instance.PlayHitEffect(target.transform.position);
        }
        
        // Truyền "this" với tư cách là kẻ tấn công (attacker)
        target.TakeDamage(attackPower, this);
    }

    // ✅ SỬA LẠI: Hàm TakeDamage giờ nhận thêm tham số "attacker"
    public void TakeDamage(int damage, Character attacker)
    {
        int actualDamage = Mathf.Max(1, damage - defense);
        currentHP -= actualDamage;
        if (currentHP < 0) currentHP = 0;

        // Play being hit effects
        PlayHitAnimation();
        if (CombatEffectManager.Instance != null)
        {
            CombatEffectManager.Instance.PlayBeingHitEffect(this, actualDamage);
        }

        UpdateHPUI(); // Phát tín hiệu cho UI

        // Phát tín hiệu cho các gimmick như CounterAttack
        OnDamaged?.Invoke(attacker);

        if (currentHP <= 0)
        {
            Die();
        }
    }
    #endregion

    #region Health System
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
        // Play death animation
        PlayDeathAnimation();

        // Play death effect
        if (CombatEffectManager.Instance != null)
        {
            CombatEffectManager.Instance.PlayDeathEffect(this);
        }

        OnDeath?.Invoke(this);
        // BattleManager will handle cleanup after gimmicks like Resurrect have a chance to act
    }
    #endregion

    #region Utility Methods
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

        // Phát sóng tín hiệu để UI tự cập nhật
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    /// <summary>
    /// Hồi đầy lại Mana của nhân vật.
    /// </summary>
    public void RestoreFullMana()
    {
        currentMana = mana; // 'mana' là maxMana
    }

    /// <summary>
    /// Xóa bỏ tất cả các hiệu ứng trạng thái xấu đang có trên nhân vật.
    /// </summary>
    public void RemoveAllNegativeStatusEffects()
    {
        // TODO: Implement when status effect system is added
        // activeEffects.RemoveAll(effect => effect.isNegative == true);
    }
    #endregion
    
    #region Visual Effects & Animations
    /// <summary>
    /// Plays hit animation when character takes damage
    /// </summary>
    public void PlayHitAnimation()
    {
        if (characterImage != null)
        {
            // Flash red briefly
            characterImage.DOColor(Color.red, 0.1f)
                .OnComplete(() => characterImage.DOColor(Color.white, 0.1f));
            
            // Shake effect
            transform.DOShakePosition(0.2f, 10f, 10, 90f, false, true);
        }
    }
    
    /// <summary>
    /// Plays attack animation when character attacks
    /// </summary>
    public void PlayAttackAnimation()
    {
        if (characterImage != null)
        {
            // Scale up briefly for attack
            transform.DOScale(1.1f, 0.1f)
                .OnComplete(() => transform.DOScale(1f, 0.1f));
        }
    }
    
    /// <summary>
    /// Plays death animation - fade out and scale down
    /// </summary>
    public void PlayDeathAnimation()
    {
        if (characterImage != null)
        {
            // Fade out and scale down over 1 second
            characterImage.DOFade(0f, 1f);
            transform.DOScale(0.5f, 1f).OnComplete(() => {
                // Hide the character after animation completes
                gameObject.SetActive(false);
            });
        }
        else
        {
            // Fallback: just hide the character if no image component
            gameObject.SetActive(false);
        }
    }
    #endregion
}