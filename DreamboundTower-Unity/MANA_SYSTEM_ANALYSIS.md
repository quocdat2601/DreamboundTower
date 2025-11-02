# Mana System Analysis & Balance Recommendations

## Current Mana System

### Base Mana Values
- **MANA_UNIT**: 5 (each stat point = 5 actual mana)
- **Race Base MANA Stats**:
  - Human: 10 → **50 mana**
  - Beastfolk: 7 → **35 mana**
  - Demon: 8 → **40 mana**
  - Celestial: 12 → **60 mana**
  - Undead: 6 → **30 mana**
- **Average Base Mana**: ~8.6 stat points → **~43 mana**

### Mana Increase Sources

#### 1. Events
- Can permanently gain +MANA stat points (amount varies by event)
- Each point = +5 mana permanently
- Typical gains: +1 to +3 MANA stat points per event

#### 2. Gear (Flat Bonuses)
- **Common**: 30 mana (Wooden Amulet)
- **Uncommon**: 40 mana (Moonstone Charm)
- **Rare**: 70 mana (Sapphire Pendant)
- **Epic**: 40-100 mana (40-80 rings/amulets, 100 amulets)
- **Legendary**: 50-100 mana (50 weapons, 100 rings/amulets/helmets)
- **Maximum Possible**: ~300-400+ mana from full gear set

#### 3. Gear (Percentage Modifiers)
- Can multiply total mana (e.g., +20% mana)
- Applies multiplicatively after flat bonuses

#### 4. Passive Skills
- Can increase base mana permanently
- Adaptive Spirit: +2 to all base stats = +10 mana

### Mana Regeneration

#### Base Regen
- **5% per turn** (fixed for all characters)

#### Skill Regen (Divine Resonance - Celestial only)
- **+5% per turn** (conditional passive)
- Total for Celestial: **10% per turn**

#### Gear Regen Bonuses
- Can have `ManaRegenPercent` modifiers
- Adds to base regen percentage multiplicatively

### Current Skill Costs (After Update)
1. Arcane Burst: **16** mana
2. Flurry Strike: **12** mana
3. Pounce: **6** mana
4. Bone Resurge: **10** mana
5. Resolve Surge: **6** mana
6. Radiant Shield: **10** mana
7. Rage Break: **10** mana
8. Sanctified Strike: **10** mana
9. Sanctuary Ward: **12** mana
10. Shield Slam: **10** mana

## Mana Economy Analysis

### Early Game (50-100 mana, 5% regen)
- **Base**: 50 mana (Human)
- **Regen per turn**: 2.5 mana (5% of 50)
- **Skills per turn**: Can cast 1-2 cheap skills (6-12 cost) per turn
- **Full mana pool**: Can cast 3-8 skills total (depending on costs)

**Example Scenarios**:
- Turn 1: Cast 2 skills (6+6 = 12) → 50-12 = 38 mana remaining
- Turn 2: Regenerate 2.5 → 40.5 mana, cast 1 skill (10) → 30.5 mana
- Turn 3: Regenerate 1.5 → 32 mana, cast 1 skill (12) → 20 mana

### Mid Game (100-200 mana, 5% regen)
- **Base + Gear**: ~100-150 mana
- **Regen per turn**: 5-7.5 mana (5% of 100-150)
- **Skills per turn**: Can cast 1-2 skills consistently
- **Full mana pool**: Can cast 5-12 skills total

### Late Game (200-400+ mana, 5-10% regen)
- **Full Gear**: 200-400+ mana
- **Regen per turn**: 10-40 mana (5-10% of 200-400)
- **Skills per turn**: Can cast 2-4 skills consistently
- **Full mana pool**: Can cast 12-30+ skills total

### Problem Analysis

#### Issue 1: Early Game Mana Starvation
- With 50 base mana and 5% regen (2.5/turn), players can only cast:
  - 1-2 cheap skills (6-12 cost) every 2-3 turns
  - Or burn entire pool in 3-4 turns
