# 🏰 Dreambound Tower — Prototype

> Narrative **roguelite RPG** — climb a dream-forged tower, make impactful choices, and fight tense **turn-based** battles.

[![Engine](https://img.shields.io/badge/Unity-6000.2.x-black.svg)]()
[![Platform](https://img.shields.io/badge/Platform-Windows%20PC-orange.svg)]()
[![Status](https://img.shields.io/badge/Status-In_Development-purple.svg)]()

---

## 🌟 Overview
**Dreambound Tower** is a turn-based roguelite set inside a child’s surreal dream.  
Progress through **100 floors** using a **node map** (Combat / Elite / Event / Shop / Rest / Treasure), make **choice-driven** decisions, and build a run with **items, relics, and stats**. Checkpoints at **1/11/21/…/91** restore your run-saving “Steadfast Heart”.  
*Design details are aligned with the current GDD.* :contentReference[oaicite:0]{index=0}

---

## 🎮 Core Features (prototype scope)
- **Node-based progression:** 4–6 nodes per floor, boss every 10 floors (10/20/…/100).
- **Turn-based combat:** clear math (no d20). STR/INT scale damage; DEF reduces damage; Crit 1.5x; Dodge from AGI (cap 40%); small chance for extra turn from AGI.
- **Choice events:** VN-style cards with stat checks, flags, and chained events.
- **Races & Classes:** 5 **Races** (Human/Demon/Celestial/Undead/Beastfolk) × 4 **Classes** (Warrior/Rogue/Mage/Cleric) with passives + 1 active.
- **Steadfast Heart:** 3 charges per segment; lose 1 on death; refills to 3 at checkpoints.

---

## ⚔️ Combat — quick math
- DamageRaw = SkillBase * (1 + ATK/100) // ATK = STR (phys) or INT (magic)
- DamageFinal = DamageRaw * (100 / (100 + DEF)) // smooth mitigation
- Crit = 1.5x // base; subject to item buffs
- Dodge = min(0.40, 0.003 * AGI) // 0.3%/AGI, cap 40%
- ExtraTurn% ≈ min(25%, 0.15% * AGI) // optional, per design

---

## 🧬 Races (base & identity)
- **Human:** balanced stats; **+5% all stats**. *Active:* **Resolve Surge** – heal 10% MaxHP & +10% dmg (CD 5).
- **Demon:** high STR; **+5% phys dmg** under 50% HP. *Active:* **Rage Break** – heavy hit, 6 Mana, CD 4, **5% MaxHP recoil**.
- **Celestial:** tanky/mage; **+5% magic dmg & mana regen**. *Active:* **Radiant Shield** – shield 15% MaxHP + reflect (CD 5).
- **Undead:** sturdy; **5% lifesteal**. *Active:* **Bone Resurge** – restore 25% HP & 1-turn stun immunity (CD 6).
- **Beastfolk:** fast; **+8% dodge**. *Active:* **Pounce** – next turn first-strike, +40% dmg (CD 3).

## 🛡️ Classes (role & active)
- **Warrior:** *Passive:* **Iron Will** −10% physical dmg taken. *Active:* **Shield Slam** – STR-scaled hit, 30% Stun (CD 3).
- **Rogue:** *Passive:* **Evasive Instinct** +6% dodge & +5% dmg vs DEF<5. *Active:* **Flurry Strike** – 3× small hits, heal 25% of dmg (CD 3).
- **Mage:** *Passive:* **Arcane Overflow** +10% Max Mana & +5% spell power. *Active:* **Arcane Burst** – INT AoE + Burn (CD 4).
- **Cleric:** *Passive:* **Divine Resilience** DR 12% & lifesteal 6% at HP≤35%. *Actives:* **Sanctified Strike** (INT hit + self-heal, CD 3) / **Sanctuary Ward** (shield, cleanse, CD 5).

---

## 💎 Items, Relics, Economy (prototype rules)
- Prices are **data-driven** from stat sums + rarity base value.
- Drops scale with floor: Normal < Elite < Boss; rarity weights bump every 10 floors.
- Gold & EXP growth per floor use smooth exponential factors (see constants).

---

## ❤️ Steadfast & Checkpoints
- **Steadfast Heart:** 3 “durability”. On defeat → −1. At 0 → run ends.
- **Checkpoints:** floors **1/11/21/…/91**. On restore: Steadfast → 3, HP to % of Max, Mana full (tunable).

---

## 🧱 Tech & Data
- **Engine:** Unity 6000.2.x, C#.
- **Architecture:** State Machine (Menu/Map/Combat/Event), TurnManager, Data-driven using **ScriptableObjects**.
- **Save:** JSON (checkpoint snapshots + meta progression).
- **Assets:** placeholder packs (Kenney / OpenGameArt / Itch/ AI).

---

## 🧪 Prototype Roadmap (internal)
- Core combat loop, HUD, and formulas
- Map/Node generation per zone (10-floor bands)
- Event system (flags/requirements/outcomes)
- Boss T10/T20 (baseline patterns), scaling tables
- Save/Load & checkpoints; Steadfast UI
- Content pass: ~25 events, ~30 items, ~10 relics

---

## 🔗 References
- **GDD (latest):** *[DreamboundTower GDD](https://docs.google.com/document/d/1H_eaLToqbxPRcF-PRwv9cIZgrl95_5Ynq3-ABvWYTXk/edit?pli=1&tab=t.0)*  
- **Data Sheets:** *[Data](https://docs.google.com/spreadsheets/d/15lJ9UKwbR84D2nuDMg84DAb7bgu6FQX3OjviFL9su_g/edit?gid=1555870868#gid=1555870868)*  

---

## 🤝 Contributing
PRs welcome (systems, content, balance).  
Please open an issue for discussion before large changes.

---

<div align="center">
Climb the dream. Face the nightmare. Find yourself.
</div>
//uyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadaduyvvbkjbjknnoifoaofiwoainfownfaoinwofnoawfnoawnfowangoiawguowoahnfoiawndiondionsadsadad
