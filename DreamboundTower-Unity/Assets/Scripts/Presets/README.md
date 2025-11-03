# Presets & Enemy Stats – Setup and Workflow

This document explains how Races/Classes, Enemy templates, Map → Combat stat flow, and Debug/Override tools work.

## 1) ScriptableObjects (where to put what)
- C# scripts: `Assets/Scripts/Presets/*.cs` (RacePresetSO, ClassPresetSO, EnemyTemplateSO, CombatConfigSO)
- Assets (.asset): `Assets/Scriptable Objects/...`
  - Races: `Assets/Scriptable Objects/RaceConfig/`
  - Classes: `Assets/Scriptable Objects/ClassConfig/`
  - Enemies: `Assets/Scriptable Objects/Enemy Config/`
  - CombatConfig (special): `Assets/Resources/CombatConfig.asset` (must be under Resources)

## 2) Player Presets
Create assets via Project → Create → Presets → RacePreset / ClassPreset.
- RacePresetSO: id, displayName, baseStats (HP, STR, DEF, MANA, INT, AGI), passiveName/Description, activeName/Cooldown/Mana/Description
- ClassPresetSO: id, displayName, passiveName/Description, and arrays for activeNames/Cooldowns/ManaCosts/Descriptions

Use: The selection UI reads these assets and composes the player’s final passives/skills.

## 3) Enemy Templates
Create 3 assets via Project → Create → Presets → EnemyTemplate:
- Normal: base at floor1 = HP=4×HP_UNIT, STR=6, DEF=2, INT/MANA/AGI=0; multipliers = 1/1/1
- Elite: copy Normal base; multipliers = HP×3.0, STR×1.6, DEF×1.5
- Boss: base at floor1 = HP=40×HP_UNIT, STR=20, DEF=8; multipliers = 1/1/1
- Growth: baseRate=0.012; endlessAfterFloor=100; endlessBonus=0.002

Runtime scaling:
```
stat(floor) = baseAtFloor1 * (1 + rate)^(floor - 1) * (multipliers)
rate = baseRate (+0.002 if floor > 100)
```

## 4) Map → Combat data flow (when you click a node)
1. Player clicks a map node (`MapPlayerTracker`)
2. Compute floor: `floorInZone = node.point.y + 1`, `absoluteFloor = (currentZone - 1) * 10 + floorInZone`
3. Choose enemy template by node type:
   - MinorEnemy → Normal (MapManager.normalTemplate)
   - EliteEnemy → Elite
   - Boss → Boss
4. Write payload to both locations:
   - `Resources/CombatConfig.asset` (ScriptableObject)
   - PlayerPrefs fallback (for scene-load safety)
5. After swirl animation delay, load combat

Expected logs (full dump):
- Map: `[MAP] Wrote CombatConfig: floor=.., kind=.., archetype=.., HP=.. STR=.. DEF=..`
- Combat:
  - `[BATTLE] Enemy init @ floor .. | Kind=.. | Archetype=..`
  - `RAW   HP=.. STR=.. DEF=.. MANA=.. INT=.. AGI=..`
  - `FINAL HP=.. (hpUnit=..) ATK=.. DEF=..`

## 5) Combat scene (BattleManager)
At Start():
- Reads `Resources/CombatConfig`; if missing, reads PlayerPrefs fallback.
- Spawns enemies and applies stats on `Character`.

HP scalar (hpUnit):
- `BattleManager.hpUnit` multiplies the template HP before applying to Character.
- Example: template HP=4 and `hpUnit=10` → final HP=40. Set `hpUnit=1` if your templates already store absolute HP.

### Debug / Overrides (Inspector)
For quick tests without the map flow:
- `overrideUseCustomStats`: uses `overrideKind` + `overrideStats` directly.
- `overrideUseTemplate`: samples `overrideTemplate` at `overrideFloor`.
- Otherwise: uses the Map payload.

## 6) MapManager references (required)
In each Zone scene, select `MapManager` and assign:
- Enemy Templates → Normal / Elite / Boss (`EnemyTemplateSO`)
- Ensure `Assets/Resources/CombatConfig.asset` exists.

## 7) Zones and Floors
- Scene name → `currentZone` (ZoneN → N)
- 10 floors per zone (1–10, 11–20, …)
- Boss node is on the last layer (floor 10)
- Floor display syncs from last visited node

## 8) Common pitfalls & fixes
- HP=1/STR=0: template base zeros, missing CombatConfig, or payload not written
- Elite/Boss show Normal: wrong node type or missing template references
- Zone not switching: boss defeat → `AdvanceFloor()` → `TransitionToNextZone()`

## 9) Extending
- Per-zone variety: registry SO `Zone → Template`
- Enemy groups: make CombatConfigSO hold a list, write multiple payloads, spawn many
- Enemy skills: add EnemySkillPresetSO and read by archetype id

## 10) Quick checklist
- [ ] Create `Resources/CombatConfig.asset`
- [ ] Assign Normal/Elite/Boss templates on `MapManager`
- [ ] Click node → verify `[MAP] Wrote CombatConfig ...`
- [ ] Combat → verify `[BATTLE] Enemy init ...`
- [ ] Optional: use BattleManager overrides