- **Feels restrictive** for skill-based builds

#### Issue 2: Late Game Mana Overflow
- With 300+ mana and 5-10% regen (15-30/turn), players can:
  - Cast 2-3 skills every turn indefinitely
  - Never run out of mana
- **Loses resource management challenge**

#### Issue 3: Regen vs Cost Mismatch
- Average skill cost: **10.4 mana**
- Base regen: **2.5 mana/turn** (50 mana pool)
- **Regen is too slow** compared to skill costs

## Balance Recommendations

### Option 1: Increase Base Regen (Recommended)
- **Change base regen from 5% to 8-10%**
- **Rationale**:
  - Early game: 50 mana × 8% = 4 mana/turn (can cast cheap skill every 2-3 turns)
  - Late game: 300 mana × 8% = 24 mana/turn (can cast 2 skills/turn consistently)
  - Better balance between early and late game

### Option 2: Reduce Base Regen but Increase Flat Regen
- **Change base regen from 5% to 3%**
- **Add flat regen**: +2-3 mana/turn base
- **Formula**: `regen = (mana × 3%) + 3`
- **Rationale**:
  - Early game: 50 mana × 3% + 3 = 4.5 mana/turn
  - Late game: 300 mana × 3% + 3 = 12 mana/turn
  - Prevents late game overflow while helping early game

### Option 3: Adjust Skill Costs (Alternative)
- **Reduce all skill costs by 20-30%**
- Early game skills: 6→5, 10→8, 12→10
- **Rationale**: Lower costs make skills more accessible early game
- **Issue**: May make late game too easy

### Recommended Solution: **8% Base Regen**

#### Why 8%?
1. **Early Game (50 mana)**:
   - Regen: 4 mana/turn
   - Can cast cheap skill (6 mana) every 1.5 turns
   - Can cast expensive skill (12 mana) every 3 turns
   - **Feels playable but not trivial**

2. **Mid Game (100-150 mana)**:
   - Regen: 8-12 mana/turn
   - Can cast average skill (10 mana) every turn
   - Can cast 2 cheap skills (6+6=12) per turn
   - **Good balance**

3. **Late Game (300 mana)**:
   - Regen: 24 mana/turn
   - Can cast 2 average skills (10+10=20) per turn
   - Can cast 1 expensive skill (16) + 1 cheap (6) = 22 per turn
   - **Still requires management, not infinite**

#### With Celestial (10% base + 5% Divine = 15% total)
- Early (60 mana): 9 mana/turn (strong but balanced)
- Late (300 mana): 45 mana/turn (can cast 2-3 skills/turn, very powerful but race-specific)

### Skill Cost Adjustments (Optional)

If keeping 5% regen, reduce costs:
- Arcane Burst: 16 → **14**
- Flurry Strike: 12 → **10**
- Pounce: 6 → **5**
- Bone Resurge: 10 → **8**
- Resolve Surge: 6 → **5**
- Radiant Shield: 10 → **8**
- Rage Break: 10 → **8**
- Sanctified Strike: 10 → **8**
- Sanctuary Ward: 12 → **10**
- Shield Slam: 10 → **8**

**Average cost**: 10.4 → **8.4** (20% reduction)

## Final Recommendation

### Primary: Increase Base Mana Regen to 8%
- **File**: `Assets/Scripts/Core/BattleManager.cs` line 1066
- **Change**: `float totalRegenPercent = 5.0f;` → `float totalRegenPercent = 8.0f;`
- **Reasoning**: Provides better balance across all game stages without breaking economy

### Secondary: Keep Current Skill Costs
- Current costs (6-16) are reasonable with 8% regen
- Early game: Can cast skills regularly
- Late game: Still requires resource management

### Alternative: 10% Base Regen (More Aggressive)
- If 8% feels too slow in testing, consider 10%
- Would make skills more accessible but may reduce strategic depth
