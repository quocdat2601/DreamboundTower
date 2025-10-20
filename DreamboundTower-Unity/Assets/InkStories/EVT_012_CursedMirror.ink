EXTERNAL GainRandomStat(amount)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL GainItem(itemName)
EXTERNAL GetSTR()
VAR player_str = 0

A tall mirror shows a darker you.
~ player_str = GetSTR()

* [Touch the mirror.]
    ~ GainRandomStat(2)
    ~ AddDebuff("HEAL_MINUS_10", 3)
    +2 random stats; Healing received -10% for 3 fights.
    -> END

* { player_str >= 12 } [Break it (STR≥12).]
    ~ GainItem("Mirror Shard_Rare")
    You pry a rare shard from the frame.
    -> END

* [Walk away.]
    Nothing happens.
    -> END