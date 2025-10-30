EXTERNAL StartCombat(combatType)
EXTERNAL GainGold(amount)
EXTERNAL GainStat(statName, amount)
EXTERNAL ModifySteadfast(amount)
EXTERNAL LoseHP(amount, type)

The air crackles with energy. A blinding light coalesces into an angelic apparition, its face a mask of cold fury. It points a fiery sword directly at you.
'Your sacrilege at the shrine ends here,' it booms. 'Face divine retribution!'

* [Fight the apparition.]
    ~ StartCombat("ELITE")
    // (ON_WIN_... will be handled by BattleManager)
    The spirit lunges, its holy fire searing the air. You must defeat it to claim its power.
    -> END

* [Repent (50g).]
    ~ GainGold(-50)
    ~ GainStat("DEF", 2)
    ~ ModifySteadfast(1)
    You fall to your knees, offering gold and penance. The spirit hesitates, its light softening slightly. It accepts your offering, reinforcing your defenses as it fades. (-50 Gold, +2 DEF, +1 Steadfast Heart)
    -> END

* [Flee.]
    ~ LoseHP(10, "PERCENT")
    You turn and run from the holy light. The feeling of shame and terror drains your vitality. (-10% Max HP)
    -> END