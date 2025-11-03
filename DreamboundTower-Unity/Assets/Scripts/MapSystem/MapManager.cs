using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using DG.Tweening;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Map
{
    public class MapManager : MonoBehaviour
    {
        public MapConfig config;
        public MapView view;

        public Map CurrentMap { get; private set; }
        
        // Singleton instance for MapGenerator access
        public static MapManager Instance { get; private set; }
        
        // Zone/Floor System - GDD Implementation
        [Header("Zone/Floor System")]
        public int currentZone = 1;           // 1, 2, 3, 4...
        public int currentFloor = 1;          // 1-10 per zone
        public int totalFloorsPerZone = 10;   // GDD: ~10 floors per zone
        public int totalNodesPerFloor = 5;    // GDD: 4-6 nodes per floor (default 5)

        [Header("Zone/Floor System")]
        [Tooltip("BẬT CÁI NÀY ĐỂ TEST SCENE NÀY RIÊNG LẺ")]
        public bool overrideZoneForTesting = false;
        [Tooltip("Zone (1-10) để test")]
        public int testZone = 1;

        // Checkpoint System - GDD: Checkpoints at floors 1, 11, 21, 31...
        public int steadfastHeartRestores = 3; // GDD: 3 times per checkpoint
        public int maxSteadfastHeartRestores = 3;

        private void Awake()
        {
            Instance = this;
            DOTween.Init();
        }
        private void Start()
        {
            if (overrideZoneForTesting)
            {
                Debug.LogWarning($"---!!! TEST MODE ON: ĐANG ÉP TẢI ZONE {testZone} !!!---");
                if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
                {
                    // Ghi đè RunData để các script khác (như MapGenerator) đọc
                    GameManager.Instance.currentRunData.mapData.currentZone = testZone;
                    // Xóa map JSON cũ để buộc tạo map mới
                    GameManager.Instance.currentRunData.mapData.currentMapJson = null;
                }
                else
                {
                    // Nếu không có GameManager (chạy test scene riêng lẻ), 
                    // ít nhất cũng phải đặt currentZone của MapManager
                    currentZone = testZone;
                }
            }
            if (GameManager.Instance == null || GameManager.Instance.currentRunData == null)
            {
                Debug.LogError("GameManager hoặc RunData không tồn tại! Không thể khởi tạo bản đồ.");
                return;
            }

            // BƯỚC 1: LUÔN LUÔN ĐỒNG BỘ TRẠNG THÁI TỪ RUNDATA TRƯỚC TIÊN
            var runData = GameManager.Instance.currentRunData;
            currentZone = runData.mapData.currentZone;
            currentFloor = runData.mapData.currentFloorInZone;

            // BƯỚC 2: KIỂM TRA XEM CÓ BẢN ĐỒ CŨ ĐỂ TẢI KHÔNG
            string mapJson = runData.mapData.currentMapJson;
            if (!string.IsNullOrEmpty(mapJson))
            {
                Debug.Log($"Đang tải bản đồ đã lưu cho Zone {currentZone}...");
                Map map = JsonConvert.DeserializeObject<Map>(mapJson);
                // --- Sanitize saved path to avoid invalid points like (-1,-1) or nodes not in this map ---
                if (runData.mapData.path == null) runData.mapData.path = new System.Collections.Generic.List<Vector2Int>();
                var cleanedPath = new System.Collections.Generic.List<Vector2Int>();
                foreach (var p in runData.mapData.path)
                {
                    if (p.x < 0 || p.y < 0) continue; // drop (-1,-1) markers
                    Node n = map.GetNode(p);
                    if (n != null) cleanedPath.Add(p);
                }
                map.path = cleanedPath; // use sanitized path
                runData.mapData.path = cleanedPath; // write back

                CurrentMap = map;
                view.ShowMap(map); // Bây giờ ShowMap sẽ có dữ liệu Zone chính xác
            }
            // BƯỚC 3: NẾU KHÔNG, TẠO MỘT BẢN ĐỒ MỚI
            else
            {
                Debug.Log($"Không có bản đồ đã lưu. Đang tạo bản đồ mới cho Zone {currentZone}...");
                GenerateNewMap();
            }
        }
        public void GenerateNewMap()
        {
            Map map = MapGenerator.GetMap(config);
            CurrentMap = map;
            Debug.Log($"Generated new map for Zone {currentZone} ({GetCurrentZoneName()})");
            view.ShowMap(map);
            // new map starts at floor 1 for this zone
            currentFloor = 1;
            // QUAN TRỌNG: Reset lại path trong RunData khi tạo map mới
            if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
            {
                GameManager.Instance.currentRunData.mapData.path.Clear();
                GameManager.Instance.currentRunData.mapData.pendingNodePoint = new Vector2Int(-1, -1);
            }

            SaveMap();
        }
        
        public void GenerateNewMapForZone(int zoneNumber)
        {
            if (zoneNumber != currentZone)
            {
                Debug.Log($"Changing zone from {currentZone} to {zoneNumber}");
                currentZone = zoneNumber;
                currentFloor = 1;
                SaveMap();
            }
            
            GenerateNewMap();
        }

        // Trong file MapManager.cs

        public void SaveMap()
        {
            if (CurrentMap == null) return;
            if (GameManager.Instance == null || GameManager.Instance.currentRunData == null)
            {
                Debug.LogWarning("Cannot save map, GameManager or RunData is missing.");
                return;
            }

            // --- PHẦN SỬA LỖI ---
            // 1. Chuyển đổi bản đồ hiện tại thành JSON
            string json = JsonConvert.SerializeObject(CurrentMap, Formatting.Indented,
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            // 2. Lấy "hộp" RunData từ GameManager
            var runData = GameManager.Instance.currentRunData;

            // 3. Cập nhật tất cả thông tin liên quan đến map vào "bản thiết kế" RunData
            runData.mapData.currentMapJson = json;
            runData.mapData.currentZone = currentZone;
            runData.mapData.currentFloorInZone = currentFloor;
            runData.mapData.path = CurrentMap.path; // Đảm bảo path luôn được cập nhật
            runData.mapData.lastKnownScene = SceneManager.GetActiveScene().name; // Thay thế cho UpdateLastScene

            // 4. Gọi hàm lưu game mới, truyền vào toàn bộ "hộp" RunData
            RunSaveService.SaveRun(runData);

            // Dòng debug cũ có thể giữ lại hoặc sửa đổi
            Debug.Log($"Saved data for Zone {currentZone} into RunData object.");
        }   

        private void OnApplicationQuit()
        {
            SaveMap();
        }
        
        // Zone/Floor System Methods
        public void SetCurrentZoneFloor(int zone, int floor)
        {
            currentZone = zone;
            currentFloor = floor;
        }
        
        public int GetAbsoluteFloor()
        {
            // Convert zone/floor to absolute floor number (1-10, 11-20, 21-30...)
            return (currentZone - 1) * totalFloorsPerZone + currentFloor;
        }
        
        public int GetFloorFromNodePosition(Vector2Int nodePoint)
        {
            // Convert node position to floor number
            // Assuming each layer represents a floor
            return nodePoint.y + 1;
        }
        
        public int GetAbsoluteFloorFromNodePosition(Vector2Int nodePoint)
        {
            // Convert node position to absolute floor number
            int floorInCurrentZone = GetFloorFromNodePosition(nodePoint);
            return (currentZone - 1) * totalFloorsPerZone + floorInCurrentZone;
        }
        
        public bool IsValidFloorForCurrentZone(int absoluteFloor)
        {
            // Check if the floor belongs to current zone
            int floorStart = (currentZone - 1) * totalFloorsPerZone + 1;
            int floorEnd = currentZone * totalFloorsPerZone;
            return absoluteFloor >= floorStart && absoluteFloor <= floorEnd;
        }
        
        public int GetFloorRangeForCurrentZone()
        {
            // Get the floor range for current zone
            int floorStart = (currentZone - 1) * totalFloorsPerZone + 1;
            int floorEnd = currentZone * totalFloorsPerZone;
            return floorStart;
        }
        
        public bool IsCheckpointFloor()
        {
            // GDD: Checkpoints at floors 1, 11, 21, 31... (every 10 floors)
            int absoluteFloor = GetAbsoluteFloor();
            return absoluteFloor % 10 == 1; // 1, 11, 21, 31...
        }
        
        public bool IsBossFloor()
        {
            // GDD: Boss at end of each zone (floors 10, 20, 30...)
            return currentFloor == totalFloorsPerZone;
        }

        public void AdvanceFloor()
        {
            currentFloor++;
            
            if (currentFloor > totalFloorsPerZone)
            {
                Debug.Log($"Zone {currentZone} completed! Moving to Zone {currentZone + 1}...");
                
                // Move to next zone
                currentZone++;
                currentFloor = 1;
                
                // Reset Steadfast Heart at zone start (checkpoint)
                if (IsCheckpointFloor())
                {
                    GameManager.Instance.RestoreSteadfastHeart();
                }
                
                // Transition to next zone scene
                TransitionToNextZone();
            }
            else
            {
                // keep state in sync
                SaveMap();
                // Update floor display for current zone
                if (view != null)
                {
                    view.UpdateFloorDisplay();
                }
            }
        }
        
        private void TransitionToNextZone()
        {
            Debug.Log($"Transitioning to Zone {currentZone}...");
            
            // Save current zone data before transition
            SaveMap();
            
            // Load next zone scene
            string nextSceneName = GetSceneNameForZone(currentZone);
            
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"Loading scene: {nextSceneName}");
                
                // Check if we're in Play mode or Editor mode
                if (Application.isPlaying)
                {
                    // Use SceneManager for Play mode
                    UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
                }
                else
                {
                    // Use EditorSceneManager for Editor mode
                    #if UNITY_EDITOR
                    string scenePath = $"Assets/Scenes/MapScene/{nextSceneName}.unity";
                    UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                    #endif
                }
                return;
            }
            else
            {
                Debug.LogWarning($"No scene found for Zone {currentZone}! Staying in current scene.");
                // Generate new map for current zone if no scene transition
                GenerateNewMap();
                return;
            }
        }
        
        private string GetSceneNameForZone(int zoneNumber)
        {
            // Map zone numbers to scene names (Zone1 = Floors 1-10, Zone2 = Floors 11-20, etc.)
            if (zoneNumber >= 1 && zoneNumber <= 10)
            {
                return $"Zone{zoneNumber}";
            }
            return ""; // No scene for this zone
        }
        
        // GDD Node Type Probabilities by Floor Region
        public enum FloorRegion
        {
            Early,  // Floors 1-20
            Mid,    // Floors 21-60  
            Late    // Floors 61-100
        }
        
        public FloorRegion GetFloorRegion()
        {
            int absoluteFloor = GetAbsoluteFloor();
            if (absoluteFloor <= 20) return FloorRegion.Early;
            if (absoluteFloor <= 60) return FloorRegion.Mid;
            return FloorRegion.Late;
        }
        
        public string GetCurrentZoneName()
        {
            if (config == null || config.zoneConfigs == null) return $"Zone {currentZone}";
            
            var zoneConfig = config.zoneConfigs.FirstOrDefault(zc => zc.zoneNumber == currentZone);
            if (zoneConfig != null && !string.IsNullOrEmpty(zoneConfig.zoneName))
            {
                return zoneConfig.zoneName;
            }
            
            return GetZoneName(currentZone);
        }

        // Keep currentFloor in sync with the last visited node in this zone
        private void SyncFloorWithCurrentPath()
        {
            if (CurrentMap == null) return;
            if (CurrentMap.path == null || CurrentMap.path.Count == 0)
            {
                currentFloor = 1;
                return;
            }

            Vector2Int lastPoint = CurrentMap.path[CurrentMap.path.Count - 1];
            // node.point.y starts at 0 for first layer, so +1 → floor-in-zone (1..10)
            int floorInZone = lastPoint.y + 1;
            // clamp to [1, totalFloorsPerZone]
            floorInZone = Mathf.Clamp(floorInZone, 1, totalFloorsPerZone);
            currentFloor = floorInZone;
            SaveMap();
        }
        
        // Debug/Test Methods
        [ContextMenu("Debug Zone/Floor Info")]
        public void DebugZoneFloorInfo()
        {
            Debug.Log($"=== Zone/Floor System Debug ===");
            Debug.Log($"Current Zone: {currentZone} ({GetCurrentZoneName()})");
            Debug.Log($"Current Floor: {currentFloor}");
            Debug.Log($"Absolute Floor: {GetAbsoluteFloor()}");
            Debug.Log($"Floor Region: {GetFloorRegion()}");
            Debug.Log($"Is Checkpoint Floor: {IsCheckpointFloor()}");
            Debug.Log($"Is Boss Floor: {IsBossFloor()}");
            Debug.Log($"Steadfast Heart Restores: {steadfastHeartRestores}/{maxSteadfastHeartRestores}");
            Debug.Log($"================================");
        }
        
        [ContextMenu("Test Advance Floor")]
        public void TestAdvanceFloor()
        {
            Debug.Log($"Before: Zone {currentZone}, Floor {currentFloor}");
            AdvanceFloor();
            Debug.Log($"After: Zone {currentZone}, Floor {currentFloor}");
            SaveMap();
        }
        
        [ContextMenu("Test Zone Progression")]
        public void TestZoneProgression()
        {
            Debug.Log("=== Testing Zone Progression ===");
            Debug.Log($"Starting: Zone {currentZone} ({GetCurrentZoneName()}), Floor {currentFloor}");
            
            // Advance through all floors in current zone
            for (int i = currentFloor; i <= totalFloorsPerZone; i++)
            {
                AdvanceFloor();
                Debug.Log($"After floor {i}: Zone {currentZone} ({GetCurrentZoneName()}), Floor {currentFloor}");
            }
            
            Debug.Log("=== Zone Progression Test Complete ===");
        }
        
        [ContextMenu("Test Zone Transition")]
        public void TestZoneTransition()
        {
            Debug.Log("=== Testing Zone Transition ===");
            Debug.Log($"Current: Zone {currentZone}, Floor {currentFloor}");
            
            // Simulate completing current zone
            currentFloor = totalFloorsPerZone; // Set to last floor
            AdvanceFloor(); // This should trigger zone transition
            
            Debug.Log("=== Zone Transition Test Complete ===");
        }
        
        [ContextMenu("Test Editor Zone Transition")]
        public void TestEditorZoneTransition()
        {
            Debug.Log("=== Testing Editor Mode Zone Transition ===");
            Debug.Log($"Current: Zone {currentZone}, Floor {currentFloor}");
            
            // Force zone transition without going through AdvanceFloor
            Debug.Log($"Zone {currentZone} completed! Transitioning to Zone {currentZone + 1}...");
            currentZone++;
            currentFloor = 1;
            
            // Save current zone data before transition
            SaveMap();
            
            // Load next zone scene
            string nextSceneName = GetSceneNameForZone(currentZone);
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log($"Loading scene: {nextSceneName}");
                
                // Use EditorSceneManager for Editor mode
                #if UNITY_EDITOR
                string scenePath = $"Assets/Scenes/MapScene/{nextSceneName}.unity";
                Debug.Log($"Editor mode: Opening scene at {scenePath}");
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
                #endif
            }
            else
            {
                Debug.LogWarning($"No scene found for Zone {currentZone}!");
            }
            
            Debug.Log("=== Editor Zone Transition Test Complete ===");
        }
        
        [ContextMenu("Test Play Mode Zone Transition")]
        public void TestPlayModeZoneTransition()
        {
            Debug.Log("=== Testing Play Mode Zone Transition ===");
            Debug.Log($"Current: Zone {currentZone}, Floor {currentFloor}");
            
            // Simulate completing Zone 1, Floor 10
            currentZone = 1;
            currentFloor = 10;
            
            Debug.Log("Simulating boss defeat at Floor 10...");
            AdvanceFloor(); // This should trigger zone transition
            
            Debug.Log("=== Play Mode Zone Transition Test Complete ===");
        }
        
        [ContextMenu("Generate Map for Zone 1")]
        public void GenerateZone1Map()
        {
            GenerateNewMapForZone(1);
        }
        
        [ContextMenu("Generate Map for Zone 2")]
        public void GenerateZone2Map()
        {
            GenerateNewMapForZone(2);
        }
        
        [ContextMenu("Generate Map for Zone 3")]
        public void GenerateZone3Map()
        {
            GenerateNewMapForZone(3);
        }
        
        [ContextMenu("Test Node Probabilities")]
        public void TestNodeProbabilities()
        {
            MapGenerator.DebugNodeProbabilities(1000);
        }
        
        [ContextMenu("Setup Default Boss Configs")]
        public void SetupDefaultBossConfigs()
        {
            if (config == null) return;
            
            // Clear existing configs
            config.bossFloorConfigs.Clear();
            
            // Add default boss configurations for floors 10, 20, 30, 40, 50, 60, 70, 80, 90, 100
            var bossBlueprints = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList();
            if (bossBlueprints.Count > 0)
            {
                for (int floor = 10; floor <= 100; floor += 10)
                {
                    // Cycle through available boss blueprints
                    var bossBlueprint = bossBlueprints[(floor / 10 - 1) % bossBlueprints.Count];
                    config.bossFloorConfigs.Add(new BossFloorConfig(floor, bossBlueprint));
                }
            }
            
            Debug.Log($"Setup {config.bossFloorConfigs.Count} default boss configurations");
        }
        
        [ContextMenu("Setup Default Zone Configs")]
        public void SetupDefaultZoneConfigs()
        {
            if (config == null) return;
            
            // Clear existing zone configs
            config.zoneConfigs.Clear();
            
            // Add default zone configurations for zones 1-10
            var bossBlueprints = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList();
            if (bossBlueprints.Count > 0)
            {
                for (int zone = 1; zone <= 10; zone++)
                {
                    // Cycle through available boss blueprints
                    var bossBlueprint = bossBlueprints[(zone - 1) % bossBlueprints.Count];
                    string zoneName = GetZoneName(zone);
                    config.zoneConfigs.Add(new ZoneConfig(zone, bossBlueprint, zoneName));
                }
            }
            
            Debug.Log($"Setup {config.zoneConfigs.Count} default zone configurations");
        }
        
        private string GetZoneName(int zoneNumber)
        {
            // Custom zone names for better gameplay experience
            switch (zoneNumber)
            {
                case 1: return "Tutorial Zone";
                case 2: return "Forest Zone";
                case 3: return "Cave Zone";
                case 4: return "Mountain Zone";
                case 5: return "Desert Zone";
                case 6: return "Volcano Zone";
                case 7: return "Ice Zone";
                case 8: return "Shadow Zone";
                case 9: return "Light Zone";
                case 10: return "Final Zone";
                default: return $"Zone {zoneNumber}";
            }
        }
    }
}
