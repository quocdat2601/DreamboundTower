EXTERNAL GainItem(itemName)
EXTERNAL GainGold(amount)
EXTERNAL Name(charName)
EXTERNAL ClearName()

#Name Shadow
'Thank you for staying. I have something for you.'
#End

* ['What is it?']
    ~ GainItem("Amulet of Steadfast")
    It gifts you an Amulet of Steadfast. 'To guard your heart.'
    -> END

* ['I don’t need gifts.']
    ~ GainGold(150)
    It nods and points at a pile of coins. (+150 Gold)
    -> END