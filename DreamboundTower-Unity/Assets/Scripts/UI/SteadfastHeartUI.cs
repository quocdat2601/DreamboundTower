using UnityEngine;
using UnityEngine.UI; // Rất quan trọng, cần để dùng component Image

public class SteadfastHeartUI : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite redHeartSprite;   // Kéo file ảnh trái tim đỏ vào đây
    public Sprite blackHeartSprite; // Kéo file ảnh trái tim đen vào đây

    [Header("Heart Image Components")]
    // Kéo 3 đối tượng Heart (là các Panel/Image) vào đây
    public Image[] heartIcons;

    // Hàm này sẽ được GameManager gọi để cập nhật giao diện
    public void UpdateVisuals(int currentDurability)
    {
        // Duyệt qua tất cả các icon trái tim trong mảng
        for (int i = 0; i < heartIcons.Length; i++)
        {
            // Nếu vị trí của icon (bắt đầu từ 0) nhỏ hơn số mạng còn lại
            // Ví dụ: còn 2 mạng (durability = 2), icon 0 và 1 sẽ là tim đỏ
            if (i < currentDurability)
            {
                // Gán hình ảnh tim đỏ
                heartIcons[i].sprite = redHeartSprite;
            }
            // Ngược lại, đây là các trái tim đã mất
            else
            {
                // Gán hình ảnh tim đen
                heartIcons[i].sprite = blackHeartSprite;
            }
        }
    }
}