# Loot System Redesign - Comprehensive Analysis & Proposal

## Current System Analysis

### 1. Gear Rarity System
- **5 Rarity Tiers**: Common (White), Uncommon (Green), Rare (Blue), Epic (Purple), Legendary (Gold)
- **Gear Types**: Weapon, Helmet, ChestArmor, Pants, Boots, Amulet, Ring (7 types)
- **Current Implementation**: Items are assigned rarity at creation (ScriptableObject property)

### 2. Enemy Classification
- **EnemyKind Enum**: 
  - `Normal` - Basic enemies (1-3 per encounter)
  - `Elite` - Stronger enemies (1-2 per encounter)  
  - `Boss` - Boss enemies (1 per encounter, at zone end)
- **Enemy Gimmicks**: Various special abilities (Resurrect, Split, Ranged, Regenerator, Summoner, etc.)
- **Floor Scaling**: Enemies scale by absolute floor (1-100+), with stat multipliers for Elite (3x HP, 1.6x STR, 1.5x DEF) and Boss

### 3. Floor/Progression System
- **Zone System**: Multiple zones (Zone 1, Zone 2, etc.), each with 10 floors
- **Floor Definition**: A floor is one horizontal row on the map (not the entire map)
  - Each floor has 4-6 nodes (battles/events)
  - Each zone has 10 floors
  - Example: Zone 1 has Floor 1, Floor 2... Floor 10
- **Absolute Floor Calculation**: `absoluteFloor = (zone - 1) * 10 + floorInZone`
  - Floor 1 in Zone 1 = Absolute Floor 1
  - Floor 10 in Zone 1 = Absolute Floor 10
  - Floor 1 in Zone 2 = Absolute Floor 11
  - Floor 10 in Zone 2 = Absolute Floor 20
- **Floor Regions** (by Absolute Floor):
  - **Early Game**: Floors 1-20 (Zones 1-2)
  - **Mid Game**: Floors 21-60 (Zones 3-6)
  - **Late Game**: Floors 61-100+ (Zones 7-10+)
- **Shop Tier System** (Already Implemented):
  - EarlyGame (F1-10): 70% Common, 27% Uncommon, 3% Rare
  - MidGame (F21-40): 50% Common, 35% Uncommon, 10% Rare, 5% Epic
  - EndGame (F61-80): 15% Common, 15% Uncommon, 30% Rare, 25% Epic, 15% Legendary

### 4. Current Loot System Issues
- **Static Loot Tables**: Each enemy has a fixed LootTable with specific items
- **No Floor Scaling**: Drop rates don't change with progression
- **No Enemy Type Differentiation**: Normal/Elite/Boss use same tables
- **Manual Configuration**: Requires creating individual loot tables per enemy
- **No Rarity-Based Drops**: Currently item-specific, not rarity-based

---

## Proposed Loot System Design

### Core Concept: Rarity-Based Dynamic Loot Tables

Instead of assigning specific items to enemies, the system should:
1. **Roll for rarity** based on enemy type and floor
2. **Select random item** of that rarity from the global item pool
3. **Scale drop rates** with floor progression
4. **Differentiate by enemy type** (Normal < Elite < Boss)

### Design Principles

1. **Accelerated Progression (Presentation)**: Loot quality improves quickly for demo purposes
   - Legendary gear appears starting at Zone 4 (Floor 31-40)
   - Epic gear appears starting at Zone 3 (Floor 21-30)
   - Designed so players can experience high-tier loot in 30-50 floors
2. **Enemy Type Matters**: Bosses drop better loot than Elites, Elites better than Normals
3. **Rarity Pool System**: Don't assign specific items, assign rarity chances
4. **Balanced Economy**: Higher rarity = rarer drops, but better stats
5. **Maximum Loot Caps**: Prevent excessive loot per enemy

---

## Detailed Loot System Proposal

### 1. Enemy Type Drop Rates by Floor

#### **Normal Enemies** (Accelerated for Presentation)

