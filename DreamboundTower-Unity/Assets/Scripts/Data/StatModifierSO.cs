using Assets.Scripts.Data; 
using UnityEngine;

[CreateAssetMenu(menuName = "DBT/Stat Modifier")]
public class StatModifierSO : ScriptableObject
{
    [Tooltip("Chỉ số sẽ bị thay đổi")]
    public StatType targetStat;

    [Tooltip("Giá trị thay đổi (ví dụ: 8 cho +8%, 10 cho +10 STR)")]
    public float value;

    [Tooltip("Loại thay đổi: Cộng dồn hay Nhân theo %")]
    public ModifierType type;

    // ✅ THÊM DÒNG NÀY VÀO
    [Tooltip("Thời gian hiệu lực của hiệu ứng (tính bằng số lượt). Đặt là 0 cho các hiệu ứng vĩnh viễn như cộng chỉ số từ trang bị.")]
    public int duration;
}