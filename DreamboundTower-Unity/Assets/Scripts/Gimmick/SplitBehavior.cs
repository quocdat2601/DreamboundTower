using UnityEngine;
using Presets;

[RequireComponent(typeof(Character))]
public class SplitBehavior : MonoBehaviour
{
    [Header("Split Config")]
    [Tooltip("Template của loại quái nhỏ hơn sẽ được tạo ra")]
    public EnemyTemplateSO smallerEnemyTemplate;

    [Tooltip("Ngưỡng máu để tách ra, 0.5 = 50%")]
    [Range(0.1f, 0.9f)]
    public float splitHealthThreshold = 0.5f;

    private Character character;
    private bool hasSplit = false;

    void Awake()
    {
        character = GetComponent<Character>();
        if (character != null)
        {
            character.OnDamaged += HandleDamage;
        }
    }

    private void HandleDamage(Character attacker)
    {
        // Chỉ tách một lần duy nhất
        if (hasSplit) return;

        // Kiểm tra xem máu có dưới ngưỡng không
        if ((float)character.currentHP / character.maxHP <= splitHealthThreshold)
        {
            hasSplit = true;
            Debug.Log($"<color=blue>{gameObject.name} đã tách ra!</color>");

            // Ở đây bạn sẽ cần gọi một hàm trong BattleManager để tạo ra 2 con quái mới.
            // Chúng ta sẽ thêm hàm này vào BattleManager ở bước sau.
            // BattleManager.Instance.SpawnAdditionalEnemies(smallerEnemyTemplate, 2);

            // Sau khi tách, con quái gốc sẽ tự chết (gây 1 lượng sát thương cực lớn)
            character.TakeDamage(99999, null);
        }
    }

    private void OnDestroy()
    {
        if (character != null)
        {
            character.OnDamaged -= HandleDamage;
        }
    }
}