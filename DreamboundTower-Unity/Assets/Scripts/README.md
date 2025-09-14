# 📁 Scripts Organization

This folder contains all the game scripts organized by functionality.

## 📂 Folder Structure

### 🎯 Core/
Core game systems and data structures:
- **`Character.cs`** - Player character stats and behavior
- **`GearItem.cs`** - Item data structure and properties
- **`GearManager.cs`** - Manages gear generation and properties
- **`BattleManager.cs`** - Handles turn-based combat system

### 🎒 Inventory/
Inventory and equipment management:
- **`Inventory.cs`** - Inventory system with 20 slots
- **`Equipment.cs`** - Equipment system with 8 slots and stat bonuses

### 🖥️ UI/
User interface components:
- **`CharacterUI.cs`** - Character stats display
- **`InventoryUI.cs`** - Basic inventory interface
- **`SimpleInventoryUI.cs`** - Simplified inventory interface

### 🎮 DragDropSystem/
Drag and drop functionality for inventory:
- **`DragDropSystem.cs`** - Main drag and drop manager
- **`DraggableItem.cs`** - Makes items draggable
- **`DropZone.cs`** - Defines valid drop areas
- **`DragDropInventoryUI.cs`** - Enhanced UI with drag and drop
- **`DragDropSetup.cs`** - Easy setup script
- **`README.md`** - System documentation
- **`SetupGuide.md`** - Step-by-step setup instructions

### 💎 LootSystem/
Loot generation and collection:
- **`LootManager.cs`** - Manages loot collection and auto-loot
- **`LootDrop.cs`** - Handles item dropping
- **`LootPickup.cs`** - Item pickup with click-to-collect
- **`LootTable.cs`** - Defines loot generation rules
- **`ItemRarity.cs`** - Item rarity system
- **`README.md`** - Loot system documentation

## 🚀 Quick Start

### Setting Up Drag and Drop
1. **Add `DragDropSetup` component** to any GameObject
2. **Click "Setup Drag and Drop System"** in Inspector
3. **Done!** Your inventory now supports drag and drop

### Using the Loot System
1. **LootManager** handles automatic collection
2. **Click on loot items** to collect manually
3. **Configure auto-loot delay** in LootPickup component

### Inventory Management
- **Left-click items** to equip/unequip
- **Drag and drop** for precise item management
- **Automatic slot finding** for equipment
- **Stat bonuses** applied automatically

## 🔧 System Integration

All systems work together seamlessly:
- **Loot System** → **Inventory** → **Equipment** → **Character Stats**
- **UI Components** update automatically via events
- **Drag and Drop** enhances existing click-to-equip functionality

## 📚 Documentation

Each folder contains its own README with detailed information:
- **DragDropSystem/README.md** - Complete drag and drop documentation
- **DragDropSystem/SetupGuide.md** - Step-by-step setup guide
- **LootSystem/README.md** - Loot system documentation

## 🎯 Key Features

- ✅ **Turn-based combat** with enemy spawning
- ✅ **Inventory management** with 20 slots
- ✅ **Equipment system** with 8 slots and stat bonuses
- ✅ **Drag and drop** for intuitive item management
- ✅ **Loot system** with auto-collect and manual pickup
- ✅ **Visual feedback** for all interactions
- ✅ **Event-driven architecture** for clean code
