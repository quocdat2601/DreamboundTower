# Map System - Slay the Spire Style

Map System đã được copy từ Slay the Spire project và tích hợp vào DreamboundTower.

## 📁 Cấu trúc thư mục

```
MapSystem/
├── Core Scripts/
│   ├── Map.cs                    - Data structure cho map
│   ├── Node.cs                   - Data structure cho node
│   ├── NodeBlueprint.cs          - ScriptableObject cho node types
│   ├── MapConfig.cs              - ScriptableObject cho map configuration
│   ├── MapLayer.cs               - Data structure cho map layers
│   ├── MapGenerator.cs           - Procedural map generation
│   ├── MapManager.cs             - Quản lý map chính
│   ├── MapView.cs                - Hiển thị map (3D/World)
│   ├── MapViewUI.cs              - Hiển thị map (UI/Canvas)
│   ├── MapNode.cs                - Node trên map với animations
│   ├── MapPlayerTracker.cs       - Theo dõi player progress
│   └── ScrollNonUI.cs            - Scroll system (đã sửa Input System)
├── UI Lines/
│   ├── UIPrimitiveBase.cs        - Base class cho UI primitives
│   ├── UILineRenderer.cs         - UI line rendering
│   └── Utilities/
│       ├── BezierPath.cs         - Bezier curve utilities
│       ├── CableCurve.cs         - Cable curve utilities
│       └── SetPropertyUtility.cs - Property utilities
├── Utilities/
│   ├── MinMax.cs                 - Min/Max value utilities
│   ├── ShufflingExtension.cs     - List shuffling utilities
│   ├── LineConnection.cs         - Line connection data
│   └── DottedLineRenderer.cs     - Dotted line rendering
└── README.md                     - File này
```

## 🚀 Cách sử dụng

### 1. Tạo Map Config
1. Right-click trong Project window
2. Create > Map > MapConfig
3. Cấu hình các thông số:
   - `nodeBlueprints`: Danh sách các node types
   - `randomNodes`: Các node types có thể random
   - `numOfPreBossNodes`: Số node trước boss
   - `numOfStartingNodes`: Số node bắt đầu
   - `extraPaths`: Số đường dẫn thêm
   - `layers`: Các layer của map

### 2. Tạo Node Blueprints
1. Right-click trong Project window
2. Create > Map > NodeBlueprint
3. Cấu hình:
   - `sprite`: Sprite cho node
   - `nodeType`: Loại node (MinorEnemy, EliteEnemy, etc.)

### 3. Setup Map Manager
1. Tạo GameObject trong scene
2. Add component `MapManager`
3. Assign:
   - `config`: MapConfig đã tạo
   - `view`: MapView hoặc MapViewUI

### 4. Setup Map View
1. Tạo GameObject cho MapView
2. Add component `MapView` hoặc `MapViewUI`
3. Cấu hình:
   - `nodePrefab`: Prefab cho node
   - `linePrefab`: Prefab cho đường nối
   - `orientation`: Hướng map (BottomToTop, TopToBottom, etc.)
   - Colors và settings khác

### 5. Setup Map Player Tracker
1. Tạo GameObject
2. Add component `MapPlayerTracker`
3. Assign:
   - `mapManager`: MapManager reference
   - `view`: MapView reference

## 🎮 Node Types

- `MinorEnemy`: Enemy thường
- `EliteEnemy`: Enemy mạnh
- `RestSite`: Nơi nghỉ ngơi
- `Treasure`: Kho báu
- `Store`: Cửa hàng
- `Boss`: Boss
- `Mystery`: Bí ẩn

## 🔧 Dependencies

Map System sử dụng các packages sau:
- `com.unity.ai.navigation` (2.0.9)
- `com.unity.nuget.newtonsoft-json` (3.2.1)

**Lưu ý**: Map System đã được sửa để không phụ thuộc vào DOTween. Nếu bạn muốn có animations mượt mà, có thể cài đặt DOTween từ Asset Store và thêm lại các animations.

## ⚠️ Lưu ý

1. **Input System**: ScrollNonUI.cs đã được sửa để tương thích với Input System mới
2. **Namespace**: Tất cả scripts sử dụng namespace `Map`
3. **DOTween**: Không cần thiết - Map System hoạt động mà không cần DOTween
4. **Newtonsoft JSON**: Cần cho việc serialize/deserialize map data

## 🎯 Tính năng

- ✅ Procedural map generation
- ✅ Multiple map orientations
- ✅ Node state management (Locked, Visited, Attainable)
- ✅ Basic animations (có thể nâng cấp với DOTween)
- ✅ UI và World space rendering
- ✅ Scroll system
- ✅ Line connections giữa nodes
- ✅ Map persistence với PlayerPrefs
- ✅ Input System compatibility

## 📝 Example Usage

```csharp
// Generate new map
mapManager.GenerateNewMap();

// Save current map
mapManager.SaveMap();

// Select a node
MapPlayerTracker.Instance.SelectNode(mapNode);

// Get current map
Map currentMap = mapManager.CurrentMap;
```

## 🔄 Integration với DreamboundTower

Map System hoàn toàn độc lập và có thể tích hợp dễ dàng:
- Không xung đột với các hệ thống hiện tại
- Sử dụng namespace riêng `Map`
- Có thể mở rộng để tích hợp với Battle System, Inventory, etc.
