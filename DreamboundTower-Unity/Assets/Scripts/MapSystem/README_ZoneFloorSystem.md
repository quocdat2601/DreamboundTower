# Zone/Floor System - Hướng dẫn Setup

## 📋 Tổng quan

Zone/Floor System cho phép tạo nhiều zone với boss khác nhau, mỗi zone có 10 floors. Hệ thống tự động quản lý map generation, node probabilities, và zone transitions.

## 🏗️ Cấu trúc Zone/Floor

### Zone Structure
- **Zone 1:** Floors 1-10 (Scene: Zone1)
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

### 2. Setup Zone Configurations (Manual)
1. Select MapConfig asset
2. Expand **"Zone Configuration"**
3. Add elements:
   - **Element 0:** Zone Number = 1, Boss Blueprint = Skeleton Boss, Zone Name = "Tutorial Zone"
   - **Element 1:** Zone Number = 2, Boss Blueprint = Spider Boss, Zone Name = "Forest Zone"
   - **Element 2:** Zone Number = 3, Boss Blueprint = Executioner Boss, Zone Name = "Cave Zone"
   - **...**

### 3. (Optional) Boss per-floor override
Chỉ dùng khi muốn OVERRIDE boss mặc định theo floor cụ thể:
1. Select MapConfig asset → **Boss Blueprint Configuration**
2. Add per-floor nếu cần (ví dụ Floor 30 dùng boss khác với Zone 3):
   - Nếu để trống, hệ thống sẽ dùng Zone Config tương ứng.

### 4. Setup Scenes (Zone1…Zone10)

#### A. Create Zone Scenes
1. Tạo các scenes: `Zone1`, `Zone2`, `Zone3`, …, `Zone10` (mỗi scene đại diện cho 1 zone = 10 floors)
2. Trong mỗi scene, tạo GameObject `MapRoot` (hoặc dùng prefab) chứa:
   - `MapManager` (component)
   - `MapView` (component) + tham chiếu `MapManager`
   - `MapObjects` (node prefab, line prefab, background nếu có)
3. Assign `MapManager.config` tới `MapConfig` dùng chung (cùng 10 layers cho 10 floors của 1 zone)

#### B. Add to Build Settings
1. File → Build Settings
2. Add scenes: `Scenes/MapScene/Zone1` … `Scenes/MapScene/Zone10`
3. Đặt `Zone1` là scene đầu vào nếu muốn bắt đầu ở Zone 1

#### C. Scene → Zone Mapping (tự động)
- Hệ thống sẽ tự detect zone từ tên scene: `Zone2` → zone = 2, `Zone7` → zone = 7
- Khi load scene `ZoneN`, `MapManager` sẽ set `currentZone = N`, `currentFloor = 1`
- Mỗi scene chỉ quản lý map của zone tương ứng (không chia sẻ map giữa các scene)

### 4.1. Cách hoạt động chuyển scene (Zone Transition)

- Khi đánh boss ở floor cuối (floor 10 của zone hiện tại), `MapManager.AdvanceFloor()` sẽ:
  - Tăng `currentFloor`
  - Nếu `currentFloor > 10`: tăng `currentZone`, reset `currentFloor = 1`
  - Gọi `TransitionToNextZone()` để load scene tiếp theo: `Zone{currentZone}`
- Tên scene được xác định bởi: `GetSceneNameForZone(int zone) => $"Zone{zone}"` (Zone1…Zone10)
- Dữ liệu map được lưu theo zone key: `Zone{zone}_Map`, floor theo `Zone{zone}_Floor`

### 4.2. Setup Map cho từng Scene (ví dụ Zone1, Zone2)

- `Zone1` (Floors 1–10):
  - Trong `MapConfig.layers`: giữ 10 layers (mỗi layer = 1 floor)
  - Boss floor (layer 10) sẽ được gán Boss theo `ZoneConfig` hoặc `BossFloorConfig` (xem mục 2 và 3)
  - Khi vào `Zone1`, hệ thống sẽ load `Zone1_Map` nếu có, hoặc generate mới

- `Zone2` (Floors 11–20):
  - Dùng cùng `MapConfig` (10 layers)
  - Boss sẽ lấy từ `ZoneConfig` (Zone2) hoặc `BossFloorConfig` (Floor 20)
  - Hệ thống lưu/đọc `Zone2_Map`, `Zone2_Floor`

### 5. Setup MapManager & MapView

#### A. MapManager Component
1. **Config:** Assign MapConfig asset
2. **View:** Assign MapView component
3. **Zone/Floor System:**
   - Không cần set tay `currentZone`/`currentFloor` (sẽ auto-detect từ scene name `ZoneN`)
   - `totalFloorsPerZone`: 10 (mặc định)
   - `totalNodesPerFloor`: 5 (tuỳ chọn hiển thị)
4. **Scene transition:** Không cần viết code thêm. Hệ thống tự gọi `TransitionToNextZone()` khi qua boss floor.

#### B. MapView Component
1. **Floor Display:**
   - `floorDisplayText`: Assign UI Text (optional)
   - `floorDisplayFormat`: "Floor {0}"
   - `floorDisplayFontSize`: 24
   - `floorDisplayColor`: White

## 🧪 Testing

1. Mở `Zone1` → Generate map → chơi tới Boss (floor 10) → hệ thống sẽ tự chuyển `Zone2`.
2. Trong `Zone2`, chơi tới Boss (floor 20) → hệ thống tự chuyển `Zone3`.
3. Lặp lại cho tới `Zone10`.

## 🔒 Zone/Map hoạt động độc lập (Isolation)

Để mỗi zone có map và tiến trình độc lập, hệ thống sử dụng 3 nguyên tắc sau:

1) Scene → Zone detection
- Tên scene phải theo dạng `ZoneN` (Zone1, Zone2, … Zone10).
- `MapManager` đọc tên scene để set `currentZone = N` và reset `currentFloor = 1` nếu cần.

2) Zone-specific save keys (PlayerPrefs)
- Map và tiến trình được lưu theo key riêng cho từng zone, vì vậy các zone không ảnh hưởng nhau:
  - `Zone{N}_Map`        → JSON của map cho zone N
  - `Zone{N}_Floor`      → floor hiện tại (1–10) trong zone N
  - `Zone{N}_SteadfastHeart` → số lần hồi Steadfast còn lại trong zone N
- Ví dụ zone 2 sẽ dùng `Zone2_Map`, `Zone2_Floor`, `Zone2_SteadfastHeart` (không đụng vào zone 1).

3) Khởi tạo & đồng bộ floor theo path
- Khi generate map mới ở một zone: `currentFloor = 1`.
- Khi load map đã có path: hệ thống tự đồng bộ `currentFloor` = layer của node cuối cùng trong `path` (y + 1).
- Khi chọn node mới: `currentFloor` được cập nhật theo node vừa chọn và lưu ngay.

Kết quả: Mỗi scene `ZoneN` sẽ luôn tải đúng map & tiến trình của zone N, không chia sẻ hay ghi đè lẫn nhau.

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

### Scene Naming
Mỗi scene phải đặt tên chính xác `Zone1`, `Zone2`, …, `Zone10`. `MapManager` sẽ dùng tên này để:
- Detect zone hiện tại (`ZoneN` → `currentZone = N`)
- Tự chuyển scene khi hoàn thành zone (`LoadScene("Zone{next}")`)

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

### Debug Commands (tuỳ chọn dành cho dev)
- Right-click `MapManager` trong Inspector:
  - `Debug Zone/Floor Info`
  - `Setup Default Zone Configs`

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
