EXTERNAL SetFlag(flagName)
EXTERNAL GainRelic(relicName)
EXTERNAL GetINT()
VAR player_int = 0
~ player_int = GetINT()
You find a small, porcelain doll with button eye. It seems to watch you.

* [Examine the doll (INT≥15).]
{ player_int >= 15:
    -> INT_Check_Success
- else:
    -> INT_Check_Fail
}

// Lựa chọn thứ hai đã được tách ra để tránh lỗi cú pháp
* [Leave it.]
    You back away slowly. It's not worth the risk.
    -> END

// ============= STITCHES (Nhãn) =============
=== INT_Check_Success ===
~ SetFlag("DOLL_FOUND")
You focus your mind on the doll. Its presence clarifies, and you understand its silent demand. (Set Flag: DOLL_FOUND)
-> END

=== INT_Check_Fail ===
You try to focus, but the doll's vacant stare makes your mind wander. You feel uneasy and learn nothing.
-> END