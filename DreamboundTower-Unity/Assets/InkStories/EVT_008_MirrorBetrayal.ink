EXTERNAL GainItem(itemName)
EXTERNAL LoseHP(amount, type)
EXTERNAL Name(charName)
EXTERNAL ClearName()

You stand before a towering, ornate mirror. Your reflection stares back at you... and grins mockingly.
#Name Reflection
'So weak. You'll never make it.'
#End

* [Shatter the mirror.]
    ~ GainItem("Blade of Nightmares")
    You strike the glass. The shards don't fall, but swirl in a dark vortex before forging themselves into a wicked-looking [Blade of Nightmares].
    -> END

* [Look away.]
    ~ LoseHP(25, "PERCENT")
    You turn away, but the reflection's laughter echoes in your mind, gnawing at your resolve. (-25% current HP)
    -> END