**Early Game (Floors 1-20)**
```
Floor 1-10:
  Common:     85%
  Uncommon:   15%
  Rare:       0%
  Epic:       0%
  Legendary:  0%

Floor 11-20:
  Common:     60%
  Uncommon:   35%
  Rare:       5%
  Epic:       0%
  Legendary:  0%
```

**Mid Game (Floors 21-60) - Accelerated**
```
Floor 21-30:
  Common:     40%
  Uncommon:   40%
  Rare:       17%
  Epic:       3%
  Legendary:  0%

Floor 31-40 (Zone 4):
  Common:     25%
  Uncommon:   40%
  Rare:       28%
  Epic:       6%
  Legendary:  1%

Floor 41-50 (Zone 5):
  Common:     15%
  Uncommon:   35%
  Rare:       35%
  Epic:       12%
  Legendary:  3%

Floor 51-60:
  Common:     8%
  Uncommon:   27%
  Rare:       45%
  Epic:       17%
  Legendary:  3%
```

**Late Game (Floors 61-100+)**
```
Floor 61-70:
  Common:     5%
  Uncommon:   20%
  Rare:       50%
  Epic:       20%
  Legendary:  5%

Floor 71-80:
  Common:     0%
  Uncommon:   10%
  Rare:       35%
  Epic:       40%
  Legendary:  15%

Floor 81-90:
  Common:     0%
  Uncommon:   5%
  Rare:       30%
  Epic:       45%
  Legendary:  20%

Floor 91-100:
  Common:     0%
  Uncommon:   0%
  Rare:       15%
  Epic:       50%
  Legendary:  35%

Floor 100+:
  Common:     0%
  Uncommon:   0%
  Rare:       10%
  Epic:       50%
  Legendary:  40%
```

#### **Elite Enemies** (Rarity boost: +1 tier shift, Accelerated)

**Early Game (Floors 1-20)**
```
Floor 1-10:
  Common:     55%
  Uncommon:   40%
  Rare:       5%
  Epic:       0%
  Legendary:  0%

Floor 11-20:
  Common:     30%
  Uncommon:   50%
  Rare:       17%
  Epic:       3%
  Legendary:  0%
```

**Mid Game (Floors 21-60) - Accelerated**
```
Floor 21-30:
  Common:     20%
  Uncommon:   45%
  Rare:       30%
  Epic:       4%
  Legendary:  1%

Floor 31-40 (Zone 4):
  Common:     10%
  Uncommon:   35%
  Rare:       40%
  Epic:       12%
  Legendary:  3%

Floor 41-50 (Zone 5):
  Common:     5%
  Uncommon:   25%
  Rare:       45%
  Epic:       20%
  Legendary:  5%

Floor 51-60:
  Common:     0%
  Uncommon:   20%
  Rare:       50%
  Epic:       25%
  Legendary:  5%
```

**Late Game (Floors 61-100+)**
```
Floor 61-70:
  Common:     0%
  Uncommon:   15%
  Rare:       50%
  Epic:       30%
  Legendary:  5%

Floor 71-80:
  Common:     0%
  Uncommon:   10%
  Rare:       40%
  Epic:       40%
  Legendary:  10%

Floor 81-90:
  Common:     0%
  Uncommon:   5%
  Rare:       30%
  Epic:       45%
  Legendary:  20%

Floor 91-100:
  Common:     0%
  Uncommon:   0%
  Rare:       10%
  Epic:       50%
  Legendary:  40%

Floor 100+:
  Common:     0%
  Uncommon:   0%
  Rare:       5%
  Epic:       45%
  Legendary:  50%
```

#### **Boss Enemies** (Rarity boost: +2 tiers, Accelerated - Epic+ guaranteed from Zone 3)

**Early Game (Floors 1-20)**
```
Floor 10 (Zone 1 Boss):
  Common:     15%
  Uncommon:   50%
  Rare:       30%
  Epic:       5%
  Legendary:  0%

Floor 20 (Zone 2 Boss):
  Common:     5%
  Uncommon:   35%
  Rare:       45%
  Epic:       13%
  Legendary:  2%
```

