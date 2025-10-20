EXTERNAL SetFlag(flagName)
EXTERNAL GainRelic(relicName)

You find a small, porcelain doll with one button eye. It seems to watch you.

* [Take the doll.]
    ~ SetFlag("HAS_DOLL")
    ~ GainRelic("CreepyDoll")
    You pick it up. A faint chill runs down your spine. (Gained Relic: Creepy Doll)
    -> END

* [Leave it.]
    You back away slowly. It's not worth the risk.
    -> END