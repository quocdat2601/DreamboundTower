EXTERNAL SetFlag(flagName)
EXTERNAL GainRelic(relicName)
EXTERNAL GetINT()
VAR player_int = 0
~ player_int = GetINT()

A small, porcelain doll sits discarded in a dusty corner. One of its eyes is a black button, which seems to follow you as you move. A chill runs down your spine.

* [Examine the doll.]
{ player_int >= 15:
    -> INT_Check_Success
- else:
    -> INT_Check_Fail
}

* [Leave it.]
    This feels wrong. You back away slowly, leaving the doll to its silent watch.
    -> END

// ============= STITCHES =============
=== INT_Check_Success ===
~ SetFlag("DOLL_FOUND")
You quiet your mind and stare back into the button eye. A cold, ancient intelligence meets your gaze. It doesn't speak, but you suddenly understand what it wants... a pact. (Flag set: DOLL_FOUND)
-> END

=== INT_Check_Fail ===
You try to focus, but the doll's vacant stare is unnerving. Your mind wanders, and you learn nothing, though the feeling of being watched remains.
-> END