using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using DG.Tweening;
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
            // Detect current zone from scene name first
            DetectZoneFromSceneName();
            
            // Load Zone/Floor System data
            LoadZoneFloorData();
            
            // Load zone-specific map
            string mapKey = $"Zone{currentZone}_Map";
            if (PlayerPrefs.HasKey(mapKey))
            {
                string mapJson = PlayerPrefs.GetString(mapKey);
                Map map = JsonConvert.DeserializeObject<Map>(mapJson);
                // using this instead of .Contains()
                if (map.path.Any(p => p.Equals(map.GetBossNode().point)))
                {
                    // player has already reached the boss, generate a new map
                    GenerateNewMap();
                }
                else
                {
                    CurrentMap = map;
                    // player has not reached the boss yet, load the current map
                    view.ShowMap(map);
                }
            }
            else
            {
                // No saved map for this zone, generate new one
                GenerateNewMap();
            }

            // After map is loaded, check for pending completion from battle scenes
            bool hasPendingCompletion = MapTravel.TryApplyPendingCompletion(this);
            if (hasPendingCompletion)
            {
                Debug.Log("Applied pending node completion from battle scene");
            }
        }
        
        private void DetectZoneFromSceneName()
        {
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"Current scene: {sceneName}");
            
            // Extract zone number from scene name (e.g., "Zone2" -> 2, "MapScene" -> 1)
            if (sceneName.StartsWith("Zone"))
            {
                string zoneString = sceneName.Substring(4); // Remove "Zone" prefix
                if (int.TryParse(zoneString, out int detectedZone))
                {
                    if (detectedZone != currentZone)
                    {
                        Debug.Log($"Zone mismatch! Scene suggests Zone {detectedZone}, but current zone is {currentZone}");
                        currentZone = detectedZone;
                        currentFloor = 1; // Reset to floor 1 for new zone
                        SaveMap(); // Save the zone change
                    }
                }
            }
            else
            {
                Debug.LogWarning($"Unknown scene name: {sceneName}. Expected format: Zone1, Zone2, etc.");
            }
        }
        
        private void LoadZoneFloorData()
        {
            // Load zone-specific data
            string zoneKey = $"Zone{currentZone}_Floor";
            string steadfastKey = $"Zone{currentZone}_SteadfastHeart";
            
            currentFloor = PlayerPrefs.GetInt(zoneKey, 1);
            steadfastHeartRestores = PlayerPrefs.GetInt(steadfastKey, maxSteadfastHeartRestores);
            
            Debug.Log($"Loaded Zone {currentZone} data: Floor {currentFloor}, Steadfast Heart: {steadfastHeartRestores}");
        }

        public void GenerateNewMap()
        {
            Map map = MapGenerator.GetMap(config);
            CurrentMap = map;
            Debug.Log($"Generated new map for Zone {currentZone} ({GetCurrentZoneName()})");
            view.ShowMap(map);
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

        public void SaveMap()
        {
            if (CurrentMap == null) return;

            // Save zone-specific map data
            string mapKey = $"Zone{currentZone}_Map";
            string json = JsonConvert.SerializeObject(CurrentMap, Formatting.Indented,
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });
            PlayerPrefs.SetString(mapKey, json);
            
            // Save Zone/Floor System data (zone-specific)
            string zoneKey = $"Zone{currentZone}_Floor";
            string steadfastKey = $"Zone{currentZone}_SteadfastHeart";
            
            PlayerPrefs.SetInt("CurrentZone", currentZone);
            PlayerPrefs.SetInt(zoneKey, currentFloor);
            PlayerPrefs.SetInt(steadfastKey, steadfastHeartRestores);
            
            PlayerPrefs.Save();
            
            Debug.Log($"Saved Zone {currentZone} data: Floor {currentFloor}, Map: {mapKey}");
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
        
        public void RestoreSteadfastHeart()
        {
            if (steadfastHeartRestores > 0)
            {
                steadfastHeartRestores--;
                Debug.Log($"Steadfast Heart restored! Remaining: {steadfastHeartRestores}");
            }
            else
            {
                Debug.LogWarning("No more Steadfast Heart restores available!");
            }
        }
        
        public void ResetSteadfastHeartRestores()
        {
            // GDD: Reset to 3 when reaching checkpoint
            steadfastHeartRestores = maxSteadfastHeartRestores;
            Debug.Log("Steadfast Heart restores reset to maximum!");
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
                    ResetSteadfastHeartRestores();
                }
                
                // Transition to next zone scene
                TransitionToNextZone();
            }
            else
            {
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
            }
            else
            {
                Debug.LogWarning($"No scene found for Zone {currentZone}! Staying in current scene.");
                // Generate new map for current zone if no scene transition
                GenerateNewMap();
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
