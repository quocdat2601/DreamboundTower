EXTERNAL GainRandomStat(amount)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL GainItem(itemName)
EXTERNAL GetSTR()
VAR player_str = 0
~ player_str = GetSTR()
A tall mirror shows a darker you.

* [Touch the mirror.]
    ~ GainRandomStat(2)
    ~ AddDebuff("HEAL_MINUS_10", 3)
    +2 random stats; Healing received -10% for 3 fights.
    -> END

* [Break it (STR≥12).]

{ player_str >= 12:
    -> STR_Check_Success
- else:
    -> STR_Check_Fail
}

// ============= STITCHES (Nhãn) =============
=== STR_Check_Success ===
~ GainItem("Mirror Shard_Rare")
You pry a rare shard from the frame.
-> END

=== STR_Check_Fail ===
// Không làm gì (NONE)
-> END