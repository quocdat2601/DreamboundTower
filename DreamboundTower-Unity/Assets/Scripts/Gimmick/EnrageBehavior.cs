using UnityEngine;

[RequireComponent(typeof(Character))]
public class EnrageBehavior : MonoBehaviour
{
    private Character character;
    private bool isEnraged = false;
    private int strBonusApplied = 0;

    private const float ENRAGE_THRESHOLD = 0.3f; // Ngưỡng 30% HP
    private const float ENRAGE_MULTIPLIER = 0.5f; // Tăng 50% STR

    void Awake()
    {
        character = GetComponent<Character>();

        // Lắng nghe OnHealthChanged, vì nó được gọi BẤT CỨ KHI NÀO HP thay đổi
        // (bao gồm cả TakeDamage và Heal)
        character.OnHealthChanged += CheckEnrageStatus;
    }

    void OnDestroy()
    {
        if (character != null)
        {
            character.OnHealthChanged -= CheckEnrageStatus;

            // Đảm bảo gỡ buff nếu quái vật bị hủy khi đang Enrage
            if (isEnraged) RemoveEnrage();
        }
    }

    // Hàm này sẽ kiểm tra trạng thái HP mỗi khi nó thay đổi
    private void CheckEnrageStatus(int currentHP, int maxHP)
    {
        float hpPercent = (float)currentHP / maxHP;

        // TRƯỜNG HỢP 1: HP thấp VÀ chưa nổi giận -> KÍCH HOẠT
        if (hpPercent <= ENRAGE_THRESHOLD && !isEnraged)
        {
            ApplyEnrage();
        }
        // TRƯỜNG HỢP 2: HP cao VÀ đang nổi giận (do được hồi máu) -> TẮT
        else if (hpPercent > ENRAGE_THRESHOLD && isEnraged)
        {
            RemoveEnrage();
        }
    }

    private void ApplyEnrage()
    {
        isEnraged = true;

        // Tính 50% dựa trên STR GỐC (baseAttackPower) để tránh cộng dồn
        strBonusApplied = Mathf.RoundToInt(character.baseAttackPower * ENRAGE_MULTIPLIER);

        // Thêmボーナスvào STR thực chiến (attackPower)
        character.attackPower += strBonusApplied;

        Debug.Log($"<color=orange>[{character.name}] NỔI GIẬN! STR +{strBonusApplied} (Tổng: {character.attackPower})</color>");

        // (Tùy chọn: Thêm hiệu ứng hình ảnh, ví dụ: đổi màu quái sang Đỏ)
        // if (character.characterImage != null) character.characterImage.color = Color.red; 
    }

    private void RemoveEnrage()
    {
        isEnraged = false;

        // Gỡ bỏ chính xác lượngボーナスđã thêm
        character.attackPower -= strBonusApplied;

        Debug.Log($"<color=gray>[{character.name}] Hết nổi giận. STR -{strBonusApplied} (Tổng: {character.attackPower})</color>");

        strBonusApplied = 0;

        // (Tùy chọn: Hoàn lại màu quái vật)
        // if (character.characterImage != null) character.characterImage.color = Color.white;
    }
}