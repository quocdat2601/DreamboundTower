EXTERNAL GainItem(itemName)
EXTERNAL HealHP(amount, type)

An old cloak hangs on a stand; it smells like your mother’s kitchen.

* [Wear it.]
    ~ GainItem("Arcanum Robe")
    You don the cloak. It turns into an Arcanum Robe and warms your spirit.
    -> END

* [Smell it.]
    ~ HealHP(100, "PERCENT")
    Home floods back. You’re fully restored. (Heal 100% HP)
    -> END