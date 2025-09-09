# 🏰 Dreambound Tower — Prototype

> Narrative Choice‑Driven Roguelite — Climb a dream‑forged tower, make impactful choices, and battle with dice‑based tactics.

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](https://github.com/your-org/DreamboundTower)
[![Engine](https://img.shields.io/badge/engine-Unity%206000.2.2f1-black.svg)](https://unity.com)
[![Platform](https://img.shields.io/badge/platform-Windows%20PC-orange.svg)](https://github.com/your-org/DreamboundTower)
[![Genre](https://img.shields.io/badge/genre-RPG%20%7C%20Roguelite%20%7C%20Turn--based-green.svg)](https://github.com/your-org/DreamboundTower)
[![Status](https://img.shields.io/badge/status-Prototype-purple.svg)](https://github.com/your-org/DreamboundTower)

<div align="center">

A surreal RPG roguelite blending node‑based progression, turn‑based combat, and visual‑novel choices with d20 dice checks. Built with Unity 6000.2.2f1.

</div>

---

## 🌟 What is Dreambound Tower?
You play as a boy falling into a surreal dream world—a colossal tower built from cherished memories and childhood nightmares. Explore node‑based maps (Slay‑the‑Spire inspire), make meaningful choices, and fight turn‑based battles where actions resolve via d20 rolls (hit/graze/crit/effects). Climb toward floor 100 to uncover the truth behind the dream and yourself.

### ✨ Key Features
- **🎲 Dice‑based Combat**: Choose actions first, then roll d20 for Hit/Graze/Crit/Effects
- **🗺️ Node‑based Exploration**: Combat, Events, Shops, Rests, and Treasures across zones
- **🧠 Choice‑Driven Events**: Visual‑novel style cards with stat checks and branching outcomes
- **🧬 RNG Character Creation**: Spin a wheel for Race, Archetype, and base stats (limited rerolls)
- **❤️ Steadfast Heart**: A 3‑charge run‑saving mechanic restored at checkpoints
- **👹 Vietnamese Urban‑Legend Bosses**: From Ông Ba Bị to Ma Lai and beyond

---

## 🎮 Core Gameplay Loop
1. **Map** → Pick a route across 4–6 nodes per floor
2. **Encounter** → Combat (turn‑based dice) or Event (choice + check)
3. **Reward** → Loot, items, upgrades
4. **Progress** → Toward boss floors (10/20/…/100)
5. **Checkpoint** → Auto‑save at floors 1/11/21/… (restore Steadfast Heart)

> Baseline balance: Hit ≈ 70–80%, Crit ≈ 10–15%, Graze margin = 2, Dodge cap = 70%

---

## 📖 Story & Lore (brief)
A bright childhood dims: distant parents, scattered friends, pressure, fear of abandonment, and loss. To hide, the boy forges a dream tower—only to face nightmares within. The prologue reveals a shadowy figure (his future self) who kills him, proving pain is real here. In the void, a small fairy (later: his lost sister) introduces the system: race, archetype, stats, RNG creation, and a 100‑floor goal.

- **Boss inspirations (VN urban legends)**: Ông Ba Bị, Ma Dã, Ma Thần Vòng, Quỷ Nhập Tràng, Ma Tràng, Ma Gà, Ma Lai, Thần Trùng, Ma Vú Dài (+ a personal twist boss)
- **Prototype bosses**: T10, T20, T40 (T40 scripted defeat for story trigger)

---

## 🧩 Systems Overview

### 🧬 Character RNG
- **Races**: Human, Demon, Celestial, Undead, Beastfolk
- **Archetypes**: Warrior, Rogue, Mage, Cleric
- Wheel rolls base stats 1–10 with limited rerolls

### 📊 Stats
HP, STR, DEF, MANA, INT, AGI, LUK, Focus. Level → EXP → Skill Points (1:1 to stats or unlock skills).

### ⚔️ Combat (d20 checks)
- Hit DC = 8 + floor(target.EVA/5)
- Graze if within 2 of DC (50% damage, no effect)
- Crit: natural 20 or d20 + LUK ≥ threshold; natural 1 can auto‑miss
- Advantage/Disadvantage: roll 2, take best/worst; spend Focus for advantage/reroll
- Fail‑forward: heavy fail grants a small benefit (e.g., +1 Focus or +1 Mana)

### 🩸 Status & Items
- Status: Bleed, Burn, Shock, Fear, Guard, Aegis, Mark, Stun (StartTurn → Action → EndTurn)
- Items: common stat boosts; rare/epic affixes; weapons grant skills/passives

### 🗒️ Events (VN style)
- Event cards with 2–3 choices; d20 + statMod vs DC → outcomes (stats, items, curses, routes)
- Race‑specific events add personalization

### ❤️ Steadfast Heart
- Starts with 3 durability; lose 1 on defeat. At 0 → run ends
- Restored to 3 at checkpoints (1/11/21/…)
- Optional passives: immunity to Panic/Despair, reduced Fear duration, minor morale resist
- UI: a 3‑light heart widget

---

## 🛠️ Tech & Data
- **Engine**: Unity 6000.2.2f1 (ea398eefe1c2)
- **Architecture**: GameStateMachine (Menu/Map/Combat/Event/Cutscene), TurnManager, ActionResolver
- **DiceService**: deterministic seeds for reproducible runs
- **Data‑Driven**: ScriptableObjects for Skills, Races, Enemies, Events
- **Save**: JSON for run state + meta unlocks
- **Audio/VFX**: lightweight animators, simple pooling

### ScriptableObject Schemas (high‑level)
- SkillData: id, name, icon, cost, resource, damage, hit bonus, crit threshold, statuses, target, desc
- RaceData: id, name, base stat mods, innate skills, lore
- EnemyData: id, name, base stats, skills, phases, portrait
- EventCardData: id, title, description, choices (checks + outcomes)

---

## 🚀 Getting Started (Dev)
- Requirements: Unity 6000.2.2f1, Git LFS (recommended for large assets)
- Clone and open the `DreamboundTower` folder in Unity Hub
- Recommended Editor settings: Visible Meta Files + Force Text

### Project Structure
- `DreamboundTower/Assets` — assets and scripts
- `DreamboundTower/Packages` — package manifest and lock
- `DreamboundTower/ProjectSettings` — editor/project settings

### Build Targets
- **Windows**: URP 2D defaults

---

## 🗺️ Roadmap (6‑week prototype)
- [ ] W1–W2: Core combat (DiceService, TurnManager), basic UI, placeholders
- [ ] W3: Map/node system, EventSystem, RNG wheel UI
- [ ] W4: Boss T10 + T20, items/inventory, save system
- [ ] W5: T40 scripted defeat, fairy intro, Steadfast Heart
- [ ] W6: Polish, playtest, bugfix, demo build

## 👥 Team Roles (suggested for 5)
- Lead Gameplay Programmer — TurnManager, DiceService, ActionResolver, Status
- Tools & Data Engineer — SOs, Save/Load, Addressables
- UI/UX Programmer — Map UI, Combat HUD, RNG wheel, Event cards
- Content Designer/Balancer — skills, events, enemies, bosses
- Artist/Animator — placeholders → art pack integration (Kenney/Itch), VFX, portraits

## 🤝 Contributing
PRs welcome for content (events, skills), systems, UI, and balancing. Please open issues for discussions first.

## 🙏 Credits
Prototype for educational purposes. For placeholders, consider assets from Kenney, Itch.io, and OpenGameArt (respect licenses).

## 🔗 Useful Links
- Design highlights: see sections above
- Future docs folder suggestion: `Docs/` for boss sheets, schemas, checklists, asset sources

---
<div align="center">

**Climb the dream. Face the nightmare. Find yourself.**

</div> 
