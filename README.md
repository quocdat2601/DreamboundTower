# 🏰 Dreambound Tower — Prototype

> Narrative Choice‑Driven Roguelite — Climb a dream‑forged tower, make impactful choices, and battle with turn‑based combat.

[![Version](https://img.shields.io/badge/version-0.1.0-blue.svg)](https://github.com/your-org/DreamboundTower)
[![Engine](https://img.shields.io/badge/engine-Unity%206000.2.2f1-black.svg)](https://unity.com)
[![Platform](https://img.shields.io/badge/platform-Windows%20PC-orange.svg)](https://github.com/your-org/DreamboundTower)
[![Genre](https://img.shields.io/badge/genre-RPG%20%7C%20Roguelite%20%7C%20Turn--based-green.svg)](https://github.com/your-org/DreamboundTower)
[![Status](https://img.shields.io/badge/status-Prototype-purple.svg)](https://github.com/your-org/DreamboundTower)

<div align="center">

A surreal RPG roguelite blending node‑based progression, turn‑based combat, and visual‑novel choices. Built with Unity 6000.2.2f1.

</div>

---

## 🌟 What is Dreambound Tower?
You play as a boy falling into a surreal dream world—a colossal tower built from cherished memories and childhood nightmares. Explore node‑based maps (Slay‑the‑Spire inspired), make meaningful choices, and fight turn‑based battles with percentage-based combat mechanics. Climb toward floor 100 to uncover the truth behind the dream and yourself.

### ✨ Key Features
- **⚔️ Turn‑based Combat**: Percentage-based hit/crit/dodge system with physical and magic damage types
- **🗺️ Node‑based Exploration**: Combat, Events, Shops, Rests, and Treasures across zones
- **🧠 Choice‑Driven Events**: Visual‑novel style cards with stat checks and branching outcomes
- **🧬 RNG Character Creation**: Spin a wheel for Race, Archetype, and base stats (limited rerolls)
- **❤️ Steadfast Heart**: A 3‑charge run‑saving mechanic restored at checkpoints
- **👹 Vietnamese Urban‑Legend Bosses**: From Ông Ba Bị to Ma Lai and beyond

---

## 🎮 Core Gameplay Loop
1. **Map** → Pick a route across 4–6 nodes per floor
2. **Encounter** → Combat (turn‑based) or Event (choice + check)
3. **Reward** → Loot, items, upgrades
4. **Progress** → Toward boss floors (10/20/…/100)
5. **Checkpoint** → Auto‑save at floors 1/11/21/… (restore Steadfast Heart)

---

## ⚔️ Combat — quick math
- DamageRaw = SkillBase * (1 + ATK/100) // ATK = STR (phys) or INT (magic)
- DamageFinal = DamageRaw * (100 / (100 + DEF)) // smooth mitigation
- Crit = 1.5x // base; subject to item buffs
- Dodge = min(0.40, 0.003 * AGI) // 0.3%/AGI, cap 40%
- ExtraTurn% ≈ min(25%, 0.15% * AGI) // optional, per design

---

## 🧩 Systems Overview

### 🧬 Character RNG
- **Races**: Human, Demon, Celestial, Undead, Beastfolk
- **Archetypes**: Warrior, Rogue, Mage, Cleric

### 📊 Stats
**Core Stats**: HP, STR (Attack Power), DEF (Defense), MANA, INT (Intelligence), AGI (Agility)

- **HP**: Maximum health points
- **STR**: Base physical attack power (scales weapon damage)
- **DEF**: Damage reduction percentage (League of Legends style: `defense / (defense + 100)`, capped at 67%)
- **MANA**: Maximum mana pool for skills
- **INT**: Scales magic damage and burn effect intensity
- **AGI**: Determines dodge chance (0.3% per AGI, capped at 40% from AGI, additional bonuses from gear possible)

**Derived Stats**: Dodge Chance (from AGI + gear bonuses), Critical Chance (from gear/passives), Lifesteal (from gear/passives), Damage Reduction (from gear + DEF)

### ⚔️ Combat System
- **Turn‑based**: Player turn → Enemy turn → End of turn effects
- **Damage Types**: Physical (white), Magic (cyan), True (yellow)
- **Critical Hits**: Percentage-based chance from gear/passives (increases damage by multiplier)
- **Dodge**: Percentage-based chance from AGI + gear bonuses (prevents all damage)
- **Defense**: Percentage-based damage reduction (DEF stat + gear, max 80% total)
- **Shield**: Absorbs damage before HP, can stack
- **Reflect**: Returns percentage of damage to attacker (active while shield exists)

### 🩸 Status Effects
**Damage Over Time (DOT)**:
- **Bleed**: Physical damage per turn (scales with 12% of physical damage dealt when applied)
- **Burn**: Magic damage per turn (scales with attacker's INT)
- **Poison**: Physical damage per turn (scales with 2.5% of target's max HP when applied)

**Other Status Effects**:
- **Shield**: Absorbs damage before HP, stacks additively
- **Reflect**: Returns percentage of damage to attacker (requires active shield)
- **Stun**: Prevents actions on start of turn
- **Heal Bonus**: Increases healing effectiveness percentage
- **Pounce**: Enhances next attack with damage bonus

Status effects tick at **Start of Turn** (Stun) or **End of Turn** (DOT effects, Shield decay, etc.)

### 🎒 Items & Equipment
- **Rarities**: Common, Uncommon, Rare, Epic, Legendary
- **Gear Slots**: Weapon, Armor, Accessory (8 total slots: 1 Weapon, 1 Armor, 6 Accessories)
- **Item Effects**: Stat boosts, percentage bonuses (crit chance, lifesteal, damage reduction), flat bonuses (damage, defense), status effect procs
- **Weapons**: Grant base physical/magic damage, can have status effect procs (e.g., bleed on physical damage)
- **Passives**: Some items grant passive skills with conditional effects
- **Inventory**: 20 slots for items, drag-and-drop system for equipping

### 👹 Enemies System
- **Enemy Types**: Normal, Elite, Boss
  - **Normal**: Standard enemies with base stats
  - **Elite**: Enhanced enemies (3x HP, 1.6x STR, 1.5x DEF)
  - **Boss**: Floor-ending bosses with unique abilities

- **Enemy Scaling**: Stats scale exponentially with floor
  - Formula: `stat(floor) = base × (1 + rate)^(floor - 1) × multipliers`
  - Growth rate increases after floor 100

- **Enemy Gimmicks**: Special behaviors enemies can have
  - **Resurrect**: Revives once after death
  - **SplitOnDamage**: Splits into smaller enemies when damaged
  - **CounterAttack**: Retaliates when attacked
  - **Ranged**: Has ranged attacks (invulnerable to melee)
  - **Enrage**: Gets stronger at low HP
  - **Bony**: Takes reduced damage
  - **Thornmail**: Reflects physical damage back to attacker
  - **Regenerator**: Heals HP each turn
  - **Summoner**: Summons minions during battle
  - **HordeSummoner**: Summons waves of enemies (bosses only)

### 💎 Loot System
- **Drop Mechanics**: Enemies drop items on death based on LootTables
- **Rarity Scaling**: Drop chances based on enemy type and floor
  - Normal/Elite: Rarity chances scale with floor progression
  - Boss: Always drops loot, higher chance for Epic/Legendary items
- **LootTables**: ScriptableObjects define what enemies can drop
  - Individual item drop chances (0-1)
  - Min/max quantities
  - Rarity-based selection
- **Auto-Collection**: Loot auto-collects after a delay (configurable)
- **Manual Collection**: Click or walk into items to collect immediately

### ⚡ Skills System
- **Active Skills**: Consume mana, have cooldowns
  - Damage scaling with STR/INT based on skill type
  - Can apply status effects (burn, bleed, poison, shield, etc.)
  - Multiple target types: Single Enemy, All Enemies, Self, Ally, All Allies
  - Physical or Magic damage types

- **Passive Skills**: Always active, no resource cost
  - From Race/Class selection
  - From gear items
  - Conditional effects (e.g., damage reduction at low HP)

- **Skill Cooldowns**: Skills refresh each turn, some have multi-turn cooldowns

### 🛒 Shop System
- **Node Type**: Shop nodes appear on the map
- **Item Selection**: Shop offers items based on current floor/zone
- **Purchase System**: Buy items with gold
- **Sell System**: Sell unwanted items for gold (right-click item to sell)

### 🏥 Rest Sites
- **Node Type**: Rest nodes appear on the map
- **Healing**: Restore HP and Mana
- **Preparation**: Prepare before boss fights

### 🗺️ Map & Zone System
- **Zone Structure**: 10 floors per zone, 10 zones total (floors 1-100)
- **Node Types**: Minor Enemy, Elite Enemy, Boss, Event, Shop, Rest, Treasure
- **Map Generation**: Procedural map generation per zone
- **Progression**: Advance floors within zone, transition to next zone at floor 10
- **Checkpoints**: Floors 1, 11, 21... (restore Steadfast Heart)
- **Boss Floors**: Every 10th floor (10, 20, 30... 100)
- **Persistence**: Map state saved per zone, resumes from last visited node

### 🗒️ Events (VN style)
- Event cards with 2–3 choices; outcomes can include stat changes, items, gold, status effects, or route changes
- Race‑specific events add personalization
- Uses Ink scripting system for narrative content

### ❤️ Steadfast Heart
- Starts with 3 durability; lose 1 on defeat. At 0 → run ends
- Restored to 3 at checkpoints (1/11/21/…)
- UI: a 3‑light heart widget

---

## 🛠️ Tech & Data
- **Engine**: Unity 6000.2.2f1 (ea398eefe1c2)
- **Architecture**: GameManager (singleton), BattleManager (turn-based combat), MapManager (node progression), StatusEffectManager (status effects)
- **Data‑Driven**: ScriptableObjects for Skills, Races, Classes, Enemies, Events, Items
- **Save**: JSON for run state (PlayerData, MapData, Inventory, Equipment)
- **Audio/VFX**: AudioManager, CombatEffectManager for damage numbers and visual effects
- **Narrative**: Ink scripting system for events and story content

### ScriptableObject Schemas (high‑level)
- SkillData: id, name, icon, cost, resource, damage, hit bonus, crit threshold, statuses, target, desc
- RaceData: id, name, base stat mods, innate skills, lore
- EnemyData: id, name, base stats, skills, phases, portrait
- EventCardData: id, title, description, choices (checks + outcomes)

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

## 🎮 Cheat Codes

The game includes several developer cheats for testing purposes:

- **Ctrl + Shift + L**: Load Legendary Run (F100) — Gives player all legendary items and sets floor to 100
- **Ctrl + Shift + G**: Toggle God Mode — Makes player invincible and deals massive damage (9999x multiplier)
- **Ctrl + Shift + K**: Kill Player — Instantly kills the player character

**Note**: These cheats are intended for development and testing only. They may not work in all scenes (e.g., Legendary Run cheat only works outside of combat).

## 🤝 Contributing
PRs welcome (systems, content, balance).  
Please open an issue for discussion before large changes.

---

<div align="center">
Climb the dream. Face the nightmare. Find yourself.
</div>
