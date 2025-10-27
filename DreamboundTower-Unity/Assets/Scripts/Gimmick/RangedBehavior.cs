using UnityEngine;

[RequireComponent(typeof(Character))]
public class RangedBehavior : MonoBehaviour
{
    private Character character;

    void Awake()
    {
        character = GetComponent<Character>();
    }

    // Hàm này sẽ được BattleManager tự động gọi bằng SendMessage
    public void OnNewTurnStarted(int currentTurn)
    {
        // Logic: Bất tử ở Lượt 1 HOẶC các lượt chia hết cho 5
        if (currentTurn == 1 || currentTurn % 5 == 0)
        {
            character.isInvincible = true;
            Debug.Log($"<color=cyan>[{character.name}] RANGED GIMMICK: Bất tử ở Lượt {currentTurn}</color>");

            // (Tùy chọn: Thêm hiệu ứng hình ảnh ở đây, ví dụ: bật 1 icon khiên)
        }
        else
        {
            character.isInvincible = false;

            // (Tùy chọn: Tắt hiệu ứng hình ảnh ở đây)
        }
    }
}