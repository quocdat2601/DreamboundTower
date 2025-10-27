using UnityEngine;

// Đảm bảo đối tượng này luôn có component Character
[RequireComponent(typeof(Character))]
public class ResurrectBehavior : MonoBehaviour
{
    private Character character;
    public bool hasResurrected = false;
    private MonoBehaviour lootableScript;

    void Awake()
    {
        character = GetComponent<Character>();
        // Đăng ký để "lắng nghe" sự kiện khi nhân vật này chết
        if (character != null)
        {
            character.OnDeath += HandleDeath;
        }
    }

    private void HandleDeath(Character deadCharacter)
    {
        // Chỉ thực hiện nếu chưa hồi sinh
        if (!hasResurrected)
        {
            // Đánh dấu là đã hồi sinh để không lặp lại
            hasResurrected = true;
            Debug.Log($"<color=lime>{gameObject.name} đã hồi sinh!</color>");

            // Hồi lại 50% máu
            character.HealPercentage(0.5f);
        }
    }

    // Dọn dẹp listener khi đối tượng bị hủy để tránh lỗi
    private void OnDestroy()
    {
        if (character != null)
        {
            character.OnDeath -= HandleDeath;
        }
    }
}