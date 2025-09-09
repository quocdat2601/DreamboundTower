# Dreambound Tower — Prototype

A narrative-driven RPG roguelite combining node‑based progression, turn‑based combat, and visual‑novel choices with impactful dice mechanics. Built with Unity 6000.2.2f1.

## One‑page pitch
You play as a boy falling into a surreal dream world: a colossal tower forged from cherished memories and childhood nightmares. Explore node‑based maps (à la Slay the Spire), make meaningful visual‑novel choices, and fight turn‑based battles where actions resolve via d20 rolls (hit/graze/crit/effects). Climb toward floor 100 to uncover the truth behind the dream and yourself.

- **Genres**: RPG Roguelite, Turn‑based, Choice‑driven Visual Novel, Dice mechanics
- **Platforms**: Windows PC, WebGL (demo)
- **Engine**: Unity 6000.2.2f1 (ea398eefe1c2)
- **Team**: 5 students (prototype)

## Story / Lore (brief)
A bright childhood fades into cracks: distant parents, scattered friends, academic pressure, fear of abandonment, and loss. To hide, the boy creates a dream tower—only to find nightmares waiting inside. Bosses take cues from Vietnamese urban legends (Ông Ba Bị, Ma Dã, Ma Thần Vòng, Quỷ Nhập Tràng, Ma Tràng, Ma Gà, Ma Lai, Thần Trùng, Ma Vú Dài). A mysterious shadow (future self) kills the boy in the opening—proving the dream hurts. In a dark void, a small fairy (later revealed as his lost sister) introduces the system: race, archetype, stats, RNG character creation, and the 100‑floor goal.

## Core gameplay loop
1) Map (node‑based) → 2) Pick node (Combat / Elite / Event / Shop / Rest / Treasure) → 3) Resolve (combat is turn‑based with dice; events use stat checks) → 4) Loot & Upgrade → 5) Continue / Boss → 6) Checkpoint persist or failure.

### Map & Nodes
- Zones of ~10 floors (1–10, 11–20, …). Each floor: 4–6 randomized nodes
- Node types: Combat, Elite, Event, Shop, Rest, Treasure/Relic
- Auto‑save checkpoints: 1, 11, 21, 31, … (post‑boss). Checkpoints fully restore Steadfast Heart

### Character RNG
- RNG race + archetype + base stats (1–10) via a wheel; limited rerolls
- **Races (prototype)**: Human, Demon, Celestial, Undead, Beastfolk
- **Archetypes**: Warrior, Rogue, Mage, Cleric

### Stats
HP, STR, DEF, MANA, INT, AGI, LUK, Focus. Earn EXP → Skill Points (1:1 to stats or to unlock skills).

### Combat (turn‑based, d20 checks)
- Choose skill → Roll d20 for Hit/Crit/Effect
- Hit DC = 8 + floor(target.EVA/5). Roll = d20 + ACC + skill bonus
- Graze if within 2 of DC (50% damage, no effect)
- Crit: natural 20, or d20 + LUK ≥ threshold (e.g., 20); natural 1 can auto‑miss
- Advantage/Disadvantage: roll twice, take best/worst; spend Focus for advantage or reroll
- Fail‑forward: heavy fail still grants a small benefit (e.g., +1 Focus / recover 1 Mana)

### Status & Items
- Status: Bleed, Burn, Shock, Fear, Guard, Aegis, Mark, Stun…
- Order: StartTurn (DoT) → Action → EndTurn (regen/decay)
- Items: common stat boosts; rare/epic with affixes or unique skills; weapons can grant skills/passives

### Events (visual‑novel style)
- 2–3 choices with stat checks (d20 + statMod vs DC); outcomes grant items, stats, or route changes
- Race‑specific events for personalization

## Steadfast Heart (run‑saving mechanic)
- Starts with 3 durability; on defeat, lose 1. At 0, the run ends
- Restored to 3 at checkpoints (1, 11, 21, …)
- Optional passives: immune to Panic/Despair, reduced Fear duration, small morale resistance
- UI: heart icon with 3 lights

## Bosses & checkpoints
- Boss floors: 10, 20, 30, …, 100
- Prototype: implement T10, T20, T40 (T40 scripted defeat for story trigger)
- Each boss has phases, moves, gimmicks, cinematic triggers

## UI / UX
- Combat HUD: HP, Mana, Focus, turn order, skills with tooltips and roll estimates, dice result panel
- Map UI: node graph, floor, boss distance, legend
- Event UI: art, text, choices, stat hints
- Character creation: RNG wheel animation
- Steadfast Heart widget: 3‑light heart

## Data design (ScriptableObjects)
- SkillData: id, name, icon, cost, resource type, damage, hit bonus, crit threshold, status list, target, description
- RaceData: id, name, base stat mods, innate skills, lore
- EnemyData: id, name, base stats, skills, phases, portrait
- EventCardData: id, title, description, choices with stat checks & outcomes

## Tech architecture
- DiceService (deterministic seed)
- GameStateMachine (Menu, Map, Combat, Event, Cutscene)
- TurnManager / ActionResolver (roll → apply damage/status)
- Data‑driven content via ScriptableObjects
- Save system: JSON (run state + meta unlocks)
- Audio/VFX: simple pooling, lightweight animators

## Balancing reference (prototype)
- Hit success ≈ 70–80%
- Crit baseline ≈ 10–15%
- Graze margin = 2
- Focus: start 1, max 3; gain on crits or events
- Dodge cap = 70% (AGI based)
- Run length (MVP): 35–60 minutes

## Roadmap (6‑week prototype)
- W1–W2: Core combat (DiceService, TurnManager), basic UI, placeholders
- W3: Map/node system, EventSystem, RNG wheel UI
- W4: Boss T10 + T20, items/inventory, save system
- W5: T40 scripted defeat, fairy intro, Steadfast Heart
- W6: Polish, playtest, bugfix, demo build

## Team roles (suggested for 5)
- Lead Gameplay Programmer: TurnManager, DiceService, ActionResolver, StatusSystem
- Tools & Data Engineer: ScriptableObjects, Save/Load, Addressables pipeline
- UI/UX Programmer: Map UI, Combat HUD, RNG wheel, Event cards
- Content Designer/Balancer: skills, events, enemies, bosses
- Artist/Animator: placeholders → art pack integration (Kenney/Itch), VFX, portraits

## Getting started (dev)
- Requirements: Unity 6000.2.2f1, Git LFS (recommended for large assets)
- Clone the repo and open the `DreamboundTower` Unity project folder in Unity Hub
- Recommended: enable Visible Meta Files and Force Text in Editor settings

### Project structure
- `DreamboundTower/Assets` — game assets and scripts
- `DreamboundTower/Packages` — package manifest and lock
- `DreamboundTower/ProjectSettings` — project/editor settings

### Build targets
- Windows: use default URP 2D settings
- WebGL: use Development build for demo; ensure Addressables profile is set

## Testing / QA
- Unit tests for DiceService, ActionResolver, status tick order
- Deterministic run seed for reproducibility
- Playtest the feel of rolls, Focus usefulness, boss difficulty, and event fairness

## License & credits
- Prototype for educational purposes
- Placeholder assets: see `Assets` notes; recommended free sources include Kenney, Itch.io, OpenGameArt

## Links
- Design document summary: see sections above
- Boss design sheets, checklists, and asset lists can be added in `/Docs` (SO schemas, checklist, asset sources)

---
Made with Unity 6000.2.2f1 — Dreambound Tower team. 