**Mid Game (Floors 21-60) - Accelerated**
```
Floor 30 (Zone 3 Boss):
  Common:     0%
  Uncommon:   25%
  Rare:       50%
  Epic:       22%
  Legendary:  3%

Floor 40 (Zone 4 Boss):
  Common:     0%
  Uncommon:   15%
  Rare:       45%
  Epic:       32%
  Legendary:  8%

Floor 50 (Zone 5 Boss):
  Common:     0%
  Uncommon:   5%
  Rare:       35%
  Epic:       47%
  Legendary:  13%

Floor 60 (Zone 6 Boss):
  Common:     0%
  Uncommon:   0%
  Rare:       25%
  Epic:       55%
  Legendary:  20%
```

**Late Game (Floors 61-100+)**
```
Floor 70 (Zone 7 Boss):
  Common:     0%
  Uncommon:   0%
  Rare:       15%
  Epic:       60%
  Legendary:  25%

Floor 80 (Zone 8 Boss):
  Common:     0%
  Uncommon:   0%
  Rare:       10%
  Epic:       60%
  Legendary:  30%

Floor 90 (Zone 9 Boss):
  Common:     0%
  Uncommon:   0%
  Rare:       5%
  Epic:       55%
  Legendary:  40%

Floor 100 (Zone 10 Boss):
  Common:     0%
  Uncommon:   0%
  Rare:       0%
  Epic:       45%
  Legendary:  55%
```

### 2. Maximum Loot Per Enemy

#### **Normal Enemies**
- **Max Drops**: 1-2 items
- **Base Drop Chance**: 70% per slot
- **Rarity Distribution**: See tables above

#### **Elite Enemies**
- **Max Drops**: 2-3 items
- **Base Drop Chance**: 85% per slot
- **Rarity Boost**: +1 tier (shifts probability up one tier)
- **Rarity Distribution**: See tables above

#### **Boss Enemies**
- **Max Drops**: 3-5 items
- **Base Drop Chance**: 100% guaranteed (always drops loot)
- **Rarity Boost**: +2 tiers (significant upgrade)
- **Guaranteed Epic**: Floor 70+ bosses guaranteed at least 1 Epic+ item
- **Rarity Distribution**: See tables above

### 3. Item Selection Logic

**Current Flow**:
1. Enemy dies → `LootDrop.DropLoot()`
2. Roll `baseDropChance` for each drop slot (1-N max drops)
3. For each successful roll:
   - Determine rarity based on enemy type + floor
   - Query `GameManager.allItems` for items of that rarity
   - Select random item from pool
   - Add to inventory

**New Flow**:
1. Enemy dies → `LootDrop.DropLoot()`
2. Get enemy type (Normal/Elite/Boss) and absolute floor
3. Calculate max drops based on enemy type
4. Roll for each drop slot:
   - First roll: `baseDropChance` (if Normal/Elite) or 100% (if Boss)
   - Second roll: Rarity distribution based on type + floor
   - Select random item of that rarity from `GameManager.allItems`
   - Add to inventory (already implemented)

### 4. Implementation Structure

#### **New Class: `LootConfigSO` (ScriptableObject)**
```
[CreateAssetMenu(menuName = "Dreambound Tower/Loot Config")]
public class LootConfigSO : ScriptableObject
{
    [Header("Floor Range")]
    public int minAbsoluteFloor;
    public int maxAbsoluteFloor;
    
    [Header("Normal Enemy Drop Rates")]
    public LootRarityConfig normalEnemyConfig;
    
    [Header("Elite Enemy Drop Rates")]
    public LootRarityConfig eliteEnemyConfig;
    
    [Header("Boss Enemy Drop Rates")]
    public LootRarityConfig bossEnemyConfig;
    
    [System.Serializable]
    public class LootRarityConfig
    {
        public RarityWeight commonWeight;
        public RarityWeight uncommonWeight;
        public RarityWeight rareWeight;
        public RarityWeight epicWeight;
        public RarityWeight legendaryWeight;
        
        [Header("Drop Settings")]
        public int minDrops = 1;
        public int maxDrops = 2;
        public float baseDropChance = 0.7f; // Chance for each drop slot
    }
}
```

