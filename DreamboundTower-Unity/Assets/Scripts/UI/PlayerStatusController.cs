using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerStatusController : MonoBehaviour
{
    [Header("Player Info")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    [Header("Steadfast Heart")]
    public Sprite redHeartSprite;
    public Sprite blackHeartSprite;
    public Image[] heartIcons;

    // ✅ OnEnable() vẫn hữu ích để cập nhật giao diện mỗi khi nó được bật lên
    void OnEnable()
    {
        // Cố gắng cập nhật ngay lập tức với dữ liệu hiện có
        if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            var playerData = GameManager.Instance.currentRunData.playerData;

            // Kiểm tra xem playerInstance đã tồn tại chưa
            if (GameManager.Instance.playerInstance != null)
            {
                var playerChar = GameManager.Instance.playerInstance.GetComponent<Character>();
                if (playerChar != null)
                {
                    UpdateHealth(playerData.currentHP, playerChar.maxHP);
                }
            }
            else
            {
                // Nếu chưa có người chơi, hiển thị trạng thái mặc định/trống
                UpdateHealth(0, 1);
            }

            // Luôn cập nhật Steadfast Heart
            UpdateSteadfastHeart(playerData.steadfastDurability);
        }
    }

    // --- CÁC HÀM CẬP NHẬT GIAO DIỆN (GIỮ NGUYÊN) ---
    // GameManager sẽ gọi các hàm này từ bên ngoài

    public void UpdateHealth(int current, int max)
    {
        if (hpSlider != null)
        {
            hpSlider.maxValue = max;
            hpSlider.value = current;
        }
        if (hpText != null)
        {
            // Đảm bảo max không bằng 0 để tránh lỗi chia cho 0
            hpText.text = (max > 0) ? $"{current} / {max}" : "0 / 0";
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
}