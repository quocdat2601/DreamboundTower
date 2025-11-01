EXTERNAL GainGold(amount)
EXTERNAL SetFlag(flagName)
EXTERNAL GetSTR()
EXTERNAL HasGold(amount)
VAR player_str = 0
VAR has_enough_gold = false

You feel a chill in the air, a familiar sense of anxiety. In a dark corner, another child is huddled and shivering. They look startled as you approach, their face mirroring your own.
~ player_str = GetSTR()
~ has_enough_gold = HasGold(15)

* [Offer some Gold (15g).]
    ~ GainGold(-15)
    ~ SetFlag("RIVAL_FRIENDLY")
    ~ SetFlag("MET_RIVAL")
    You hold out 15 Gold. They hesitate, then quickly snatch it and dart into the shadows, a brief flicker of warmth in their eyes. (-15 Gold, Flag set: RIVAL_FRIENDLY, MET_RIVAL)
    -> END

* [Demand their items (STR 10).]
    ~ SetFlag("MET_RIVAL")
    { player_str >= 10:
        -> STR_Check_Success
    - else:
        -> STR_Check_Fail
    }

* [Ignore them.]
    ~ SetFlag("MET_RIVAL")
    You walk past, leaving them to their shivering fate. The silence is heavy with mutual neglect. (Flag set: MET_RIVAL)
    -> END

// ============= STITCHES =============
=== STR_Check_Success ===
    ~ GainGold(30)
    ~ SetFlag("RIVAL_HOSTILE")
    [STR ≥10] You step forward aggressively. They flinch, drop a small pouch, and flee. (+30 Gold, Flag set: RIVAL_HOSTILE)
    -> END

=== STR_Check_Fail ===
    ~ SetFlag("RIVAL_HOSTILE") // Still hostile even if you fail
    [STR <10] You try to be intimidating, but they just glare at you with surprising defiance before slipping away into the shadows. (Flag set: RIVAL_HOSTILE)
    -> END