// File: GameEnums.cs

// Enum này định nghĩa TẤT CẢ các loại chỉ số có thể bị thay đổi trong game.
// Chúng ta thêm các chỉ số % mới vào đây.
namespace Assets.Scripts.Data
{
    public enum StatType
    {
        STR, INT, DEF, HP, MANA, AGI, None,
        ManaRegenPercent,      // % hồi năng lượng
        PhysicalDamagePercent, // % sát thương vật lý
        MagicDamagePercent,    // % sát thương phép
        LifestealPercent,      // % hút máu
        DodgeChance,           // % né tránh
        DamageReduction,       // % giảm sát thương
        LowHpPhysicalDamageBonus, // % sát thương vật lý khi HP thấp
        LowHpDamageReduction,     // % giảm sát thương khi HP thấp
        LowHpLifesteal,           // % hút máu khi HP thấp
        NonBossDamageReduction,   // % giảm sát thương từ non-boss
        LowDefDamageBonus,        // % sát thương với kẻ địch DEF thấp
        ManaRegenPerTurn,         // % hồi mana mỗi turn
        AdaptiveSpiritBonus       // +2 to all base stats (Adaptive Spirit)
    }

    // Enum này định nghĩa cách hiệu ứng được áp dụng: cộng vào hay nhân lên.
    public enum ModifierType
    {
        Additive,       // Cộng dồn (ví dụ: +10 STR)
        Multiplicative  // Nhân theo % (ví dụ: +10% STR)
    }

    // Enum này bạn đã có, nhưng để ở đây cho dễ quản lý
    public enum ResourceType { None, Mana }

    // Enum này bạn đã có
    public enum TargetType { SingleEnemy, AllEnemies, Self, Ally, AllAlly }
}