# Zone/Floor System - Hướng dẫn Setup

## 📋 Tổng quan

Zone/Floor System cho phép tạo nhiều zone với boss khác nhau, mỗi zone có 10 floors. Hệ thống tự động quản lý map generation, node probabilities, và zone transitions.

## 🏗️ Cấu trúc Zone/Floor

### Zone Structure
- **Zone 1:** Floors 1-10 (Scene: MapScene hoặc Zone1)
- **Zone 2:** Floors 11-20 (Scene: Zone2)
- **Zone 3:** Floors 21-30 (Scene: Zone3)
- **Zone 4:** Floors 31-40 (Scene: Zone4)
- **...**
- **Zone 10:** Floors 91-100 (Scene: Zone10)

### Floor Structure (mỗi Zone)
- **Floor 1:** Node đầu tiên (MinorEnemy)
- **Floor 2-9:** Random nodes theo GDD probabilities
- **Floor 10:** Boss node (cố định)

## 🎮 Node Types & Probabilities

### Node Types
- `MinorEnemy`: Combat thường
- `EliteEnemy`: Combat mạnh
- `RestSite`: Nơi nghỉ ngơi
- `Store`: Cửa hàng
- `Treasure`: Kho báu
- `Mystery`: Event/choice card
- `Boss`: Boss cuối zone

### GDD Node Probabilities theo Floor Region

#### Early Region (Floors 1-20)
- **Combat:** 55%
- **Event:** 20%
- **Rest:** 12%
- **Shop:** 8%
- **Treasure:** 5%
- **Elite:** 0% (chỉ normal combat)

#### Mid Region (Floors 21-60)
- **Combat:** 60%
- **Event:** 18%
- **Rest:** 10%
- **Shop:** 7%
- **Treasure:** 5%
- **Elite:** 5% + 0.2% × floor (tăng dần theo floor)

#### Late Region (Floors 61-100)
- **Combat:** 65%
- **Event:** 12%
- **Rest:** 8%
- **Shop:** 5%
- **Treasure:** 5%
- **Elite:** 5% + 0.2% × floor (tăng dần theo floor)

## ⚙️ Setup Instructions

### 1. Setup MapConfig

#### A. Tạo MapConfig Asset
1. Right-click trong Project window
2. Create → Map → MapConfig
3. Đặt tên: `DefaultMapConfig`

#### B. Configure MapConfig
1. **Node Blueprints:** Drag tất cả NodeBlueprint assets vào list
2. **Random Nodes:** Chọn các node types có thể random
3. **Grid Settings:**
   - `numOfPreBossNodes`: Min 4, Max 5
   - `numOfStartingNodes`: 1
   - `extraPaths`: 0
4. **Layers:** Setup 10 layers (1 layer = 1 floor)

### 2. Setup Zone Configurations

#### A. Auto Setup (Recommended)
1. Select MapManager trong scene
2. Right-click MapManager component
3. Chọn **"Setup Default Zone Configs"**
4. System sẽ tự động tạo configurations cho Zone 1-10

#### B. Manual Setup
1. Select MapConfig asset
2. Expand **"Zone Configuration"**
3. Add elements:
   - **Element 0:** Zone Number = 1, Boss Blueprint = Skeleton Boss, Zone Name = "Tutorial Zone"
   - **Element 1:** Zone Number = 2, Boss Blueprint = Spider Boss, Zone Name = "Forest Zone"
   - **Element 2:** Zone Number = 3, Boss Blueprint = Executioner Boss, Zone Name = "Cave Zone"
   - **...**

### 3. Setup Boss Configurations (Optional)

#### Specific Floor Boss Config
1. Select MapConfig asset
2. Expand **"Boss Blueprint Configuration"**
3. Add elements:
   - **Element 0:** Floor Number = 10, Boss Blueprint = Skeleton Boss
   - **Element 1:** Floor Number = 20, Boss Blueprint = Spider Boss
   - **Element 2:** Floor Number = 30, Boss Blueprint = Executioner Boss
   - **...**

### 4. Setup Scenes

#### A. Create Zone Scenes
1. Tạo scenes: `Zone1`, `Zone2`, `Zone3`, ..., `Zone10`
2. Copy MapObjects từ MapScene vào mỗi scene
3. Setup MapManager trong mỗi scene

#### B. Add to Build Settings
1. File → Build Settings
2. Add scenes vào build:
   - `Scenes/MapScene/SampleScene` (Zone 1)
   - `Scenes/MapScene/Zone1` (Zone 1 alternative)
   - `Scenes/MapScene/Zone2` (Zone 2)
   - `Scenes/MapScene/Zone3` (Zone 3)
   - **...**

### 5. Setup MapManager

#### A. MapManager Component
1. **Config:** Assign MapConfig asset
2. **View:** Assign MapView component
3. **Zone/Floor System:**
   - `currentZone`: 1 (sẽ auto-detect từ scene)
   - `currentFloor`: 1
   - `totalFloorsPerZone`: 10
   - `totalNodesPerFloor`: 5

