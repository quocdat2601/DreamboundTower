EXTERNAL GainItem(itemName)
EXTERNAL LoseHP(amount, type)
EXTERNAL AddBuff(buffName, duration)
EXTERNAL GainGold(amount)
EXTERNAL GetINT()
EXTERNAL GetSTR()
VAR player_str = 0
VAR player_int = 0

A beautiful sword lies shattered on an altar, humming with sad energy.
 ~ player_str = GetSTR()
 ~ player_int = GetINT()
* [Repair it (STR 18).]
    { player_str >= 18 }
        ~ GainItem("Oathkeeper_Rare")
        [STR ≥18: You force the pieces together. It reforms into 'Oathkeeper'!]
    { player_str < 18 }
        ~ LoseHP(15, "FLAT")
        [STR <18: The shards cut you. (-15 HP)]
    -> END

* [Swear an oath upon it.]
    ~ AddBuff("STR_PLUS_3", 3)
    You pledge to see this through. (Buff: +3 STR for 3 fights)
    -> END

* [Salvage it (INT 15).]
    { player_int >= 15 }
        ~ GainGold(120)
        [INT ≥15: You carefully melt the metal. (+120 Gold)]
    { player_int < 15 }
        [INT <15: You can't light the forge.]
    -> END