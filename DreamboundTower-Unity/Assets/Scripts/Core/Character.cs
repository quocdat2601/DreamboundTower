// File: Character.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using DG.Tweening;
using StatusEffects;
public enum DamageType
{
    Physical, // Vật lý (màu trắng)
    Magic,    // Phép (màu xanh)
    True      // Sát thương chuẩn (ví dụ: màu vàng - nếu cần sau này)
}
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

    // Dodge formula: 0.3% per AGI, max 40% cap
    // You need 134 AGI to reach cap (134 * 0.003 = 0.402 > 0.40)
    public const float DODGE_PER_AGI = 0.003f; // 0.3% dodge per AGI point (per design doc)
    public const float DODGE_CAP = 0.40f; // 40% maximum dodge chance
    
    // Double Action formula: 0.15% per AGI, max 25%
    // You need 167 AGI to reach cap (167 * 0.0015 = 0.2505 > 0.25)
    public const float DOUBLE_ACTION_PER_AGI = 0.0015f; // 0.15% double action per AGI point
    public const float DOUBLE_ACTION_CAP = 0.25f; // 25% maximum double action chance
    
    // Critical Damage formula: Crits deal 1.5x damage (was 2.0x)
    public const float CRITICAL_DAMAGE_MULTIPLIER = 1.5f; // Critical hits deal 1.5x damage

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
    private Equipment equipment;

    /// <summary>
    /// Tracks if the last damage received was magical (for color differentiation)
    /// </summary>
    private bool isLastDamageMagical = false;
    
    /// <summary>
    /// Tracks the type of last damage received (for color differentiation)
    /// </summary>
    private DamageType lastDamageType = DamageType.Physical;
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
    
    #region Visual Components
    [Header("Visual Effects")]
    [Tooltip("UI Image component for the character (for hit effects)")]
    public Image characterImage;
    
    [Tooltip("Is this character the player? (affects screen effects)")]
    public bool isPlayer = false;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        equipment = GetComponent<Equipment>();
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
        UpdateDerivedStats();
    }
    /// <summary>
    /// Tính toán lại các chỉ số phụ thuộc (derived stats) như Dodge Chance.
    /// Gọi hàm này sau khi base stats, gear bonus, hoặc buff/debuff thay đổi.
    /// </summary>
    public void UpdateDerivedStats()
    {
        // Tính Dodge Chance từ Agility hiện tại
        // agility là chỉ số thực tế đã bao gồm bonus từ trang bị/buff
        dodgeChance = Mathf.Clamp(agility * DODGE_PER_AGI, 0f, DODGE_CAP);

        // (Sau này có thể thêm tính Crit Chance, Block Chance... vào đây nếu cần)

         Debug.Log($"[{name}] Updated Derived Stats: Agility={agility}, DodgeChance={dodgeChance * 100f}%");
    }
    #endregion

    #region Equipment System
    public void AddGearBonus(GearItem gear)
    {
        if (gear == null) return;

        maxHP += gear.hpBonus;
        attackPower += gear.attackBonus;
        defense += gear.defenseBonus;
        intelligence += gear.intBonus;
        mana += gear.manaBonus;
        agility += gear.agiBonus;

        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
        if (currentMana > mana)
        {
            currentMana = mana;
        }
        UpdateDerivedStats();
        UpdateHPUI();
        UpdateManaUI();
    }

    public void RemoveGearBonus(GearItem gear)
    {
        if (gear == null) return;

        maxHP -= gear.hpBonus;
        attackPower -= gear.attackBonus;
        defense -= gear.defenseBonus;
        intelligence -= gear.intBonus;
        mana -= gear.manaBonus;
        agility -= gear.agiBonus;

        maxHP = Mathf.Max(maxHP, baseMaxHP);
        attackPower = Mathf.Max(attackPower, baseAttackPower);
        defense = Mathf.Max(defense, baseDefense);
        intelligence = Mathf.Max(intelligence, baseIntelligence);
        mana = Mathf.Max(mana, baseMana);
        agility = Mathf.Max(agility, baseAgility);

        if (currentHP > maxHP)
        {
            currentHP = maxHP;
        }
        if (currentMana > mana)
        {
            currentMana = mana;
        }

        UpdateDerivedStats(); // Recalculate dodge chance after AGI change
        UpdateHPUI();
        UpdateManaUI();
    }
    #endregion

    #region Combat System
    // Trong Character.cs

    public void Attack(Character target, float damageMultiplier = 1.0f)
    {
        if (target == null) return;
        PlayAttackAnimation(); // Chơi animation tấn công 1 lần
        AudioManager.Instance?.PlayAttackSFX();
        // 1. Get base physical and magic damage from weapon
        (int physicalBase, int magicBase) = CalculateAttackDamage();

        // 1.5. Check for critical hits SEPARATELY for Physical and Magic
        bool isPhysicalCrit = false;
        bool isMagicCrit = false;
        
        if (physicalBase > 0)
        {
            isPhysicalCrit = CheckCritical();
            //AudioManager.Instance?.PlayCriticalHitSFX(); // set tạm để test coi đúng không
            if (isPhysicalCrit)
            {
                Debug.Log($"[CRITICAL] {name} Physical attack will CRIT!");
            }
        }
        
        if (magicBase > 0)
        {
            isMagicCrit = CheckCritical();
            if (isMagicCrit)
            {
                Debug.Log($"[CRITICAL] {name} Magic attack will CRIT!");
            }
        }

        // 2. Áp dụng Multiplier (ví dụ: CounterAttack) cho CẢ HAI
        int modifiedPhysical = Mathf.RoundToInt(physicalBase * damageMultiplier);
        int modifiedMagic = Mathf.RoundToInt(magicBase * damageMultiplier);
        
        // Apply critical damage multiplier if that type crit
        if (isPhysicalCrit)
        {
            modifiedPhysical = Mathf.RoundToInt(modifiedPhysical * CRITICAL_DAMAGE_MULTIPLIER);
        }
        if (isMagicCrit)
        {
            modifiedMagic = Mathf.RoundToInt(modifiedMagic * CRITICAL_DAMAGE_MULTIPLIER);
        }

        // 3. Apply passive bonuses separately for each damage type
        int finalPhysicalDamage = CalculatePhysicalDamage(modifiedPhysical, target);
        int finalMagicDamage = CalculateMagicDamage(modifiedMagic);

        // 4. Apply damage and effects sequentially
        bool hitConnected = false;

        // --- Áp dụng sát thương VẬT LÝ (Nếu > 0) ---
        if (finalPhysicalDamage > 0)
        {
            // Hiệu ứng Hit Vật lý (Giả sử PlayHitEffect là hiệu ứng vật lý)
            if (CombatEffectManager.Instance != null)
            {
                CombatEffectManager.Instance.PlayHitEffect(target.transform.position);
            }

            // Gây sát thương Vật lý (Gọi hàm đã nâng cấp với crit flag)
            if (target.isPlayer)
            {
                target.TakeDamageWithShield(finalPhysicalDamage, this, DamageType.Physical, isPhysicalCrit); // Truyền Type và Crit
            }
            else
            {
                target.TakeDamage(finalPhysicalDamage, this, DamageType.Physical, isPhysicalCrit); // Truyền Type và Crit
            }
            hitConnected = true; // Đánh dấu đã đánh trúng
        }

        // --- Áp dụng sát thương PHÉP (Nếu > 0) ---
        if (finalMagicDamage > 0)
        {
            // Hiệu ứng Hit Phép (Tạm thời dùng hiệu ứng vật lý, bạn có thể tạo hàm riêng sau)
            if (CombatEffectManager.Instance != null)
            {
                // CombatEffectManager.Instance.PlayMagicHitEffect(target.transform.position); // Hàm riêng nếu có
                CombatEffectManager.Instance.PlayHitEffect(target.transform.position); // Tạm dùng hiệu ứng cũ
            }

            // Gây sát thương Phép (Gọi hàm đã nâng cấp với crit flag)
            if (target.isPlayer)
            {
                target.TakeDamageWithShield(finalMagicDamage, this, DamageType.Magic, isMagicCrit); // Truyền Type và Crit
            }
            else
            {
                target.TakeDamage(finalMagicDamage, this, DamageType.Magic, isMagicCrit); // Truyền Type và Crit
            }
            hitConnected = true; // Đánh dấu đã đánh trúng
        }

        // 5. Xử lý Lifesteal (Ví dụ: Chỉ tính trên phần sát thương vật lý)
        if (hitConnected && lifestealPercent > 0f)
        {
            int totalDamageDealtForLifesteal = finalPhysicalDamage; // Chỉ tính phần vật lý
                                                                    // Hoặc: int totalDamageDealtForLifesteal = finalPhysicalDamage + finalMagicDamage; // Nếu muốn tính cả hai

            int healAmount = Mathf.RoundToInt(totalDamageDealtForLifesteal * lifestealPercent);
            if (healAmount > 0) // Chỉ hồi máu nếu lifesteal > 0
            {
                RestoreHealth(healAmount);
                // Debug.Log($"[PASSIVE] Lifesteal: {healAmount} HP restored from {totalDamageDealtForLifesteal} physical damage");
            }
        }
        // (Code Conditional Lifesteal giữ nguyên nếu có)
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null && hitConnected) // Chỉ apply nếu đánh trúng
        {
            // Truyền tổng sát thương gây ra (trước khi trừ DEF) vào hàm conditional lifesteal
            conditionalManager.ApplyConditionalLifesteal(finalPhysicalDamage + finalMagicDamage);
        }
    }
    /// <summary>
    /// Tính toán sát thương cơ bản cho đòn Attack dựa trên vũ khí trang bị.
    /// </summary>
    private (int physicalDamage, int magicDamage) CalculateAttackDamage()
    {
        WeaponScalingType scaling = WeaponScalingType.STR;
        GearItem equippedWeapon = null;
        int physicalBase = 0;
        int magicBase = 0;

        // Lấy vũ khí (Slot 3)
        if (equipment != null && equipment.equipmentSlots != null && equipment.equipmentSlots.Length > 3)
        {
            equippedWeapon = equipment.equipmentSlots[3]; // Đã sửa thành slot 3
        }
        // Debug.Log($"[CalculateAttackDamage] Weapon in slot 3: {(equippedWeapon != null ? equippedWeapon.itemName : "None")}"); // Giữ log này

        if (equippedWeapon != null && equippedWeapon.gearType == GearType.Weapon)
        {
            // Debug.Log($"[CalculateAttackDamage] Reading scalingType from {equippedWeapon.itemName}: {equippedWeapon.scalingType}"); // Giữ log này
            scaling = equippedWeapon.scalingType;
        }
        else
        {
            // Debug.Log($"[CalculateAttackDamage] No weapon or not a weapon. Defaulting to STR."); // Giữ log này
            scaling = WeaponScalingType.STR;
        }

        // Tính toán sát thương gốc dựa trên scaling
        switch (scaling)
        {
            case WeaponScalingType.STR:
                physicalBase = attackPower; // 100% STR -> Vật lý
                magicBase = 0;
                break;
            case WeaponScalingType.INT:
                physicalBase = 0;
                magicBase = intelligence; // 100% INT -> Phép
                break;
            case WeaponScalingType.Hybrid:
                physicalBase = Mathf.RoundToInt(attackPower * 0.7f); // 70% STR -> Vật lý
                magicBase = Mathf.RoundToInt(intelligence * 0.3f); // 30% INT -> Phép
                break;
            case WeaponScalingType.None:
            default:
                physicalBase = attackPower; // Mặc định về STR
                magicBase = 0;
                break;
        }

        physicalBase = Mathf.Max(0, physicalBase);
        magicBase = Mathf.Max(0, magicBase);

        // Trả về cả hai giá trị
        return (physicalBase, magicBase);
    }
    // Hàm CalculatePhysicalDamage chỉ làm nhiệm vụ cộng bonus bị động/có điều kiện
    // Nó nhận sát thương đã tính theo vũ khí làm đầu vào
    public int CalculatePhysicalDamage(int baseDamageFromWeapon, Character target) // Đổi tên tham số cho rõ
    {
        float bonusMultiplier = 1f + physicalDamageBonus; // Bonus bị động % vật lý

        // Áp dụng bonus có điều kiện (nếu có)
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null)
        {
            // Truyền sát thương đã nhân bonus bị động vào hàm điều kiện
            return conditionalManager.CalculatePhysicalDamage(Mathf.RoundToInt(baseDamageFromWeapon * bonusMultiplier), target);
        }

        // Trả về sát thương cuối cùng trước khi trừ DEF
        return Mathf.RoundToInt(baseDamageFromWeapon * bonusMultiplier);
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
        if (isPlayer)
        {
            dodgeChance = 1.0f;
        }
        
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
        if (isPlayer)
        {
            criticalChance = 1.0f;
        }
        
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
    

    // ✅ Hàm TakeDamage nhận tham số "attacker", "damageType", và "isCritical"
    public void TakeDamage(int damage, Character attacker, DamageType damageType = DamageType.Physical, bool isCritical = false)
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
            PlayDodgeAnimation();
            AudioManager.Instance?.PlayMissSFX();
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
        
        // NOTE: Currently, defense applies to BOTH physical and magic
        // TODO: If you want separate Magic Resistance, add int magicDefense stat and check damageType here
        int actualDamage = Mathf.Max(1, reducedDamage - defense);
        currentHP -= actualDamage;
        if (currentHP < 0) currentHP = 0;
        
        // Track damage type
        lastDamageType = damageType;
        isLastDamageMagical = (damageType == DamageType.Magic);
        
        // Choose color based on damage type AND crit status
        Color damageColor = Color.white; // Default: white (Physical Normal)
        if (damageType == DamageType.Magic)
        {
            damageColor = isCritical ? new Color(0f, 0.4f, 0.8f) : Color.cyan; // Darker blue (crit) or cyan (normal)
        }
        else if (damageType == DamageType.Physical)
        {
            damageColor = isCritical ? new Color(1f, 0.5f, 0f) : Color.white; // Orange (crit) or white (normal)
        }
        else if (damageType == DamageType.True)
        {
            damageColor = Color.yellow; // Yellow (True damage)
        }

        // Show damage with appropriate color based on damage type and crit
        if (CombatEffectManager.Instance != null)
        {
            Vector3 uiPosition = CombatEffectManager.Instance.GetCharacterUIPosition(this);
            
            // For critical hits, show "CRIT! [damage]" with custom color
            if (isCritical)
            {
                string displayText = "CRIT! " + actualDamage.ToString();
                CombatEffectManager.Instance.ShowDamageNumberAtPosition(uiPosition, displayText, damageColor);
            }
            else
            {
                // Normal damage - use regular ShowDamageNumber for proper magical color handling
                bool isMagical = (damageType == DamageType.Magic);
                CombatEffectManager.Instance.ShowDamageNumber(uiPosition, actualDamage, false, isMagical, false);
            }
        }

        // Gửi đi "attacker" và "actualDamage"
        OnDamageTaken?.Invoke(attacker, actualDamage);

        // Play hit animation (no damage number, already shown above)
        if (isCritical)
        {
            PlayCriticalHitAnimation(); // Critical hit animation
        }
        else
        {
            PlayHitAnimation(); // Normal hit animation
        }
        
        isLastDamageMagical = false; // Reset flag

        UpdateHPUI(); // Phát tín hiệu cho UI

        // Phát tín hiệu cho các gimmick như CounterAttack
        OnDamaged?.Invoke(attacker);

        if (currentHP <= 0)
        {
            Die();
        }
    }
    #endregion

    #region UI Updates
    public void UpdateHPUI()
    {
        // PHÁT SÓNG TÍN HIỆU RA BÊN NGOÀI
        OnHealthChanged?.Invoke(currentHP, maxHP);

        // CẬP NHẬT UI CỤC BỘ
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
    #endregion

    #region Health Management
    public void Heal(int amount)
    {
        currentHP += amount;
        if (currentHP > maxHP) currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void HealPercent(int percent)
    {
        int amountToHeal = Mathf.RoundToInt(maxHP * (percent / 100f));
        Heal(amountToHeal);
    }

    /// <summary>
    /// Heals character by percentage of max HP (0.5f = 50%)
    /// </summary>
    /// <param name="percentage">Percentage to heal (0.5f = 50%)</param>
    public void HealPercentage(float percentage)
    {
        int healAmount = Mathf.FloorToInt(maxHP * percentage);
        currentHP += healAmount;
        if (currentHP > maxHP) currentHP = maxHP;
        OnHealthChanged?.Invoke(currentHP, maxHP);
    }

    public void RestoreHealth(int amount)
    {
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        UpdateHPUI();
    }

    public void TakeDamagePercent(int percent)
    {
        int amountToDamage = Mathf.RoundToInt(maxHP * (percent / 100f));
        TakeDamage(amountToDamage, null);
    }

    void Die()
    {
        //PlayDeathAnimation();
        OnDeath?.Invoke(this);
    }
    #endregion

    #region Mana Management
    /// <summary>
    /// Restores mana by the specified amount
    /// </summary>
    public void RestoreMana(int amount)
    {
        currentMana += amount;
        if (currentMana > mana) currentMana = mana;
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
        RestoreMana(amountToRestore);
    }

    public void UseManaPercent(int percent)
    {
        int amountToUse = Mathf.RoundToInt(mana * (percent / 100f));
        UseMana(amountToUse);
    }

    /// <summary>
    /// Restores mana to full
    /// </summary>
    public void RestoreFullMana()
    {
        currentMana = mana;
        UpdateManaUI();
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
        float totalPercent = percent + (manaRegenBonus * 100f);
        int regenAmount = Mathf.RoundToInt(mana * totalPercent / 100.0f);
        RegenerateMana(regenAmount);
    }
    #endregion

    #region Status Effects Integration
    // Generic Status Effect Integration
    public int CurrentShield => ShieldEffectHandler.GetShieldAmount(this);
    public int ShieldTurnsRemaining => ShieldEffectHandler.GetShieldTurns(this);
    public float DamageReflectionPercent => ShieldEffectHandler.GetReflectPercent(this);
    
    /// <summary>
    /// Checks if character has a specific status effect
    /// </summary>
    public bool HasStatusEffect<T>() where T : StatusEffect
    {
        if (StatusEffectManager.Instance == null) return false;
        return StatusEffectManager.Instance.HasEffect(this, typeof(T));
    }
    
    /// <summary>
    /// Removes all negative status effects from character
    /// </summary>
    public void RemoveAllNegativeStatusEffects()
    {
        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.RemoveAllNegativeEffects(this);
        }
    }
    
    /// <summary>
    /// Takes damage with shield protection and reflection
    /// </summary>
    public int TakeDamageWithShield(int damage, Character attacker, DamageType damageType = DamageType.Physical, bool isCritical = false)
    {
        if (isInvincible)
        {
            Debug.Log($"[BATTLE] {name} Bất tử! Đã chặn {damage} sát thương (vào khiên).");
            return 0;
        }

        bool dodged = CheckDodge();

        if (dodged)
        {
            // Play miss animation
            PlayMissAnimation();
            PlayDodgeAnimation();
            AudioManager.Instance?.PlayMissSFX();
            // Show "MISS" text
            if (CombatEffectManager.Instance != null)
            {
                Vector3 uiPosition = CombatEffectManager.Instance.GetCharacterUIPosition(this);
                CombatEffectManager.Instance.ShowDamageNumber(uiPosition, 0, false, false, true);
            }

            return 0; // Attack missed, no damage taken
        }

        int reflectedDamage = 0;
        int actualDamagePassedToHP = damage;
        int currentShield = CurrentShield;
        float reflectPercent = DamageReflectionPercent;

        // Shield absorbs damage first
        if (currentShield > 0)
        {
            int shieldAbsorbed = Mathf.Min(damage, currentShield);
            actualDamagePassedToHP = damage - shieldAbsorbed;
            ShieldEffectHandler.ReduceShieldAmount(this, shieldAbsorbed);

            if (reflectPercent > 0 && attacker != null)
            {
                reflectedDamage = Mathf.RoundToInt(damage * reflectPercent / 100f);
                attacker.TakeDamage(reflectedDamage, this, DamageType.Physical);
            }
        }

        // Apply remaining damage
        if (actualDamagePassedToHP > 0)
        {
            TakeDamage(actualDamagePassedToHP, attacker, damageType, isCritical);
        }
        return reflectedDamage;
    }
    #endregion

    #region Passive Bonuses Management
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
        
        var conditionalManager = GetComponent<ConditionalPassiveManager>();
        if (conditionalManager != null)
        {
            conditionalManager.ResetAllConditionalBonuses();
        }
    }
    #endregion
    
    #region Visual Effects & Animations
    /// <summary>
    /// Plays revive animation (flashes yellow)
    /// </summary>
    public void PlayReviveAnimation()
    {
        if (characterImage != null)
        {
            characterImage.DOFade(1f, 0.1f);
            transform.DOScale(1f, 0.1f);
            characterImage.DOColor(Color.yellow, 0.15f)
                .SetLoops(6, LoopType.Yoyo)
                .OnComplete(() => characterImage.color = Color.white);
        }
    }

    /// <summary>
    /// Plays hit animation when character takes damage
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

    /// <summary>
    /// Plays dodge animation (moves back slightly then returns).
    /// </summary>
    public void PlayDodgeAnimation()
    {
        // Lưu vị trí gốc
        Vector3 originalPosition = transform.position;
        // Tính vị trí lùi lại (ví dụ: lùi 30 unit theo trục X âm - bạn có thể điều chỉnh)
        float dodgeDistance = 30f;
        Vector3 dodgePosition = originalPosition - transform.right * dodgeDistance; // Dùng transform.right để lùi đúng hướng

        // Tạo chuỗi animation: Lùi nhanh -> Dừng ngắn -> Về nhanh
        Sequence dodgeSequence = DOTween.Sequence();
        dodgeSequence.Append(transform.DOMove(dodgePosition, 0.1f).SetEase(Ease.OutQuad)); // Lùi ra 0.1s
        dodgeSequence.AppendInterval(0.05f); // Dừng 0.05s
        dodgeSequence.Append(transform.DOMove(originalPosition, 0.1f).SetEase(Ease.InQuad)); // Về 0.1s

        // (Tùy chọn: Thêm hiệu ứng khác như làm mờ nhẹ?)
        // if (characterImage != null)
        // {
        //     dodgeSequence.Insert(0, characterImage.DOFade(0.7f, 0.1f).SetLoops(2, LoopType.Yoyo));
        // }
    }
    #endregion
}