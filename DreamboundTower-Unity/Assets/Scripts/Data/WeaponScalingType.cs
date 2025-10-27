// File: WeaponScalingType.cs
public enum WeaponScalingType
{
    None,
    STR,    // Chỉ scale theo STR (100%)
    INT,    // Chỉ scale theo INT (100%)
    Hybrid // Scale theo cả STR và INT (ví dụ: 70% STR + 30% INT)
}