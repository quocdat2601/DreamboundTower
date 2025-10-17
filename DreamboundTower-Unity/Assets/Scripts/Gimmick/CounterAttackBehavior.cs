using UnityEngine;

[RequireComponent(typeof(Character))]
public class CounterAttackBehavior : MonoBehaviour
{
    [Tooltip("Tỷ lệ phản đòn, 0 = 0%, 1 = 100%")]
    [Range(0f, 1f)]
    public float counterChance = 0.5f; // 50% tỷ lệ phản đòn

    private Character character;

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
        // Chỉ phản đòn nếu attacker tồn tại (không phải sát thương từ môi trường)
        // và roll tỷ lệ thành công
        if (attacker != null && UnityEngine.Random.value <= counterChance)
        {
            Debug.Log($"<color=red>{gameObject.name} phản đòn vào {attacker.name}!</color>");

            // Thực hiện một đòn tấn công cơ bản ngược lại kẻ đã tấn công nó
            character.Attack(attacker);
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