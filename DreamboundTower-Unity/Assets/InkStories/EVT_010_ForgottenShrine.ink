EXTERNAL GainGold(amount)
EXTERNAL GainStat(statName, amount)
EXTERNAL GainRelic(relicName)
EXTERNAL LoseHP(amount, type)
EXTERNAL GainItem(itemName)
EXTERNAL SetFlag(flagName)
EXTERNAL GetRace()

VAR player_race = ""
~ player_race = GetRace()

A small, dusty altar to a long-forgotten goddess of light awaits an offering.

* [Offer 20 gold.]
	~ GainGold(-20)
	~ GainStat("DEF", 1)
	~ GainRelic("Moonstone Charm")
	You place the coins on the altar. A faint, protective light envelops you. (-20 Gold, +1 DEF, Got [Relic: Moonstone Charm])
	-> END

* { player_race == "Celestial" } [Pray silently (Celestial).]
	~ GainStat("MANA", 2)
	~ GainStat("INT", 1)
You whisper an ancient prayer. The altar resonates with your celestial nature, blessing you. (+2 MANA, +1 INT)
	-> END

* { player_race == "Demon" } [Desecrate the shrine (Demon).]
	~ LoseHP(10, "FLAT")
	~ GainStat("STR", 2)
	~ GainItem("Sinbrand Dagger")
	~ SetFlag("SHRINE_DESECRATED")
You smash the altar. A pulse of dark energy answers your blasphemy, strengthening your rage. (-10 HP, +2 STR, Got [Sinbrand Dagger], Flag set: SHRINE_DESECRATED)
	-> END