// File: TooltipFormatter.cs
using Assets.Scripts.Data;
using Presets; // Để truy cập StatBlock
using UnityEngine;

public static class TooltipFormatter
{
    // Hàm này nhận vào một skill và chỉ số của nhân vật, trả về mô tả đã được định dạng
    public static string GenerateDescription(SkillData skill, StatBlock characterStats)
    {
        if (skill == null || string.IsNullOrEmpty(skill.descriptionTemplate))
        {
            return "";
        }

        string formattedDesc = skill.descriptionTemplate;

        // --- Xử lý placeholder {damage} ---
        if (formattedDesc.Contains("{damage}"))
        {
            // Lấy đúng chỉ số scale của skill (STR, INT, v.v.)
            int scalingStatValue = GetStatValue(skill.scalingStat, characterStats);

            // Tính toán sát thương dựa trên công thức của bạn: SkillBase * (1 + stat/100)
            // Vì scalingPercent của bạn là 1.0 cho 100%, chúng ta không cần chia cho 100
            float totalDamage = skill.baseDamage * (1 + (scalingStatValue * skill.scalingPercent) / 100f);

            // Làm tròn và thay thế vào mô tả
            formattedDesc = formattedDesc.Replace("{damage}", Mathf.RoundToInt(totalDamage).ToString());
        }

        // (Sau này bạn có thể thêm các placeholder khác ở đây, ví dụ {heal_amount}, {duration})

        return formattedDesc;
    }

    // Hàm phụ trợ để lấy giá trị của một chỉ số từ StatBlock
    private static int GetStatValue(StatType stat, StatBlock characterStats)
    {
        switch (stat)
        {
            case StatType.STR: return characterStats.STR;
            case StatType.INT: return characterStats.INT;
            case StatType.DEF: return characterStats.DEF;
            case StatType.HP: return characterStats.HP;
            case StatType.MANA: return characterStats.MANA;
            case StatType.AGI: return characterStats.AGI;
            default: return 0;
        }
    }
}