EXTERNAL LoseHP(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL SetFlag(flagName)
EXTERNAL RemoveFlag(flagName)
EXTERNAL HealHP(amount, type)
EXTERNAL Name(charName)
EXTERNAL ClearName()

#Name Doll
'One final offering. Give me half of your heart, and I will give you true power.'
#End

* [Accept the pact.]
    ~ LoseHP(50, "PERCENT")
    ~ GainStat("STR", 5)
    ~ GainStat("INT", 5)
    ~ SetFlag("DOLL_PACT")
    You agree. Excruciating pain... followed by immense power. (-50% Max HP, +5 STR, +5 INT)
    -> END

* [Refuse and cast it out.]
    ~ RemoveFlag("HAS_DOLL")
    ~ RemoveFlag("FED_DOLL")
    ~ HealHP(30, "PERCENT")
    You throw the doll into a chasm. You feel lighter. (Heal 30% HP)
    -> END