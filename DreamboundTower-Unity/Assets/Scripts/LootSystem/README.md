# Loot System Setup Guide

This loot system allows enemies to drop items when they die. Here's how to set it up:

## Components Overview

### 1. LootManager
- **Purpose**: Manages all loot spawning and collection in the scene
- **Setup**: Add to an empty GameObject in your scene
- **Required**: Assign a loot pickup prefab

### 2. LootDrop
- **Purpose**: Component that makes enemies drop loot when they die
- **Setup**: Add to enemy GameObjects that should drop loot
- **Required**: Assign a LootTable ScriptableObject

### 3. LootTable
- **Purpose**: ScriptableObject that defines what items an enemy can drop
- **Setup**: Right-click in Project > Create > RPG > Loot Table
- **Required**: Add LootEntry items with drop chances

### 4. LootPickup
- **Purpose**: Represents dropped items in the world
- **Setup**: Usually part of a prefab, automatically handled by LootManager

## Quick Setup Steps

### Step 1: Create LootManager
1. Create an empty GameObject in your scene
2. Add the `LootManager` component
3. Assign the `playerTransform` (your player's Character)
4. Assign the `playerInventory` (your player's Inventory component)

### Step 2: Create Loot Tables
1. Right-click in Project window
2. Go to Create > RPG > Loot Table
3. Name it (e.g., "GoblinLootTable")
4. Add LootEntry items:
   - Drag GearItem assets to the "Item" field
   - Set drop chances (0-1, where 1 = 100% chance)
   - Set min/max quantities
   - Choose rarity levels

### Step 3: Add LootDrop to Enemies
1. Select your enemy prefab/GameObject
2. Add the `LootDrop` component
3. Assign the LootTable you created
4. Adjust drop settings:
   - `baseDropChance`: Overall chance to drop anything
   - `maxDrops`: Maximum items that can drop
   - `scatterDrops`: Whether items scatter around enemy

### Step 4: Create Loot Pickup Prefab
1. Create a new GameObject
2. Add `LootPickup` component
3. Add `SpriteRenderer` component
4. Add `CircleCollider2D` component (set as trigger)
5. Save as prefab
6. Assign to LootManager's `lootPickupPrefab` field

## Example Loot Tables

### Basic Goblin Loot Table
- **Common Sword** (30% chance, 1 quantity)
- **Health Potion** (50% chance, 1-2 quantity)
- **Gold Coin** (80% chance, 1-3 quantity)

### Boss Loot Table
- **Epic Weapon** (100% chance, 1 quantity, Epic rarity)
- **Rare Armor** (75% chance, 1 quantity, Rare rarity)
- **Legendary Accessory** (25% chance, 1 quantity, Legendary rarity)

## Collection Methods

### 1. Manual Collection (Default)
- **Click to Collect**: Click on dropped items to pick them up
- **Walk to Collect**: Walk into items to collect them
- Items stay on the ground until manually collected

### 2. Auto-Collect Timer
- Set `globalAutoCollectDelay` in LootManager (e.g., 10 seconds)
- All dropped items will automatically collect after the delay
- Players can still manually collect before the timer expires
- Useful for faster gameplay or when you don't want items cluttering the ground

## Tips

1. **Drop Chances**: Start with low chances (0.1-0.3) to avoid too much loot
2. **Rarity Colors**: Items glow different colors based on rarity
3. **Auto-Collection**: Set `globalAutoCollectDelay` to auto-collect after time
4. **Despawning**: Items disappear after 60 seconds by default
5. **Testing**: Use the "Force Drop Loot" context menu on LootDrop components

## Troubleshooting

- **No loot dropping**: Check that LootManager exists in scene
- **Items not collecting**: Ensure player has Inventory component
- **Visual issues**: Make sure loot pickup prefab has SpriteRenderer
- **Performance**: Limit max drops per enemy (3-5 is usually good)
