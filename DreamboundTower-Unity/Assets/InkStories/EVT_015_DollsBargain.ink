EXTERNAL LoseHP(amount, type)
EXTERNAL GainStat(statName, amount)
EXTERNAL SetFlag(flagName)
EXTERNAL RemoveFlag(flagName)
EXTERNAL HealHP(amount, type)
EXTERNAL Name(charName)
EXTERNAL ClearName()

The doll appears one last time. Its button eye seems to glow with an eerie, hungry light.
#Name Doll
'One final offering. Give me half of your heart, and I will give you true power. A small price, yes?'
#End

* [Accept the pact.]
    ~ LoseHP(50, "PERCENT") // Assuming this is 50% of Max HP, not current
    ~ GainStat("STR", 5)
    ~ GainStat("INT", 5)
    ~ SetFlag("DOLL_PACT")
    You nod. The world dissolves in searing pain as it takes its price... but when you recover, you feel an immense, terrifying power. (-50% Max HP, +5 STR, +5 INT, Flag set: DOLL_PACT)
    -> END

* [Refuse and cast it out.]
    ~ RemoveFlag("HAS_DOLL")
    ~ RemoveFlag("FED_DOLL")
    ~ HealHP(30, "PERCENT")
    'No.' You snatch the doll and hurl it into a chasm. As it falls, you feel a weight lift from your shoulders, a strange sense of relief. (Heal 30% HP, All doll flags removed)
    -> END