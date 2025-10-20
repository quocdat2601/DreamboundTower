EXTERNAL GainItem(itemName)
EXTERNAL LoseHP(amount, type)
EXTERNAL Name(charName)
EXTERNAL ClearName()

A towering mirror shows your reflection… grinning mockingly. 
#Name Reflection
'So weak.'
#End

* [Shatter the mirror.]
    ~ GainItem("Blade of Nightmares")
    Shards swirl and forge a Blade of Nightmares.
    -> END

* [Look away.]
    ~ LoseHP(25, "PERCENT")
    The voice gnaws at you. (-25% current HP)
    -> END