EXTERNAL GainStat(statName, amount)
EXTERNAL LoseMana(amount, type)
EXTERNAL HealHP(amount, type)
EXTERNAL HealMana(amount, type)
EXTERNAL GetSTR()
VAR player_str = 0
~ player_str = GetSTR()
Books whirl and scream about tests and grades. The pressure chokes you.
* ['I won’t fail!']
    { player_str >= 15:
    -> STR_Check_Success
- else:
    -> STR_Check_Fail
}

* ['I need a break.']
    ~ HealHP(30, "FLAT")
    ~ HealMana(30, "FLAT")
    You breathe slowly, accepting anxiety. (+30 HP, +30 Mana)
    -> END

* [Hide in the corner.]
    You wait until the books pass.
    -> END
    
// ============= STITCHES (Nhãn) =============
=== STR_Check_Success ===
~ GainStat("STR", 2)
You smash a book—strength surges! (+2 STR)
-> END

=== STR_Check_Fail ===
~ LoseMana(15, "FLAT")
Panic drains you. (-15 Mana)
-> END