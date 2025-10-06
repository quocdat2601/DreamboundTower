# Map System (Zone/Floor) — Developer Guide

This document explains how the map flow works, how it persists state, and how it integrates with the single‑run model.

## What it does (high level)
- Procedurally generates a map per zone with 10 floors each (default).
- Tracks current zone and floor, and progresses floors and zones.
- Persists the map JSON and zone/floor data in PlayerPrefs per zone.
- Reloads the saved map when re‑entering a zone scene, or generates a new one when needed.

## Core scripts
- `MapManager.cs`: Orchestrates zone/floor, loads/saves data, generates new maps, handles zone transitions.
- `MapGenerator.cs`: Produces a `Map` from `MapConfig` and blueprints.
- `MapView.cs`: Renders the map and provides UI layer updates.
- `MapPlayerTracker.cs`: Tracks the player path/progression on the map.
- `Node/Map/MapConfig/*`: Data, blueprints, and configuration assets.

## Scene/Zone detection
- On `Start()`, `MapManager.DetectZoneFromSceneName()` reads the scene name. If the scene is `ZoneN`, `currentZone = N`.
- If the detected zone differs from stored `currentZone`, `MapManager` syncs it and saves immediately.

## Load sequence (MapManager.Start)
1. Detect zone from scene name.
2. Load `currentFloor` and `steadfastHeartRestores` for this zone.
3. If `Zone{currentZone}_Map` exists:
   - Load JSON → `CurrentMap`.
   - If the boss node is already in `map.path`, a new map is generated (run has finished that zone).
   - Otherwise show the loaded map and sync floor from the last visited node.
4. If no saved map for this zone exists, generate a new one.
5. Apply any pending completion carried over from battle scenes via `MapTravel`.

## Persistence model (PlayerPrefs keys)
- Per zone:
  - `Zone{N}_Map` → Map JSON for zone N
  - `Zone{N}_Floor` → Floor in zone N (1..10)
  - `Zone{N}_SteadfastHeart` → Remaining restores
- Global helpers:
  - `CurrentZone` → Active zone index

`MapManager.SaveMap()` writes all the above keys and calls `PlayerPrefs.Save()`.

## Floor/zone progression
- `AdvanceFloor()` increments `currentFloor` and persists.
- When passing last floor (10), it increments `currentZone`, resets `currentFloor = 1`, saves, and loads the next `Zone{currentZone}` scene.
- `IsCheckpointFloor()` uses absolute floor math: floors 1, 11, 21... are checkpoints.
- `IsBossFloor()` returns `currentFloor == totalFloorsPerZone`.

## Single‑Run integration
The main menu uses `RunSaveService` to enforce one active run.
- `Run_Active` and `Run_LastScene` are maintained by `RunSaveService`.
- We update run meta whenever the map is saved so Continue knows where to resume.

Hook already added:
- In `MapManager.SaveMap()`:
  - `RunSaveService.UpdateLastScene(SceneManager.GetActiveScene().name);`

Run meta keys (PlayerPrefs):
- `Run_Active` (int 0/1) → if an active run exists
- `Run_LastScene` (string) → last scene to continue from

Start and Continue flow:
- New Game: `RunSaveService.StartNewRun("Zone1");`
- Continue: `RunSaveService.ContinueRunOrFallback("Zone1");`
- Overwrite: `RunSaveService.ClearRun()` then `StartNewRun`.

## Public entry points (common)
- `MapManager.GenerateNewMap()` → Create and show a new map for the current zone; resets `currentFloor = 1` and saves.
- `MapManager.SaveMap()` → Persist map JSON and zone/floor/steadfast; updates run meta last scene.
- `MapManager.AdvanceFloor()` → Move to next floor, auto‑transition at zone boundary.
- `MapManager.GenerateNewMapForZone(int zone)` → Switch zone, reset floor, then generate.

## Adding features safely
- When adding any data that is per‑run (inventory, party, relics), store them under a clear prefix (`Run_*`) so `RunSaveService.ClearRun()` can remove them without touching user settings.
- If you introduce additional scenes beyond `ZoneN`, call `RunSaveService.UpdateLastScene(SceneManager.GetActiveScene().name)` at meaningful save/checkpoint moments so Continue remains accurate.

## Minimal setup checklist
- Create `MapConfig` asset and assign it to `MapManager.config`.
- Ensure `MapView` is assigned for rendering.
- Ensure your zone scenes follow `Zone1`, `Zone2`, ... naming so detection works.
- Main Menu wired to `RunSaveService` via `MainMenu` methods.

## Example snippets
```csharp
// Generate a new run and go to Zone1
RunSaveService.StartNewRun("Zone1");

// Continue existing run or fallback to Zone1
RunSaveService.ContinueRunOrFallback("Zone1");

// Update last scene at a checkpoint
RunSaveService.UpdateLastScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);

// In map after moving to a node
MapManager.Instance.SaveMap();
```

