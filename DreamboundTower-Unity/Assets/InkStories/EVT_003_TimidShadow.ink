EXTERNAL HealMana(amount, type)
EXTERNAL SetFlag(flagName)
EXTERNAL Name(charName)
EXTERNAL ClearName()

A small, indistinct shadow shivers in the corner. As you approach, it takes on a familiar shape—it looks exactly like you as a child.
#Name Shadow
'I'm... I'm scared…' it whispers, pulling its knees to its chest.
#End

* ['It's okay. I'm here.']
    ~ HealMana(20, "FLAT")
    ~ SetFlag("MET_SHADOW")
    You sit down beside it, not speaking, just offering quiet presence. The shadow's trembling subsides. Your thoughts clear, and fear fades from your own mind. (+20 Mana). It offers a weak smile.
    -> END

* ['Stop being a coward.']
    The shadow recoils as if struck, its form dissolving entirely as it melts back into the darkness.
    -> END