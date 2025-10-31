EXTERNAL LoseHP(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL SetFlag(flagName)
EXTERNAL GainGold(amount)
EXTERNAL RemoveFlag(flagName)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL Name(charName)
EXTERNAL ClearName()
EXTERNAL HasGold(amount)

VAR has_enough_gold = false
~ has_enough_gold = HasGold(30)

#Name Doll
'I know a secret... if you feed me.'
#End

* [Feed it 15 HP.]
    ~ LoseHP(15, "FLAT")
    ~ GainStat("INT", 2)
    ~ SetFlag("FED_DOLL")
    You feel a sharp pain as it feeds on your essence, but your mind clears. (+2 INT)
    -> END

* [Feed it 30 Gold (30g).]
    ~ GainGold(-30)
    ~ GainStat("STR", 2)
    ~ SetFlag("FED_DOLL")
    It seems to... eat the gold? You feel a surge of power. (+2 STR)
    -> END

* [Destroy the doll.]
    ~ RemoveFlag("HAS_DOLL")
    ~ AddDebuff("STR_MINUS_2", 3)
    You smash it. A faint scream echoes. (Curse: -2 STR for 3 fights)
    -> END