#### B. MapView Component
1. **Floor Display:**
   - `floorDisplayText`: Assign UI Text (optional)
   - `floorDisplayFormat`: "Floor {0}"
   - `floorDisplayFontSize`: 24
   - `floorDisplayColor`: White

## 🧪 Testing & Debug

### Debug Commands
1. Right-click MapManager component:
   - **"Debug Zone/Floor Info"** - Xem thông tin zone/floor
   - **"Test Zone Progression"** - Test advance qua zone
   - **"Test Zone Transition"** - Test chuyển scene
   - **"Generate Map for Zone X"** - Generate map cho zone cụ thể

### Test Flow
1. **Zone 1:** Click nodes → Floor 1 → Floor 2 → ... → Floor 10 (Boss)
2. **Boss defeated:** Auto transition to Zone 2
3. **Zone 2:** Load với Spider Boss, Floor 11-20
4. **Continue:** Zone 2 → Zone 3 → ... → Zone 10

## 📁 File Structure

```
Assets/Scripts/MapSystem/
├── Core Scripts/
│   ├── MapManager.cs          # Main zone/floor manager
│   ├── MapGenerator.cs        # Map generation với zone logic
│   ├── MapView.cs            # Map display với floor display
│   ├── MapPlayerTracker.cs   # Player progression
│   └── MapTravel.cs          # Scene transition logic
├── Configuration/
│   ├── MapConfig.cs          # Zone/Boss configurations
│   ├── NodeBlueprint.cs      # Node definitions
│   └── MapLayer.cs           # Layer definitions
└── README_ZoneFloorSystem.md # This file
```

## 🔧 Customization

### Custom Zone Names
Edit `GetZoneName()` method trong MapManager:
```csharp
private string GetZoneName(int zoneNumber)
{
    switch (zoneNumber)
    {
        case 1: return "Tutorial Zone";
        case 2: return "Forest Zone";
        case 3: return "Cave Zone";
        case 4: return "Mountain Zone";
        // Add more...
    }
}
```

### Custom Node Probabilities
Edit `GetNodeTypeByGDDProbabilities()` method trong MapGenerator:
```csharp
case FloorRegion.Early:
    // Custom probabilities for early floors
    if (randomValue < 0.50f) return NodeType.MinorEnemy;
    if (randomValue < 0.70f) return NodeType.Mystery;
    // ...
```

### Custom Scene Names
Edit `GetSceneNameForZone()` method trong MapManager:
```csharp
private string GetSceneNameForZone(int zoneNumber)
{
    switch (zoneNumber)
    {
        case 1: return "MapScene";
        case 2: return "Zone2";
        case 3: return "Zone3";
        // Add more...
    }
}
```

## 🐛 Troubleshooting

### Common Issues

#### 1. Floor không advance sau boss
**Symptoms:** Floor 10 không chuyển sang Floor 11
**Solution:** 
- Check console logs để xem boss có được detect đúng không
- Ensure boss node có `NodeType.Boss`
- Check `AdvanceFloor()` logic

#### 2. Boss không đúng theo zone
**Symptoms:** Zone 2 vẫn hiển thị Skeleton Boss thay vì Spider Boss
**Solution:**
- Check Zone Configuration trong MapConfig
- Ensure boss blueprints được assign đúng
- Check MapGenerator boss selection logic

#### 3. Map data bị share giữa zones
**Symptoms:** Zone 2 hiển thị Floor 18 thay vì Floor 11
**Solution:**
- Check zone-specific save keys
- Ensure mỗi zone có map data riêng
- Check scene detection logic

#### 4. Scene transition không hoạt động
**Symptoms:** Không chuyển scene sau boss
**Solution:**
- Check scene names trong build settings
- Ensure `GetSceneNameForZone()` method đúng
- Check `TransitionToNextZone()` logic

### Debug Commands
```csharp
// In MapManager
[ContextMenu("Debug Zone/Floor Info")]
public void DebugZoneFloorInfo()

[ContextMenu("Test Zone Transition")]
public void TestZoneTransition()

[ContextMenu("Setup Default Zone Configs")]
public void SetupDefaultZoneConfigs()
```

## 📝 Notes

- **Zone Detection:** System tự động detect zone từ scene name
- **Map Persistence:** Mỗi zone có map data riêng biệt
- **Boss Priority:** Floor-specific config > Zone config > Random fallback
- **Scene Transition:** Tự động chuyển scene khi hoàn thành zone
- **Floor Display:** Hiển thị real-time khi player di chuyển

## 🎯 Best Practices

1. **Always test zone transitions** trước khi build
2. **Setup zone configs** trước khi test gameplay
3. **Use debug commands** để troubleshoot issues
4. **Keep scene names consistent** với zone numbers
5. **Test boss configurations** cho từng zone
6. **Verify build settings** include tất cả zone scenes
