EXTERNAL GainRandomStat(numStats, amountPerStat)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL GainItem(itemName)
EXTERNAL GetSTR()
VAR player_str = 0
~ player_str = GetSTR()

You find another tall mirror, this one framed in tarnished, coiling silver. Your reflection is clear, but it isn't you. It's darker, colder... and it's smiling.

* [Touch the mirror.]
    ~ GainRandomStat(2, 2)
    ~ AddDebuff("HEAL_MINUS_10", 3)
    You reach out. Your reflection's hand meets yours at the glass. A jolt of cold power flows into you, but you feel a creeping, parasitic sickness. (+2 Random Stats; Curse: Healing received -10% for 3 fights)
    -> END

* [Break it (STR≥12).]
{ player_str >= 12:
    -> STR_Check_Success
- else:
    -> STR_Check_Fail
}

* [Walk away.]
    You ignore the reflection and walk away, its silent, mocking grin following you until you turn the corner. Nothing happens.
-> END


// ============= STITCHES =============
=== STR_Check_Success ===
~ GainItem("Mirror Shard_Rare")
You smash the glass. Your reflection shrieks. Among the shards, you find one that pulses with dark energy. (Got [Rare: Mirror Shard])
-> END

=== STR_Check_Fail ===
You strike the glass, but it doesn't even crack. Your reflection just laughs at the feeble attempt.
-> END