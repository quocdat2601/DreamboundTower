EXTERNAL HealMana(amount, type)
EXTERNAL SetFlag(flagName)
EXTERNAL Name(charName)
EXTERNAL ClearName()

A small shadow in the corner shivers—it looks exactly like you as a child. 
#Name Shadow
'I'm scared…' it whispers.
#End

* ['It's okay. I'm here.']
    ~ HealMana(20, "FLAT")
    ~ SetFlag("MET_SHADOW")
    You sit beside it. Fear fades; your thoughts clear. (+20 Mana). It smiles.
    -> END

* ['Stop being a coward.']
    The shadow recoils and melts into darkness.
    -> END