#### **Modified Class: `LootDrop.cs`**
- Remove dependency on `LootTable` asset
- Add floor-based rarity calculation
- Use `LootConfigSO` to determine drop rates
- Select random item from `GameManager.allItems` by rarity

#### **New System: `LootConfigManager.cs`**
- Singleton that manages all `LootConfigSO` assets
- Provides method: `GetRarityForEnemy(EnemyKind kind, int floor)`
- Provides method: `GetDropCountForEnemy(EnemyKind kind, int floor)`
- Provides method: `GetDropChanceForEnemy(EnemyKind kind, int floor)`

---

## Recommended Rarity Drop Tables (Summary)

### Normal Enemies - Accelerated Scale (Presentation)
- **F1-10**: 85% Common, 15% Uncommon
- **F11-20**: 60% Common, 35% Uncommon, 5% Rare
- **F21-30**: 40% Common, 40% Uncommon, 17% Rare, 3% Epic
- **F31-40 (Zone 4)**: 25% Common, 40% Uncommon, 28% Rare, 6% Epic, **1% Legendary** ⭐
- **F41-50 (Zone 5)**: 15% Common, 35% Uncommon, 35% Rare, 12% Epic, **3% Legendary** ⭐
- **F51-60**: 8% Common, 27% Uncommon, 45% Rare, 17% Epic, 3% Legendary
- **F61-70**: 5% Common, 20% Uncommon, 50% Rare, 20% Epic, 5% Legendary
- **F71-80**: 0% Common, 10% Uncommon, 35% Rare, 40% Epic, 15% Legendary
- **F81-90**: 0% Common, 5% Uncommon, 30% Rare, 45% Epic, 20% Legendary
- **F91-100**: 0% Common, 0% Uncommon, 15% Rare, 50% Epic, 35% Legendary
- **F100+**: 0% Common, 0% Uncommon, 10% Rare, 50% Epic, 40% Legendary

### Elite Enemies - +1 Tier Boost (Accelerated)
- **F1-10**: 55% Common, 40% Uncommon, 5% Rare
- **F11-20**: 30% Common, 50% Uncommon, 17% Rare, 3% Epic
- **F21-30**: 20% Common, 45% Uncommon, 30% Rare, 4% Epic, 1% Legendary
- **F31-40 (Zone 4)**: 10% Common, 35% Uncommon, 40% Rare, 12% Epic, **3% Legendary** ⭐
- **F41-50 (Zone 5)**: 5% Common, 25% Uncommon, 45% Rare, 20% Epic, **5% Legendary** ⭐
- **F51-60**: 0% Common, 20% Uncommon, 50% Rare, 25% Epic, 5% Legendary
- **F61-70**: 0% Common, 15% Uncommon, 50% Rare, 30% Epic, 5% Legendary
- **F71-80**: 0% Common, 10% Uncommon, 40% Rare, 40% Epic, 10% Legendary
- **F81-90**: 0% Common, 5% Uncommon, 30% Rare, 45% Epic, 20% Legendary
- **F91-100**: 0% Common, 0% Uncommon, 10% Rare, 50% Epic, 40% Legendary
- **F100+**: 0% Common, 0% Uncommon, 5% Rare, 45% Epic, 50% Legendary

