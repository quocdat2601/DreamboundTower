EXTERNAL LoseHP(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL SetFlag(flagName)
EXTERNAL GainGold(amount)
EXTERNAL RemoveFlag(flagName)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL Name(charName)
EXTERNAL ClearName()

The porcelain doll appears before you again, its head tilted slightly.
#Name Doll
'I know a secret,' it whispers, its voice like cracking glass. '...if you feed me.'
#End

* [Feed it with your blood.]
    ~ LoseHP(15, "FLAT")
    ~ GainStat("INT", 2)
    ~ SetFlag("FED_DOLL")
    You offer your arm. It "drinks" from your vitality. The pain is sharp but quick, leaving your mind feeling clearer, sharper. (-15 HP, +2 INT, Flag set: FED_DOLL)
    -> END

* [Feed it 30 Gold.]
    ~ GainGold(-30)
    ~ GainStat("STR", 2)
    ~ SetFlag("FED_DOLL")
    You toss the coins at its feet. They vanish before they hit the ground, as if consumed. You feel a sudden surge of physical power. (-30 Gold, +2 STR, Flag set: FED_DOLL)
    -> END

* [Destroy the doll.]
    ~ RemoveFlag("HAS_DOLL") // Assuming DOLL_FOUND was renamed to HAS_DOLL
    ~ AddDebuff("STR_MINUS_2", 3)
    You've had enough. You smash the doll against the wall. A faint, terrible scream echoes in your head. (Curse: -2 STR for 3 fights)
    -> END