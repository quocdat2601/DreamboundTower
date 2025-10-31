EXTERNAL HealHP(amount, type)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL HealMana(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL GetINT()
VAR player_int = 0
~ player_int = GetINT()

A marble fountain glows faintly, the water itself seeming to be made of liquid dream energy.

* [Drink from it.]
    ~ HealHP(30, "PERCENT")
    You cup your hands and drink. The water is invigorating! (+30% HP);
    { RANDOM(1, 100) <= 20: 
        -> Apply_Curse 
    - else:
        -> Curse_Avoided 
    }
    
* [Channel its energy.] (Requires INT 12)
    { player_int >= 12:
        -> INT_Check_Success
    - else:
        -> INT_Check_Fail
    }

=== Apply_Curse ===
    ~ AddDebuff("DEF_MINUS_1", 3)
    ...but an icy chill follows. A minor curse afflicts you. (-1 DEF for 3 fights).
    -> END

=== Curse_Avoided ===
    ...and nothing bad happens.
    -> END

=== INT_Check_Success ===
~ HealMana(50, "PERCENT")
~ GainStat("MANA", 1)
You place your hand over the water and focus your will. The dream energy flows into you, sharpening your mind. (Restore 50% Mana, +1 Max MANA)
-> END

=== INT_Check_Fail ===
You try to channel the energy, but the flow is too chaotic and slips through your grasp. Nothing happens.
-> END