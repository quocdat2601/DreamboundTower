EXTERNAL GainGold(amount)
EXTERNAL GainItem(itemName)
EXTERNAL HealHP(amount, type)
EXTERNAL LoseHP(amount, type)
EXTERNAL GetINT()
VAR player_int = 0

An ancient stone well hums with forgotten wishes.
~ player_int = GetINT()

* [Throw in a coin.]
    ~ GainGold(-20) // Dùng số âm cho LoseGold
    ~ GainItem("Silver Ring")
    You toss 20 gold. A Silver Ring rises from the water!
    -> END

* [Drink from the well.]
    ~ HealHP(25, "PERCENT")
    Sweet water soothes you. (Restore 25% HP)
    -> END

* [Peer into the depths.]

    
    { player_int >= 10 }
        ~ GainGold(100)
        You spot a pouch wedged in the stones. (+100 Gold)
    { player_int < 10 }
        ~ LoseHP(10, "FLAT")
        You slip and bump your head. (-10 HP)
    -> END