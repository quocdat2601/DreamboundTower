EXTERNAL GainGold(amount)
EXTERNAL GainItem(itemName)
EXTERNAL HealHP(amount, type)
EXTERNAL LoseHP(amount, type)
EXTERNAL GetINT()
EXTERNAL HasGold(amount)
VAR player_int = 0
VAR has_enough_gold = false

The faint sound of trickling water leads you to an ancient stone well. The air around it hums with the energy of forgotten wishes.
~ player_int = GetINT()
~ has_enough_gold = HasGold(20)

* [Throw in a coin (20g).]
	~ GainGold(-20) 
	~ GainItem("Silver Ring")
	You toss 20 gold into the darkness. A moment later, a [Silver Ring] magically rises from the water's surface!
	-> END

* [Drink from the well.]
    ~ HealHP(25, "PERCENT")
    The cool, sweet water soothes your throat and clears your mind. (Restore 25% HP)
    -> END

* [Peer into the depths.]
    { player_int >= 10: 
        -> INT_Check_Success
    - else:
        -> INT_Check_Fail
    }

=== INT_Check_Success ===
~ GainGold(100)
You focus your gaze, piercing the gloom. You spot a small leather pouch wedged in the stones just below the water line. (+100 Gold)
-> END

=== INT_Check_Fail ===
~ LoseHP(10, "FLAT")
You lean too far over the edge, lose your balance, and bump your head on the stone rim. (-10 HP)
-> END