### Boss Enemies - +2 Tier Boost (Accelerated - Epic+ from Zone 3)
- **F10 (Zone 1)**: 15% Common, 50% Uncommon, 30% Rare, 5% Epic
- **F20 (Zone 2)**: 5% Common, 35% Uncommon, 45% Rare, 13% Epic, 2% Legendary
- **F30 (Zone 3)**: 0% Common, 25% Uncommon, 50% Rare, 22% Epic, 3% Legendary ⭐
- **F40 (Zone 4)**: 0% Common, 15% Uncommon, 45% Rare, 32% Epic, **8% Legendary** ⭐
- **F50 (Zone 5)**: 0% Common, 5% Uncommon, 35% Rare, 47% Epic, **13% Legendary** ⭐
- **F60 (Zone 6)**: 0% Common, 0% Uncommon, 25% Rare, 55% Epic, 20% Legendary
- **F70 (Zone 7)**: 0% Common, 0% Uncommon, 15% Rare, 60% Epic, 25% Legendary
- **F80 (Zone 8)**: 0% Common, 0% Uncommon, 10% Rare, 60% Epic, 30% Legendary
- **F90 (Zone 9)**: 0% Common, 0% Uncommon, 5% Rare, 55% Epic, 40% Legendary
- **F100 (Zone 10)**: 0% Common, 0% Uncommon, 0% Rare, 45% Epic, **55% Legendary** ⭐ (Max Cap)

---

## Shop & Event Scaling Recommendation

Since loot progression is accelerated for presentation, **Shop and Event systems have been updated** to match:

### Shop Tier Updates (✅ IMPLEMENTED)
- **F1-10 (EarlyGame1)**: 75% Common, 23% Uncommon, 2% Rare
- **F11-20 (EarlyGame2)**: 50% Common, 42% Uncommon, 8% Rare
- **F21-40 (MidGame1)**: 30% Common, 42% Uncommon, 23% Rare, 5% Epic
- **F41-60 (MidGame2)**: 12% Common, 28% Uncommon, 38% Rare, 17% Epic, **5% Legendary** ⭐
- **F61-80 (EndGame1)**: 3% Common, 12% Uncommon, 40% Rare, 32% Epic, **13% Legendary** ⭐
- **F81-100 (EndGame2)**: 0% Common, 2% Uncommon, 28% Rare, 42% Epic, **28% Legendary** ⭐

**Shop is slightly better than loot drops** since players pay gold for items.

### Recommended Event/Mystery Scaling
- **Zone 3 (F21-30)**: Epic items should appear (5-10% chance)
- **Zone 4 (F31-40)**: Legendary items should appear (2-5% chance)
- **Zone 5 (F41-50)**: Legendary items more common (5-10% chance)

This ensures players can experience all rarity tiers within 30-50 floors during presentation.

---

## Technical Implementation Plan

### Phase 1: Create LootConfig System
1. Create `LootConfigSO` ScriptableObject
2. Create `LootConfigManager` singleton
3. Create multiple `LootConfigSO` assets for each floor range
4. Load configs based on absolute floor

### Phase 2: Modify LootDrop Component
1. Remove `LootTable` dependency
2. Add `EnemyKind` detection from `EnemyTemplateSO`
3. Get absolute floor from `BattleManager` or `GameManager`
4. Query `LootConfigManager` for rarity distribution
5. Select random item from `GameManager.allItems` by rarity

### Phase 3: Update LootManager
1. Modify `SpawnLoot()` to accept rarity instead of `LootTable`
2. Update `GetRandomLoot()` to work with rarity pools
3. Ensure item selection works with rarity filtering

### Phase 4: Testing & Balance
1. Test drop rates at different floors
2. Verify rarity distribution matches intended percentages
3. Balance max drops per enemy type
4. Test with different enemy types

---

## Benefits of This System

1. **Automatic Scaling**: Loot improves naturally as player progresses
2. **No Manual Configuration**: Don't need to create loot tables per enemy
3. **Enemy Type Differentiation**: Bosses feel rewarding, Elites feel special
4. **Balanced Progression**: Common enemies start with Common loot, gradually get better
5. **Maintainable**: Single config file per floor range, easy to adjust
6. **Consistent**: Uses same rarity system as Shop and Treasure

---

## Next Steps

1. **Review & Approve**: Review this proposal and adjust percentages if needed
2. **Create Config Assets**: Create `LootConfigSO` assets for each floor range
3. **Implement Code**: Modify `LootDrop` and `LootManager` to use new system
4. **Test**: Playtest at different floors to verify drop rates
5. **Balance**: Adjust percentages based on gameplay feedback

