EXTERNAL GainStat(statName, amount)
EXTERNAL LoseMana(amount)
EXTERNAL HealHP(amount, type)
EXTERNAL HealMana(amount, type)
EXTERNAL GetSTR()
VAR player_str = 0

Books whirl and scream about tests and grades. The pressure chokes you.
~ player_str = GetSTR()
* ['I won’t fail!']
    { player_str >= 15 }
        ~ GainStat("STR", 2)
        You smash a book—strength surges! (+2 STR)
    { player_str < 15 }
        ~ LoseMana(15)
        Panic drains you. (-15 Mana)
    -> END

* ['I need a break.']
    ~ HealHP(30, "FLAT")
    ~ HealMana(30, "FLAT")
    You breathe slowly, accepting anxiety. (+30 HP, +30 Mana)
    -> END

* [Hide in the corner.]
    You wait until the books pass.
    -> END