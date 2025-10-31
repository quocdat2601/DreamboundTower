EXTERNAL GainGold(amount)
EXTERNAL GainItem(itemName)
EXTERNAL HealHP(amount, type)
EXTERNAL LoseHP(amount, type)
EXTERNAL GetINT()
EXTERNAL HasGold(amount)
VAR player_int = 0
VAR has_enough_gold = false

An ancient stone well hums with forgotten wishes.
~ player_int = GetINT()
~ has_enough_gold = HasGold(20)

* [Throw in a coin (20g).]
	~ GainGold(-20) 
	~ GainItem("Silver Ring")
	You toss 20 gold. A Silver Ring rises from the water!
	-> END

* [Drink from the well.]
 ~ HealHP(25, "PERCENT")
 Sweet water soothes you. (Restore 25% HP)
-> END

* [Peer into the depths.]
// BƯỚC 1: Sử dụng cấu trúc if/else để NHẢY TỚI nhãn, không có text output

{ player_int >= 10: 
    -> INT_Check_Success
- else:
    -> INT_Check_Fail
}


// ============= STITCHES (Nhãn) RIÊNG BIỆT CHO RESULT =============
=== INT_Check_Success ===
~ GainGold(100)
You spot a pouch wedged in the stones. (+100 Gold)
-> END // Kết thúc luồng

=== INT_Check_Fail ===
~ LoseHP(10, "FLAT")
You slip and bump your head. (-10 HP)
-> END // Kết thúc luồng