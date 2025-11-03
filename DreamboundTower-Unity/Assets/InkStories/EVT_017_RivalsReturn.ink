EXTERNAL HealHP(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL StartCombat(combatType)
EXTERNAL HasFlag(flagName)

VAR has_rival_friendly_flag = false
VAR has_rival_hostile_flag = false

Further up the tower, you are blocked. It's the child from before. They are standing directly in your path, looking stronger and far more determined.
~ has_rival_friendly_flag = HasFlag("RIVAL_FRIENDLY")
~ has_rival_hostile_flag = HasFlag("RIVAL_HOSTILE")

* { has_rival_friendly_flag } ['We meet again.' (Friendly)]
    ~ HealHP(50, "PERCENT")
    ~ GainStat("DEF", 2)
    [Friendly] They nod, stepping aside. They share a healing salve and show you a defensive stance they learned. (+50% HP, +2 DEF)
    -> END

* { has_rival_hostile_flag } ['You again!' (Hostile)]
    ~ StartCombat("ELITE")
    // (ON_WIN_... will be handled by BattleManager)
    [Hostile] They draw a sharp, gleaming blade. 'This time, you won't bully me!' (Starts combat!)
    -> END

* ['Get out of my way.']
    They hold your gaze for a moment, sigh, and step aside. You sense their disappointment as you pass.
    -> END