// File: SplitBehavior.cs
using UnityEngine;
using Presets;

[RequireComponent(typeof(Character))]
public class SplitBehavior : MonoBehaviour
{
    // (Biến này sẽ được BattleManager gán khi spawn quái)
    [HideInInspector]
    public EnemyTemplateSO myOriginalTemplate;

    [Tooltip("Ngưỡng máu để tách ra, 0.5 = 50%")]
    [Range(0.1f, 0.9f)]
    public float splitHealthThreshold = 0.5f;

    private Character character;
    public bool hasSplit = false;
    private BattleManager battleManager;

    void Awake()
    {
        character = GetComponent<Character>();
        if (character != null)
        {
            character.OnDamaged += HandleDamage;
        }
    }

    void Start()
    {
        // Dùng Start() để đảm bảo BattleManager đã Awake
        battleManager = FindFirstObjectByType<BattleManager>();
    }

    private void HandleDamage(Character attacker)
    {
        // Nếu đã tách, hoặc không tìm thấy manager, hoặc không có template gốc thì dừng
        if (hasSplit || battleManager == null || myOriginalTemplate == null) return;

        // Kiểm tra xem máu có dưới ngưỡng không
        if ((float)character.currentHP / character.maxHP <= splitHealthThreshold)
        {
            hasSplit = true;
            Debug.Log($"<color=cyan>{gameObject.name} đã kích hoạt Split!</color>");

            // Yêu cầu BattleManager xử lý việc tách
            battleManager.HandleSplit(character, myOriginalTemplate);

            // Hủy đăng ký ngay lập tức
            character.OnDamaged -= HandleDamage;
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