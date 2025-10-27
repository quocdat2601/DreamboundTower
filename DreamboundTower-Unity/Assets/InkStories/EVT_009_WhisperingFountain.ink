EXTERNAL HealHP(amount, type)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL HealMana(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL GetINT()
VAR player_int = 0
~ player_int = GetINT()

A marble fountain glows faintly with dreamlike mana.
* [Drink from it.]
    ~ HealHP(30, "PERCENT")
    You heal 30% HP;
    { RANDOM(1, 100) <= 20: 
        -> Apply_Curse 
    - else:
        -> Curse_Avoided 
    }
    
* [Channel mana.]

{ player_int >= 12:
    -> INT_Check_Success
- else:
    -> INT_Check_Fail
}

// ============= STITCHES (Nhãn) RIÊNG BIỆT CHO LỜI CHỌN 1 =============
=== Apply_Curse ===
    ~ AddDebuff("DEF_MINUS_1", 3)
    ...a curse sets in. (-1 DEF for 3 fights).
    -> END // Kết thúc luồng Choice 1

=== Curse_Avoided ===
    -> END // Kết thúc luồng Choice 1 mà không làm gì
// ============= STITCHES (Nhãn) RIÊNG BIỆT CHO RESULT =============
=== INT_Check_Success ===
~ HealMana(50, "PERCENT")
~ GainStat("MANA", 1)
Restore 50% Mana, +1 MANA
-> END

=== INT_Check_Fail ===
// Không làm gì (NONE), chỉ kết thúc
-> END