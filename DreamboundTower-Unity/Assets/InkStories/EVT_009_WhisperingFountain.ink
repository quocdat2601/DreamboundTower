EXTERNAL HealHP(amount, type)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL HealMana(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL GetINT()
VAR player_int = 0

A marble fountain glows faintly with dreamlike mana.
    ~ player_int = GetINT()
* [Drink from it.]
    ~ HealHP(30, "PERCENT")
    You heal 30% HP;
    { RANDOM(1, 100) <= 20 }
        ~ AddDebuff("DEF_MINUS_1", 3)
        ...a curse sets in. (-1 DEF for 3 fights).
    -> END

* [Channel mana.]
    { player_int >= 12 }
        ~ HealMana(50, "PERCENT")
        ~ GainStat("MANA", 1)
        Restore 50% Mana, +1 MANA
    { player_int < 12 }
        nothing
    -> END