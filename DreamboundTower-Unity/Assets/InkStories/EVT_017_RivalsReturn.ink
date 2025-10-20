EXTERNAL HealHP(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL StartCombat(combatType)
EXTERNAL HasFlag(flagName)

VAR has_rival_friendly_flag = false
VAR has_rival_hostile_flag = false

The child you met before stands in your path, looking stronger.
~ has_rival_friendly_flag = HasFlag("RIVAL_FRIENDLY")
~ has_rival_hostile_flag = HasFlag("RIVAL_HOSTILE")

* { has_rival_friendly_flag } ['We meet again.' (Friendly)]
    ~ HealHP(50, "PERCENT")
    ~ GainStat("DEF", 2)
    [Requires RIVAL_FRIENDLY flag]: They nod, share a healing salve... (+50% HP, +2 DEF)
    -> END

* { has_rival_hostile_flag } ['You again!' (Hostile)]
    ~ StartCombat("ELITE")
    // (ON_WIN_... sẽ do BattleManager xử lý)
    [Requires RIVAL_HOSTILE flag]: They draw a sharp blade... (Starts combat)
    -> END

* ['Get out of my way.']
    They sigh and step aside, disappointed.
    -> END