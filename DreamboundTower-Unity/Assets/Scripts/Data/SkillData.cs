// File: SkillData.cs
using Assets.Scripts.Data;
using UnityEngine;

[CreateAssetMenu(menuName = "DBT/Active Skill")] // Đổi tên menu cho rõ ràng
public class SkillData : BaseSkillSO // Kế thừa từ BaseSkillSO
{
    // Các trường chung (id, displayName, icon, descriptionTemplate) đã có từ lớp cha
    // Chúng ta chỉ cần định nghĩa các trường riêng của Active Skill

    [Header("Visuals & Audio")]
    [Tooltip("Prefab chứa hiệu ứng hình ảnh (VFX) sẽ được tạo ra khi dùng skill.")]
    public GameObject vfxPrefab;

    public ResourceType resource = ResourceType.Mana;
    public int cost = 0;
    public int baseDamage = 10;
    public StatType scalingStat = StatType.STR;
    [Range(0f, 5f)]
    public float scalingPercent = 1.0f;
    public int cooldown = 0;
    public TargetType target = TargetType.SingleEnemy;
}