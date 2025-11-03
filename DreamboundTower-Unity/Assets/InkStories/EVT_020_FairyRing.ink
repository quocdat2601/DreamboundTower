EXTERNAL HealHP(amount, type)
EXTERNAL HealMana(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL GainGold(amount)

VAR roll = 0

You come across a perfect circle of shimmering, pale-blue mushrooms. The air within feels peaceful and oddly still.

* [Step inside the circle.]
    ~ HealHP(20, "FLAT")
    ~ HealMana(20, "FLAT")
    You step inside. A gentle, cooling light envelops you, easing your fatigue. (+20 HP, +20 Mana)
    -> END

* [Eat a mushroom.]
    ~ roll = RANDOM(1, 100)
    { roll <= 10:
        -> success_mushroom
    - else:
        -> fail_mushroom
    }

* [Crush the mushrooms.]
    ~ GainGold(10)
    You stomp them flat. Underneath, you find a few coins someone must have dropped. (+10 Gold)
    -> END


// === STITCHES ===

= success_mushroom
    ~ GainStat("ALL", 1) 
    It tastes strange... but you suddenly feel stronger, faster, and clearer-headed! (+1 All Stats!)
    -> END

= fail_mushroom
    ~ AddDebuff("POISON", 3)
    A terrible, bitter taste fills your mouth. You feel sick. (Poisoned for 3 fights!)
    -> END