using UnityEngine;

[RequireComponent(typeof(Character))]
public class BonyBehavior : MonoBehaviour
{
    private const float DAMAGE_REDUCTION_PERCENT = 0.25f; // 25%

    void Awake()
    {
        Character character = GetComponent<Character>();
        if (character != null)
        {
            // Cộng thêm 25% vào tỷ lệ giảm sát thương có sẵn
            // Dùng "+=" để nó có thể cộng dồn với các hiệu ứng khác
            character.damageReduction += DAMAGE_REDUCTION_PERCENT;

            Debug.Log($"<color=gray>[{character.name}] BONY GIMMICK: Đã kích hoạt. Giảm sát thương +25%.</color>");
        }
    }
}