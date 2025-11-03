EXTERNAL GainItem(itemName)
EXTERNAL HealHP(amount, type)

A faded, old cloak hangs on a wooden stand. It smells faintly, inexplicably, like your mother’s kitchen on a rainy day.

* [Wear it.]
    ~ GainItem("Arcanum Robe")
    You pull the cloak over your shoulders. It shimmers and reforms into a fine [Arcanum Robe], the comforting scent lingering within its threads.
    -> END

* [Just smell it.]
    ~ HealHP(100, "PERCENT")
    You close your eyes and inhale the scent of home. The feeling of safety and comfort is so profound, it washes away all your wounds. (Heal 100% HP)
    -> END