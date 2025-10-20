EXTERNAL GainGold(amount)
EXTERNAL SetFlag(flagName)
EXTERNAL GetSTR()
VAR player_str = 0


You see another child, huddled and shivering. They look startled to see you.
~ player_str = GetSTR()
* [Offer some Gold (15g).]
    ~ GainGold(-15)
    ~ SetFlag("RIVAL_FRIENDLY")
    ~ SetFlag("MET_RIVAL")
    You offer 15 Gold. They nod thankfully and dart into the shadows.
    -> END

* [Demand their items (STR 10).]
    ~ SetFlag("MET_RIVAL")
    { player_str >= 10 }
        ~ GainGold(30)
        ~ SetFlag("RIVAL_HOSTILE")
        [STR ≥10: You scare them off, grabbing a pouch. (+30 Gold)]
    { player_str < 10 }
        ~ SetFlag("RIVAL_HOSTILE")
        [STR <10: They glare and slip away.]
    -> END

* [Ignore them.]
    ~ SetFlag("MET_RIVAL")
    You walk past, leaving them to their fate.
    -> END