EXTERNAL StartCombat(combatType)
EXTERNAL GainGold(amount)
EXTERNAL GainStat(statName, amount)
EXTERNAL ModifySteadfast(amount)
EXTERNAL LoseHP(amount, type)

A blazing spirit confronts your sacrilege.

* [Fight the apparition.]
    ~ StartCombat("ELITE")
    // (Kết quả ON_WIN_... sẽ được xử lý bởi BattleManager)
    Defeat it to claim an Epic Relic.
    -> END

* [Repent (50g).]
    ~ GainGold(-50)
    ~ GainStat("DEF", 2)
    ~ ModifySteadfast(1)
    You atone; your guard strengthens.
    -> END

* [Flee.]
    ~ LoseHP(10, "PERCENT")
    Terror drains you. (-10% MaxHP)
    -> END