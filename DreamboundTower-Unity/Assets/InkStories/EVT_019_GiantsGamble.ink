EXTERNAL GainGold(amount)
EXTERNAL LoseHP(amount, type)
EXTERNAL GainRelic(relicName)
EXTERNAL Name(charName)
EXTERNAL ClearName()

VAR roll = 0

#Name Giant
'Gamble?'
#End

* [Gamble 50 Gold.]
    ~ GainGold(-50)
    ~ roll = RANDOM(1, 100)
    
    // SỬA LỖI: Dùng "divert"
    { roll <= 50:
        -> win_gold
    }
    { roll > 50:
        -> lose_gamble
    }

* [Gamble 10% Vitality.]
    ~ LoseHP(10, "PERCENT")
    ~ roll = RANDOM(1, 100)
    
    // SỬA LỖI: Dùng "divert"
    { roll <= 50:
        -> win_relic
    }
    { roll > 50:
        -> lose_gamble
    }

* [Refuse.]
    'Wise... or cowardly.' The giant slumbers.
    -> END


// === CÁC PHÂN ĐOẠN KẾT QUẢ ===

= win_gold
    ~ GainGold(150)
    You win 150 Gold!
    -> END

= win_relic
    ~ GainRelic("GiantsHeart_Epic")
    You win an Epic Relic 'Giant's Heart'!
    -> END
    
= lose_gamble
    You lost.
    -> END