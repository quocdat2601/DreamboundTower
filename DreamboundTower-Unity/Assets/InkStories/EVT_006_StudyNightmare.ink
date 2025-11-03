EXTERNAL GainStat(statName, amount)
EXTERNAL LoseMana(amount, type)
EXTERNAL HealHP(amount, type)
EXTERNAL HealMana(amount, type)
EXTERNAL GetSTR()
VAR player_str = 0
~ player_str = GetSTR()

A familiar nightmare. You're in a library where books whirl around you like a storm, their pages screaming about tests, grades, and failure. The pressure is choking.

* ['I won’t fail!'] (Requires STR 15)
    { player_str >= 15:
        -> STR_Check_Success
    - else:
        -> STR_Check_Fail
    }

* ['I need a break.']
    ~ HealHP(30, "FLAT")
    ~ HealMana(30, "FLAT")
    You close your eyes and take a slow, deep breath, accepting the anxiety instead of fighting it. The vortex of books slows and settles. (+30 HP, +30 Mana)
    -> END

* [Hide in the corner.]
    You curl up, shielding your head, and wait for the suffocating nightmare to pass on its own.
    -> END
    
=== STR_Check_Success ===
~ GainStat("STR", 2)
You roar in defiance and smash a screaming textbook out of the air. A surge of power rushes through you! (+2 STR)
-> END

=== STR_Check_Fail ===
~ LoseMana(15, "FLAT")
You try to fight back, but the pressure is too much. Panic drains your will. (-15 Mana)
-> END