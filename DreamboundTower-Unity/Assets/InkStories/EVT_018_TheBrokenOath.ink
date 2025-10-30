EXTERNAL GainItem(itemName)
EXTERNAL LoseHP(amount, type)
EXTERNAL AddBuff(buffName, duration)
EXTERNAL GainGold(amount)
EXTERNAL GetINT()
EXTERNAL GetSTR()
VAR player_str = 0
VAR player_int = 0

A beautiful sword lies shattered on an altar, humming with a sad, broken energy.
 ~ player_str = GetSTR()
 ~ player_int = GetINT()
 
* [Repair it (STR 18).]
    { player_str >= 18:
        -> Repair_Success
    - else:
        -> Repair_Fail
    }

* [Swear an oath upon it.]
    ~ AddBuff("STR_PLUS_3", 3)
    You pledge to see this through, using the broken blade as a symbol of your resolve. A ghostly energy strengthens your arm. (Buff: +3 STR for 3 fights)
    -> END

* [Salvage it (INT 15).]
    { player_int >= 15:
        -> Salvage_Success
    - else:
        -> Salvage_Fail
    }

// ============= STITCHES =============
=== Repair_Success ===
    ~ GainItem("Arcane Blade")
    [STR ≥18] You focus your strength and grip the pieces, forcing them together. The blade magically reforms in your hand! (Got Arcane Blade)
    -> END

=== Repair_Fail ===
    ~ LoseHP(15, "FLAT")
    [STR <18] You try, but the magical shards are sharp and resist your grip, cutting your hands. (-15 HP)
    -> END
    
=== Salvage_Success ===
    ~ GainGold(120)
    [INT ≥15] You recognize the valuable enchanted metal and find a way to carefully melt it down for scrap. (+120 Gold)
    -> END
    
=== Salvage_Fail ===
    [INT <15] You can't figure out how to work the strange metal. You give up.
    -> END