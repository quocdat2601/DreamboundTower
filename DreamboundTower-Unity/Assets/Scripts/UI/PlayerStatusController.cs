using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatusController : MonoBehaviour
{
    [Header("Player Info")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;
    public Slider manaSlider;
    public TextMeshProUGUI manaText;

    [Header("Steadfast Heart")]
    public Sprite redHeartSprite;
    public Sprite blackHeartSprite;
    public Image[] heartIcons;

    [Header("Gold Display")]
    public TextMeshProUGUI goldValueText;
    
    private float lastShieldUpdate = 0f;
    private const float SHIELD_UPDATE_INTERVAL = 0.2f;

    // ✅ OnEnable() vẫn hữu ích để cập nhật giao diện mỗi khi nó được bật lên
    void OnEnable()
    {
        // Cố gắng cập nhật ngay lập tức với dữ liệu hiện có
        if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            var playerData = GameManager.Instance.currentRunData.playerData;
            // Đảm bảo tham chiếu goldValueText được gán (trong trường hợp quên kéo trong Inspector)
            if (goldValueText == null)
            {
                var coinValueTf = transform.Find("Panel/Coin/CoinValue");
                if (coinValueTf != null)
                {
                    goldValueText = coinValueTf.GetComponent<TextMeshProUGUI>();
                }
            }

            // Kiểm tra xem playerInstance đã tồn tại chưa
            if (GameManager.Instance.playerInstance != null)
            {
                var playerChar = GameManager.Instance.playerInstance.GetComponent<Character>();
                if (playerChar != null)
                {
                    UpdateHealth(playerData.currentHP, playerChar.maxHP);
                    UpdateMana(playerData.currentMana, playerChar.mana);
                }
            }
            else
            {
                // Nếu chưa có người chơi, hiển thị trạng thái mặc định/trống
                UpdateHealth(0, 1);
                UpdateMana(0, 1);
            }

            // Luôn cập nhật Steadfast Heart
            UpdateSteadfastHeart(playerData.steadfastDurability);

            // Cập nhật hiển thị vàng ngay khi UI bật
            UpdateGold(playerData.gold);
        }
        
        // Force initial shield update
        lastShieldUpdate = 0f;
    }
    
    void Update()
    {
        // Update shield display every SHIELD_UPDATE_INTERVAL seconds
        if (Time.time - lastShieldUpdate > SHIELD_UPDATE_INTERVAL)
        {
            ForceUpdateHPWithShield();
            lastShieldUpdate = Time.time;
        }
    }

    // --- CÁC HÀM CẬP NHẬT GIAO DIỆN (GIỮ NGUYÊN) ---
    // GameManager sẽ gọi các hàm này từ bên ngoài

    public void UpdateHealth(int current, int max)
    {
        // Get shield value for display
        int shieldAmount = GetShieldValue();
        
        if (hpSlider != null)
        {
            hpSlider.value = current;
            
            // Update max value to show shield extension
            if (shieldAmount > 0)
            {
                hpSlider.maxValue = Mathf.Max(max, current + shieldAmount);
            }
            else
            {
                hpSlider.maxValue = max;
            }
        }
        
        if (hpText != null)
        {
            // Show HP + Shield in text
            if (shieldAmount > 0)
            {
                hpText.text = $"{current} / {max} [Shield: {shieldAmount}]";
            }
            else
            {
                hpText.text = (max > 0) ? $"{current} / {max}" : "0 / 0";
            }
        }
    }
    
    /// <summary>
    /// Gets the current shield value from StatusEffectManager
    /// </summary>
    private int GetShieldValue()
    {
        if (GameManager.Instance?.playerInstance == null) return 0;
        
        var character = GameManager.Instance.playerInstance.GetComponent<Character>();
        if (character == null) return 0;
        
        return ShieldEffectHandler.GetShieldAmount(character);
    }
    
    /// <summary>
    /// Force refresh HP display with shield
    /// </summary>
    private void ForceUpdateHPWithShield()
    {
        if (GameManager.Instance?.playerInstance != null)
        {
            var character = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (character != null)
            {
                UpdateHealth(character.currentHP, character.maxHP);
            }
        }
    }

    public void UpdateMana(int current, int max)
    {
        if (manaSlider != null)
        {
            manaSlider.maxValue = max;
            manaSlider.value = current;
        }
        if (manaText != null)
        {
            // Đảm bảo max không bằng 0 để tránh lỗi chia cho 0
            manaText.text = (max > 0) ? $"{current} / {max}" : "0 / 0";
        }
    }

    public void UpdateSteadfastHeart(int durability)
    {
        for (int i = 0; i < heartIcons.Length; i++)
        {
            if (heartIcons[i] != null) // Thêm kiểm tra an toàn
            {
                heartIcons[i].sprite = (i < durability) ? redHeartSprite : blackHeartSprite;
            }
        }
    }

    public void UpdateGold(int amount)
    {
        if (goldValueText != null)
        {
            goldValueText.text = amount.ToString();
        }
    }
}