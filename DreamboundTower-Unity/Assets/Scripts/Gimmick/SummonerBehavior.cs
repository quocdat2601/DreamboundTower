using UnityEngine;
using System.Collections.Generic; // Cần cho List
using Presets; // Cần cho EnemyTemplateSO

[RequireComponent(typeof(Character))]
public class SummonerBehavior : MonoBehaviour
{
    private Character character;
    private List<EnemyTemplateSO> summonableEnemies; // Danh sách quái có thể gọi (BattleManager sẽ gán)
    private bool wantsToSummon = false; // Cờ báo hiệu ý định triệu hồi trong lượt này

    void Awake()
    {
        character = GetComponent<Character>();
    }

    /// <summary>
    /// Gán danh sách quái có thể triệu hồi (được gọi từ BattleManager).
    /// </summary>
    public void SetSummonList(List<EnemyTemplateSO> enemies)
    {
        summonableEnemies = enemies;
    }

    /// <summary>
    /// Hàm này được BattleManager gọi qua SendMessage("OnNewTurnStarted", currentTurn)
    /// vào ĐẦU lượt của TẤT CẢ nhân vật (bao gồm cả Enemy này).
    /// </summary>
    public void OnNewTurnStarted(int currentTurn)
    {
        // Reset ý định triệu hồi mỗi đầu lượt
        wantsToSummon = false;

        // Nếu quái vật còn sống và danh sách triệu hồi không rỗng
        if (character == null || character.currentHP <= 0 || summonableEnemies == null || summonableEnemies.Count == 0)
        {
            return;
        }

        // --- LOGIC KIỂM TRA LƯỢT VÀ TỶ LỆ ---
        // 1. Lượt hiện tại có chia hết cho 3 không?
        if (currentTurn > 0 && currentTurn % 3 == 0)
        {
            // 2. Tung xúc xắc 50%
            if (Random.value <= 0.8f)
            //if (Random.value <= 1f) //for testing ginmmick summoner 
            {
                // Nếu cả 2 điều kiện đúng -> Đặt cờ muốn triệu hồi
                wantsToSummon = true;
                Debug.Log($"<color=#ADD8E6>[{character.name}] SUMMONER: Quyết định triệu hồi ở Lượt {currentTurn}.</color>");
            }
            // else { Debug.Log($"[{character.name}] SUMMONER: Lượt {currentTurn} hợp lệ nhưng roll 50% thất bại."); }
        }
        // else { Debug.Log($"[{character.name}] SUMMONER: Lượt {currentTurn} không phải lượt triệu hồi."); }
        // --- KẾT THÚC LOGIC ---
    }

    /// <summary>
    /// BattleManager sẽ gọi hàm này trong EnemyTurnRoutine để hỏi xem Summoner có muốn triệu hồi không.
    /// Nếu có, nó sẽ "tiêu thụ" ý định đó (đặt lại cờ thành false).
    /// </summary>
    /// <returns>True nếu muốn triệu hồi, False nếu không.</returns>
    public bool TryConsumeSummonIntent()
    {
        if (wantsToSummon)
        {
            wantsToSummon = false; // Tiêu thụ ý định
            return true;          // Trả về true
        }
        return false; // Mặc định là false
    }

    /// <summary>
    /// Trả về danh sách quái có thể triệu hồi (để BattleManager sử dụng).
    /// </summary>
    public List<EnemyTemplateSO> GetSummonableEnemies()
    {
        return summonableEnemies;
    }
}