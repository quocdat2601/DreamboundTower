EXTERNAL GainItem(itemName)
EXTERNAL GainGold(amount)
EXTERNAL Name(charName)
EXTERNAL ClearName()

The small shadow you met earlier appears in your path, looking slightly less afraid.
#Name Shadow
'Thank you... for staying. I have something for you. I kept it safe.'
#End

* ['What is it?']
    ~ GainItem("Amulet of Steadfast")
    It holds out a worn amulet, humming with a protective energy. 'To guard your heart.' (Got [Amulet of Steadfast])
    -> END

* ['I don’t need gifts.']
    ~ GainGold(150)
    It nods, understanding. It points instead to a pile of forgotten coins you hadn't noticed in a nearby alcove. (+150 Gold)
    -> END