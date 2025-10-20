EXTERNAL HealHP(amount, type)
EXTERNAL HealMana(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL AddDebuff(debuffName, duration)
EXTERNAL GainGold(amount)

VAR roll = 0

A perfect circle of shimmering mushrooms. It feels peaceful.

* [Step inside the circle.]
    ~ HealHP(20, "FLAT")
    ~ HealMana(20, "FLAT")
    A gentle light envelops you. (+20 HP, +20 Mana)
    -> END

* [Eat a mushroom.]
    ~ roll = RANDOM(1, 100)
    
    // SỬA LỖI: Dùng "divert" (->) để nhảy đến "knot" (phân đoạn)
    { roll <= 10:
        -> success_mushroom
    }
    { roll > 10:
        -> fail_mushroom
    }

* [Crush the mushrooms.]
    ~ GainGold(10)
    You stomp them flat and find a few coins. (+10 Gold)
    -> END


// === CÁC PHÂN ĐOẠN KẾT QUẢ ===

= success_mushroom
    ~ GainStat("ALL", 1) 
    You feel stronger! (+1 All Stats!)
    -> END

= fail_mushroom
    ~ AddDebuff("POISON", 3)
    You feel sick. (Poisoned for 3 fights!)
    -> END