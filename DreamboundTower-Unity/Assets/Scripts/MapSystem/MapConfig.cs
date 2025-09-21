using System.Collections.Generic;
using UnityEngine;

namespace Map
{
    [CreateAssetMenu]
    public class MapConfig : ScriptableObject
    {
        public List<NodeBlueprint> nodeBlueprints;
        [Tooltip("Nodes that will be used on layers with Randomize Nodes > 0")]
        public List<NodeType> randomNodes = new List<NodeType>
            {NodeType.Mystery, NodeType.Store, NodeType.Treasure, NodeType.MinorEnemy, NodeType.RestSite};
        public int GridWidth => Mathf.Max(numOfPreBossNodes.max, numOfStartingNodes.max);

        [Header("Number of Pre-Boss Nodes")]
        public IntMinMax numOfPreBossNodes;
        [Header("Number of Starting Nodes")]
        public IntMinMax numOfStartingNodes;

        [Tooltip("Increase this number to generate more paths")]
        public int extraPaths;
        public List<MapLayer> layers;
        
        [Header("Boss Blueprint Configuration")]
        [Tooltip("Drag and drop Boss Node Blueprints for specific floors (10, 20, 30, etc.)")]
        public List<BossFloorConfig> bossFloorConfigs = new List<BossFloorConfig>();
        
        [Header("Zone Configuration")]
        [Tooltip("Configuration for each zone (Zone 1, Zone 2, Zone 3, etc.)")]
        public List<ZoneConfig> zoneConfigs = new List<ZoneConfig>();
    }
    
    [System.Serializable]
    public class BossFloorConfig
    {
        [Tooltip("Floor number (10, 20, 30, etc.)")]
        public int floorNumber;
        [Tooltip("Boss Node Blueprint for this floor")]
        public NodeBlueprint bossBlueprint;
        
        public BossFloorConfig(int floor, NodeBlueprint blueprint)
        {
            floorNumber = floor;
            bossBlueprint = blueprint;
        }
    }
    
    [System.Serializable]
    public class ZoneConfig
    {
        [Tooltip("Zone number (1, 2, 3, etc.)")]
        public int zoneNumber;
        [Tooltip("Boss Node Blueprint for this zone (boss at floor 10, 20, 30, etc.)")]
        public NodeBlueprint zoneBossBlueprint;
        [Tooltip("Zone name for display")]
        public string zoneName;
        
        public ZoneConfig(int zone, NodeBlueprint bossBlueprint, string name = "")
        {
            zoneNumber = zone;
            zoneBossBlueprint = bossBlueprint;
            zoneName = string.IsNullOrEmpty(name) ? $"Zone {zone}" : name;
        }
    }
}

