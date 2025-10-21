EXTERNAL SetFlag(flagName)
EXTERNAL GainRelic(relicName)
EXTERNAL GetINT()
VAR player_int = 0
~ player_int = GetINT()
You find a small, porcelain doll with one button eye. It seems to watch you.

* [Examine the doll (INT≥15).]
{ player_int >= 15:
    -> INT_Check_Success 
- else:
    -> INT_Check_Fail
  }
  * [Leave it.]
    You back away slowly. It's not worth the risk.
    -> END
  
=== INT_Check_Success ===
~ SetFlag("DOLL_FOUND")
-> END
=== INT_Check_Fail ===
-> END

