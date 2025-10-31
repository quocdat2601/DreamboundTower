EXTERNAL GainGold(amount)
EXTERNAL SetFlag(flagName)
EXTERNAL GetSTR()
EXTERNAL HasGold(amount)
VAR player_str = 0
VAR has_enough_gold = false

You feel a chill in the air, the kind you get when you've done something wrong. A dark corner reveals another child, huddled and shivering. They look startled to see you—their reflection, perhaps.

~ player_str = GetSTR()
~ has_enough_gold = HasGold(15)

* [Offer some Gold (15g).]
    ~ GainGold(-15)
    ~ SetFlag("RIVAL_FRIENDLY")
    ~ SetFlag("MET_RIVAL")
    You offer 15 Gold. They nod thankfully, a brief flicker of warmth in their eyes, and dart into the shadows.
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
    You walk past, leaving them to their shivering fate. The silence is heavy with mutual neglect.
    -> END

// ============= STITCHES (Nhãn) RIÊNG BIỆT CHO RESULT =============
=== STR_Check_Success ===
    ~ GainGold(30)
    ~ SetFlag("RIVAL_HOSTILE")
    Your forceful presence makes them flinch. You scare them off, grabbing a pouch they dropped as they fled. (+30 Gold)
    -> END

=== STR_Check_Fail ===
    ~ SetFlag("RIVAL_HOSTILE")
    You try to look menacing, but they just glare back, a spark of defiance in their eyes, and slip away into the darkness before you can react. You gain nothing.
    -> END