EXTERNAL GainGold(amount)
EXTERNAL GainStat(statName, amount)
EXTERNAL GainRelic(relicName)
EXTERNAL LoseHP(amount, type)
EXTERNAL GainItem(itemName)
EXTERNAL SetFlag(flagName)
EXTERNAL GetRace()
VAR player_race = ""    

A dusty altar to a goddess of light awaits an offering.
~ player_race = GetRace()
* [Offer 20 gold.]
    ~ GainGold(-20)
    ~ GainStat("DEF", 1)
    ~ GainRelic("Charm of Purity")
    You feel protected. (+1 DEF, Relic: Charm of Purity)
    -> END

* { player_race == "Celestial" } [Pray silently (Celestial).]
    ~ GainStat("MANA", 2)
    ~ GainStat("INT", 1)
    Grace fills you. (+2 MANA, +1 INT)
    -> END

* { player_race == "Demon" } [Desecrate the shrine (Demon).]
    ~ LoseHP(10, "FLAT")
    ~ GainStat("STR", 2)
    ~ GainItem("Sinbrand Dagger")
    ~ SetFlag("SHRINE_DESECRATED")
    Power answers blasphemy. (-10 HP, +2 STR, Rare dagger, unlock retribution)
    -> END