// File: Character.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using StatusEffects;

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
    
    [Header("Passive Bonuses")]
    [Tooltip("Percentage bonus to mana regeneration from passive skills")]
    public float manaRegenBonus = 0f; // <-- THÊM MỚI
    [Tooltip("Percentage bonus to physical damage from passive skills")]
    public float physicalDamageBonus = 0f; // Physical damage bonus (0.05 = 5%)
    [Tooltip("Percentage bonus to magic damage from passive skills")]
    public float magicDamageBonus = 0f; // Magic damage bonus (0.05 = 5%)
    [Tooltip("Percentage of damage dealt returned as healing")]
    public float lifestealPercent = 0f; // Lifesteal percentage (0.05 = 5%)
    [Tooltip("Chance to dodge incoming attacks")]
    public float dodgeChance = 0f; // Dodge chance (0.1 = 10%)
    
    [Tooltip("Chance to land critical hits (0.1 = 10%)")]
    public float criticalChance = 0f; // Critical hit chance
    
    [Tooltip("Percentage damage reduction from all sources")]
    public float damageReduction = 0f; // Damage reduction (0.1 = 10%)
    [HideInInspector]
    public bool isInvincible = false;
    
    /// <summary>
    /// Tracks if the last damage received was magical (for color differentiation)
    /// </summary>
    private bool isLastDamageMagical = false;
    #endregion

    #region Events
    // Nó sẽ gửi đi thông tin về kẻ tấn công.
    public event Action<Character> OnDamaged;
    // Sự kiện này sẽ gửi đi 2 giá trị: currentHP và maxHP
    public event Action<int, int> OnHealthChanged;
    // Sự kiện này sẽ gửi đi 2 giá trị: currentMana và maxMana
    public event Action<int, int> OnManaChanged;
    // Sự kiện này đã có, dùng cho Resurrect
    public System.Action<Character> OnDeath;
    // (Gửi đi thông tin: Kẻ tấn công, Lượng sát thương thực tế đã nhận)
    public event System.Action<Character, int> OnDamageTaken;
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
    /// <summary>
    /// Plays revive animation (flashes yellow)
    /// </summary>
    public void PlayReviveAnimation()
    {
        if (characterImage != null)
        {
            // Đảm bảo quái vật hiện hình trở lại (nếu nó đã bị mờ)
            characterImage.DOFade(1f, 0.1f);
            transform.DOScale(1f, 0.1f);

            // Nhấp nháy màu vàng (Yellow) 3 lần
            // .SetLoops(6, LoopType.Yoyo) nghĩa là (đi -> về) * 3 = 6 GIAI ĐOẠN
            characterImage.DOColor(Color.yellow, 0.15f)
          .SetLoops(6, LoopType.Yoyo)
          .OnComplete(() => characterImage.color = Color.white); // Đảm bảo màu trở về trắng
        }
    }
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
    }
    #endregion

    #region Combat System
    public void Attack(Character target, float damageMultiplier = 1.0f)
    {
        if (target == null) return;

        // Play attack animation
        PlayAttackAnimation();

        // Tính sát thương gốc (100%)
        int baseDamage = CalculatePhysicalDamage(attackPower, target);

        // Check for critical hit
        bool isCriticalHit = CheckCritical();
        
        // Apply critical damage multiplier (2x damage)
        if (isCriticalHit)
        {
            damageMultiplier *= 2.0f;
            Debug.Log($"[ATTACK] CRITICAL HIT! {name} deals 2x damage!");
        }

        // ✅ ÁP DỤNG HỆ SỐ SÁT THƯƠNG
        int totalDamage = Mathf.RoundToInt(baseDamage * damageMultiplier);
        
        Debug.Log($"[ATTACK] {name} attacks {target.name} for {totalDamage} {(isCriticalHit ? "CRIT" : "normal")} damage");

        // Note: Hit/Miss effect will be shown inside TakeDamage based on dodge result

        // (Code TakeDamage... giữ nguyên)
        if (target.isPlayer)
        {
            target.TakeDamageWithShield(totalDamage, this, false, isCriticalHit);
        }
        else
        {
            target.TakeDamage(totalDamage, this, isCriticalHit);
        }

        // Apply regular lifesteal if available
        if (lifestealPercent > 0f)
        {
            int healAmount = Mathf.RoundToInt(totalDamage * lifestealPercent);
            RestoreHealth(healAmount);
            Debug.Log($"[PASSIVE] Lifesteal: {healAmount} HP restored from {totalDamage} damage");
        }
        
        // Apply conditional lifesteal if ConditionalPassiveManager exists
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null)
        {
            conditionalManager.ApplyConditionalLifesteal(totalDamage);
        }
    }
    
    /// <summary>
    /// Calculates physical damage with passive bonuses
    /// </summary>
    public int CalculatePhysicalDamage(int baseDamage)
    {
        float bonusMultiplier = 1f + physicalDamageBonus;
        return Mathf.RoundToInt(baseDamage * bonusMultiplier);
    }
    
    /// <summary>
    /// Calculates physical damage with conditional bonuses against a specific target
    /// </summary>
    public int CalculatePhysicalDamage(int baseDamage, Character target)
    {
        float bonusMultiplier = 1f + physicalDamageBonus;
        
        // Apply conditional bonuses if ConditionalPassiveManager exists
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null)
        {
            return conditionalManager.CalculatePhysicalDamage(Mathf.RoundToInt(baseDamage * bonusMultiplier), target);
        }
        
        return Mathf.RoundToInt(baseDamage * bonusMultiplier);
    }
    
    /// <summary>
    /// Calculates magic damage with passive bonuses
    /// </summary>
    public int CalculateMagicDamage(int baseDamage)
    {
        float bonusMultiplier = 1f + magicDamageBonus;
        return Mathf.RoundToInt(baseDamage * bonusMultiplier);
    }
    
    /// <summary>
    /// Checks if character dodges an attack
    /// </summary>
    public bool CheckDodge()
    {
        // TEMPORARY: 100% dodge for PLAYER ONLY (for testing)
        // if (isPlayer)
        // {
        //     dodgeChance = 1.0f;
        // }
        
        if (dodgeChance <= 0f) return false;
        
        float roll = UnityEngine.Random.Range(0f, 1f);
        bool dodged = roll < dodgeChance;
        
        Debug.Log($"[DODGE CHECK] {name}: Roll={roll:F2}, DodgeChance={dodgeChance:F2}, Result={(dodged ? "MISSED" : "HIT")}");
        
        if (dodged)
        {
            Debug.Log($"[PASSIVE] {name} dodged! Roll: {roll:F2} < {dodgeChance:F2}");
        }
        
        return dodged;
    }
    
    /// <summary>
    /// Checks if attack is a critical hit
    /// </summary>
    public bool CheckCritical()
    {
        // TEMPORARY: 100% crit for PLAYER ONLY (for testing)
        // if (isPlayer)
        // {
        //     criticalChance = 1.0f;
        // }
        
        if (criticalChance <= 0f) return false;
        
        float roll = UnityEngine.Random.Range(0f, 1f);
        bool isCritical = roll < criticalChance;
        
        if (isCritical)
        {
            Debug.Log($"[CRITICAL] {name} landed a CRIT! Roll: {roll:F2} < {criticalChance:F2}");
        }
        
        return isCritical;
    }
    
    /// <summary>
    /// Checks if character is a boss enemy
    /// </summary>
    public bool IsBoss()
    {
        // This could be determined by a tag, name, or specific property
        // For now, we'll check if the name contains "Boss"
        return name.ToLower().Contains("boss");
    }

    /// <summary>
    /// Applies damage reduction to incoming damage
    /// </summary>
    public int ApplyDamageReduction(int damage)
    {
        if (damageReduction <= 0f) return damage;
        
        int reducedDamage = Mathf.RoundToInt(damage * (1f - damageReduction));
        int reductionAmount = damage - reducedDamage;
        
        if (reductionAmount > 0)
        {
            Debug.Log($"[PASSIVE] Damage reduction: {damage} -> {reducedDamage} (-{reductionAmount})");
        }
        
        return reducedDamage;
    }
    

    // ✅ SỬA LẠI: Hàm TakeDamage giờ nhận thêm tham số "attacker" và "isCritical"
    public void TakeDamage(int damage, Character attacker, bool isCritical = false)
    {
        if (isInvincible)
        {
            Debug.Log($"[BATTLE] {name} Bất tử! Đã chặn {damage} sát thương.");
            return;
        }
        // Check for dodge first
        bool dodged = CheckDodge();
        
        if (dodged)
        {
            // Play miss animation
            PlayMissAnimation();
            
            // Show "MISS" text
            if (CombatEffectManager.Instance != null)
            {
                Vector3 uiPosition = CombatEffectManager.Instance.GetCharacterUIPosition(this);
                CombatEffectManager.Instance.ShowDamageNumber(uiPosition, 0, false, false, true);
            }
            
            return; // Attack missed, no damage taken
        }
        
        // Apply conditional damage reduction first if ConditionalPassiveManager exists
        int conditionallyReducedDamage = damage;
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null)
        {
            conditionallyReducedDamage = conditionalManager.ApplyConditionalDamageReduction(damage, attacker);
        }
        
        // Apply regular damage reduction
        int reducedDamage = ApplyDamageReduction(conditionallyReducedDamage);
        
        int actualDamage = Mathf.Max(1, reducedDamage - defense);
        currentHP -= actualDamage;
        if (currentHP < 0) currentHP = 0;

        // Gửi đi "attacker" và "actualDamage"
       OnDamageTaken?.Invoke(attacker, actualDamage);

        // Play being hit effects (includes animation + damage number)
        if (CombatEffectManager.Instance != null)
        {
            // Show damage with appropriate color based on damage type
            CombatEffectManager.Instance.PlayBeingHitEffect(this, actualDamage, isCritical, isLastDamageMagical);
            isLastDamageMagical = false; // Reset flag
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

    /// <summary>
    /// Updates mana UI display
    /// </summary>
    public void UpdateManaUI()
    {
        // PHÁT SÓNG TÍN HIỆU RA BÊN NGOÀI
        OnManaChanged?.Invoke(currentMana, mana);

        // CẬP NHẬT UI CỤC BỘ
        float t = (float)currentMana / mana;
        if (manaSlider != null)
            manaSlider.value = t;
        if (manaFillImage != null)
            manaFillImage.fillAmount = t;
    }

    // Tương tự, nếu bạn có hàm Heal(int amount), cũng hãy gọi event ở cuối
    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;

        // ✅ PHÁT SÓNG TÍN HIỆU
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }
    public void HealPercent(int percent)
    {
        int amountToHeal = Mathf.RoundToInt(maxHP * (percent / 100f));
        Heal(amountToHeal); // Gọi hàm Heal(flat)
    }

    public void TakeDamagePercent(int percent)
    {
        int amountToDamage = Mathf.RoundToInt(maxHP * (percent / 100f));
        TakeDamage(amountToDamage, null); // Gọi hàm TakeDamage(flat)
    }

    /// <summary>
    /// Restores mana by the specified amount
    /// </summary>
    public void RestoreMana(int amount)
    {
        currentMana += amount;
        if (currentMana > mana) currentMana = mana;

        // ✅ PHÁT SÓNG TÍN HIỆU
        OnManaChanged?.Invoke(currentMana, mana);
    }
    public void UseMana(int amount)
    {
        currentMana -= amount;
        OnManaChanged?.Invoke(currentMana, mana);
    }

    public void RestoreManaPercent(int percent)
    {
        int amountToRestore = Mathf.RoundToInt(mana * (percent / 100f));
        RestoreMana(amountToRestore); // Gọi hàm RestoreMana(flat)
    }
    public void UseManaPercent(int percent)
    {
        // Tính toán mana sử dụng dựa trên % mana tối đa (mana)
        int amountToUse = Mathf.RoundToInt(mana * (percent / 100f));
        UseMana(amountToUse); // Gọi hàm UseMana(flat) với lượng mana đã tính
    }
    void Die()
    {
        // Play death animation (fade out and scale down)
        PlayDeathAnimation();

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
        UpdateManaUI(); // Update mana display
    }
    
    public void RestoreHealth(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHPUI();
    }
    
    // Generic Status Effect Integration
    public int CurrentShield => ShieldEffectHandler.GetShieldAmount(this);
    public int ShieldTurnsRemaining => ShieldEffectHandler.GetShieldTurns(this);
    public float DamageReflectionPercent => ShieldEffectHandler.GetReflectPercent(this);
    
    /// <summary>
    /// Gets the intensity value of a status effect
    /// </summary>
    private int GetStatusEffectValue<T>() where T : StatusEffect
    {
        if (StatusEffectManager.Instance == null) return 0;
        
        var effect = StatusEffectManager.Instance.GetEffect(this, typeof(T)) as T;
        return effect?.intensity ?? 0;
    }
    
    /// <summary>
    /// Gets the duration of a status effect
    /// </summary>
    private int GetStatusEffectDuration<T>() where T : StatusEffect
    {
        if (StatusEffectManager.Instance == null) return 0;
        
        var effect = StatusEffectManager.Instance.GetEffect(this, typeof(T)) as T;
        return effect?.duration ?? 0;
    }
    
    /// <summary>
    /// Checks if character has a specific status effect
    /// </summary>
    public bool HasStatusEffect<T>() where T : StatusEffect
    {
        if (StatusEffectManager.Instance == null) return false;
        return StatusEffectManager.Instance.HasEffect(this, typeof(T));
    }
    
    /// <summary>
    /// Legacy method for backward compatibility - now uses ShieldEffectHandler
    /// </summary>
    [System.Obsolete("Use ShieldEffectHandler.ApplyShield() instead")]
    public void ApplyShield(int shieldAmount, int turns, float reflectionPercent = 0f)
    {
        ShieldEffectHandler.ApplyShield(this, shieldAmount, turns, reflectionPercent);
    }
    
    /// <summary>
    /// Legacy method for backward compatibility - now handled by StatusEffectManager
    /// </summary>
    [System.Obsolete("Status effects are now handled automatically by StatusEffectManager")]
    public void ReduceShieldTurns()
    {
        // This is now handled automatically by StatusEffectManager
    }
    
    /// <summary>
    /// Takes damage with shield protection and reflection
    /// </summary>
    public int TakeDamageWithShield(int damage, Character attacker, bool isMagicalDamage = false, bool isCritical = false)
    {
        if (isInvincible)
        {
            Debug.Log($"[BATTLE] {name} Bất tử! Đã chặn {damage} sát thương (vào khiên).");
            return 0; // Không nhận sát thương, không phản đòn
       }
        int reflectedDamage = 0;
        int actualDamage = damage;
        
        // Get current shield and reflect values from status effects
        int currentShield = CurrentShield;
        float reflectPercent = DamageReflectionPercent;
        
        // Shield absorbs damage first
        if (currentShield > 0)
        {
            int shieldAbsorbed = Mathf.Min(damage, currentShield);
            actualDamage = damage - shieldAbsorbed;
            
            // Reduce shield amount using ShieldEffectHandler
            ShieldEffectHandler.ReduceShieldAmount(this, shieldAbsorbed);
            
            // Reflect damage back to attacker
            if (reflectPercent > 0 && attacker != null)
            {
                reflectedDamage = Mathf.RoundToInt(damage * reflectPercent / 100f);
                attacker.TakeDamage(reflectedDamage, this);
            }
        }
        
        // Apply remaining damage to HP
        if (actualDamage > 0)
        {
            isLastDamageMagical = isMagicalDamage; // Set flag before taking damage
            TakeDamage(actualDamage, attacker, isCritical);
        }
        
        return reflectedDamage;
    }

    /// <summary>
    /// Regenerates mana over time (call this in Update or coroutine)
    /// </summary>
    public void RegenerateMana(int amount)
    {
        if (currentMana < mana)
        {
            currentMana = Mathf.Min(mana, currentMana + amount);
            UpdateManaUI();
        }
    }
    
    /// <summary>
    /// Regenerates mana based on percentage of max mana
    /// </summary>
    public void RegenerateManaPercent(float percent)
    {
        // Add passive bonus to base regeneration percentage
        // percent is already in percentage form (5.0f = 5%)
        // manaRegenBonus is in decimal form (0.05 = 5%)
        float totalPercent = percent + (manaRegenBonus * 100f);
        int regenAmount = Mathf.RoundToInt(mana * totalPercent / 100.0f);
        
        // Mana regeneration with passive bonus
        
        RegenerateMana(regenAmount);
    }

    /// <summary>
    /// Xóa bỏ tất cả các hiệu ứng trạng thái xấu đang có trên nhân vật.
    /// </summary>
    public void RemoveAllNegativeStatusEffects()
    {
        // Use StatusEffectManager to remove negative effects
        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.RemoveAllNegativeEffects(this);
        }
    }
    

    /// <summary>
    /// Resets mana regeneration bonus to zero (used when clearing passive skills)
    /// </summary>
    public void ResetManaRegenBonus()
    {
        manaRegenBonus = 0f;
    }
    
    /// <summary>
    /// Resets all passive skill bonuses to zero (used when clearing passive skills)
    /// </summary>
    public void ResetAllPassiveBonuses()
    {
        manaRegenBonus = 0f;
        physicalDamageBonus = 0f;
        magicDamageBonus = 0f;
        lifestealPercent = 0f;
        dodgeChance = 0f;
        damageReduction = 0f;
        
        // Reset conditional passive bonuses if ConditionalPassiveManager exists
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null)
        {
            conditionalManager.ResetAllConditionalBonuses();
        }
    }
    #endregion
    
    #region Visual Effects & Animations
    /// <summary>
    /// Plays hit animation when character takes damage
    /// Includes PlayMissAnimation, PlayCriticalHitAnimation, PlayHitAnimation with customizable colors
    /// </summary>
    public void PlayHitAnimation()
    {
        PlayHitAnimation(Color.red);
    }
    
    public void PlayHitAnimation(Color flashColor)
    {
        if (characterImage != null)
        {
            // Flash color briefly
            characterImage.DOColor(flashColor, 0.1f)
                .OnComplete(() => characterImage.DOColor(Color.white, 0.1f));
            
            // Shake effect
            transform.DOShakePosition(0.2f, 10f, 10, 90f, false, true);
        }
    }
    
    public void PlayCriticalHitAnimation()
    {
        if (characterImage != null)
        {
            // Flash yellow and scale up for critical
            characterImage.DOColor(Color.yellow, 0.15f)
                .OnComplete(() => characterImage.DOColor(Color.white, 0.1f));
            
            // Larger shake for critical hits
            transform.DOShakePosition(0.3f, 15f, 15, 90f, false, true);
            
            // Brief scale up
            transform.DOScale(1.15f, 0.1f)
                .OnComplete(() => transform.DOScale(1f, 0.1f));
        }
    }
    
    public void PlayMissAnimation()
    {
        if (characterImage != null)
        {
            // Subtle blue flash for dodges
            characterImage.DOColor(new Color(0.8f, 0.8f, 1f, 1f), 0.1f)
                .OnComplete(() => characterImage.DOColor(Color.white, 0.1f));
            
            // Gentle shake for miss
            transform.DOShakePosition(0.15f, 5f, 10, 90f, false, true);
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
                //gameObject.SetActive(false);
            });
        }
        else
        {
            // Fallback: just hide the character if no image component
            //gameObject.SetActive(false);
        }
    }
    #endregion
}