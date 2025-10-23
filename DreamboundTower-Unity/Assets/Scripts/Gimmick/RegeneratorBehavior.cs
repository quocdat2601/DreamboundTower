using UnityEngine;

[RequireComponent(typeof(Character))]
public class RegeneratorBehavior : MonoBehaviour
{
    private Character character;
    private const float HEAL_PERCENT = 0.10f; // 10%

    void Awake()
    {
        character = GetComponent<Character>();
    }

    // Hàm này được BattleManager gọi qua SendMessage("OnNewTurnStarted", currentTurn)
    // (Chúng ta đã thêm SendMessage này khi làm Gimmick "Ranged")
    public void OnNewTurnStarted(int currentTurn)
    {
        // Nếu quái vật còn sống
        if (character == null || character.currentHP <= 0)
        {
            return;
        }

        // Logic: Hồi máu VÀO MỖI LƯỢT CHẴN (2, 4, 6, 8...)
        if (currentTurn > 0 && currentTurn % 2 == 0)
        {
            // Tính toán 10% máu tối đa
            int healAmount = Mathf.RoundToInt(character.maxHP * HEAL_PERCENT);

            // Hồi máu
            character.Heal(healAmount);

            Debug.Log($"<color=green>[{character.name}] REGENERATOR: Hồi {healAmount} HP (Lượt {currentTurn})</color>");

            // (Tùy chọn: Hiển thị số máu hồi màu xanh lá)
            if (CombatEffectManager.Instance != null)
            {
                // Bạn có thể cần thêm hàm này vào CombatEffectManager
                // CombatEffectManager.Instance.ShowHealingText(character, healAmount);
            }
        }
    }
}