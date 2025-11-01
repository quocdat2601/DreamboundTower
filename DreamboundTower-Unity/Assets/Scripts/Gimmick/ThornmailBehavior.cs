using UnityEngine;

[RequireComponent(typeof(Character))]
public class ThornmailBehavior : MonoBehaviour
{
    [Tooltip("Tỷ lệ sát thương phản lại, 0.1 = 10%")]
    [Range(0f, 1f)]
    public float reflectPercent = 0.1f;

    private Character character;

    void Awake()
    {
        character = GetComponent<Character>();
        if (character != null)
        {
            // Lắng nghe sự kiện MỚI (OnDamageTaken)
            character.OnDamageTaken += HandleDamageTaken;
        }
    }

    void OnDestroy()
    {
        if (character != null)
        {
            character.OnDamageTaken -= HandleDamageTaken;
        }
    }

    // Hàm này nhận attacker VÀ lượng sát thương thực tế
    private void HandleDamageTaken(Character attacker, int damageAmount)
    {
        // Chỉ phản đòn nếu kẻ tấn công là Người chơi (isPlayer == true)
        if (attacker != null && attacker.isPlayer)
        {
            // Tính 10% sát thương nhận vào
            int recoilDamage = Mathf.RoundToInt(damageAmount * reflectPercent);

            // Luôn phản ít nhất 1 sát thương nếu có
            if (recoilDamage < 1 && damageAmount > 0) recoilDamage = 1;

            if (recoilDamage > 0)
            {
                Debug.Log($"<color=purple>[{character.name}] THORNMAIL: Phản {recoilDamage} sát thương vào {attacker.name}</color>");

                // Bắt người chơi nhận sát thương
                // Gửi "null" làm attacker để tránh vòng lặp phản đòn vô hạn
                // Reflected damage cannot be dodged (bypassDodge = true)
                attacker.TakeDamage(recoilDamage, null, DamageType.Physical, false, bypassDodge: true);
            }
        }
    }
}