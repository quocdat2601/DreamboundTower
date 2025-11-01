EXTERNAL GainGold(amount)
EXTERNAL LoseHP(amount, type)
EXTERNAL GainRelic(relicName)
EXTERNAL Name(charName)
EXTERNAL ClearName()
EXTERNAL HasGold(amount)

VAR roll = 0
VAR has_enough_gold = false
~ has_enough_gold = HasGold(50)

A sleeping stone giant, the size of a hill, cracks open one massive, mossy eye as you approach.
#Name Giant
'Gamble?' it rumbles, its voice shaking the floor. It points to a small pile of gold and a glowing relic.
#End

* [Gamble 50 Gold (50g).]
    ~ GainGold(-50)
    ~ roll = RANDOM(1, 100)
    { roll <= 50:
        -> win_gold
    }
    { roll > 50:
        -> lose_gamble
    }

* [Gamble 10% Vitality.]
    ~ LoseHP(10, "PERCENT") // Assuming this means 10% of Max HP
    ~ roll = RANDOM(1, 100)
    { roll <= 50:
        -> win_relic
    }
    { roll > 50:
        -> lose_gamble_hp
    }

* [Refuse.]
    'Wise... or cowardly.' The giant's eye closes, and it returns to its slumber.
    -> END


// === STITCHES ===

= win_gold
    ~ GainGold(150)
    The giant rumbles, pleased by the game, and tosses you a much larger bag. (+150 Gold!)
    -> END

= win_relic
    ~ GainRelic("Aegis of Valor") // Assuming item name
    The giant nods in respect for your boldness and offers you a massive, glowing stone. (Got Aegis of Valor!)
    -> END

= lose_gamble
    The giant grunts and simply pockets your coins. You lost. (-50 Gold)
    -> END

= lose_gamble_hp
    You feel a draining sensation as the giant takes its due, but you receive nothing in return. (-10% Max HP)
    -> END