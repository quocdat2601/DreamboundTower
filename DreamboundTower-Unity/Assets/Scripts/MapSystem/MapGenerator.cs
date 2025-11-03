using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Map
{
    public static class MapGenerator
    {
        // GDD Floor Region enum
        public enum FloorRegion
        {
            Early,  // Floors 1-20
            Mid,    // Floors 21-60  
            Late    // Floors 61-100
        }
        private static MapConfig config;

        private static List<float> layerDistances;
        // ALL nodes by layer:
        private static readonly List<List<Node>> nodes = new List<List<Node>>();

        public static Map GetMap(MapConfig conf)
        {
            if (conf == null)
            {
                Debug.LogWarning("Config was null in MapGenerator.Generate()");
                return null;
            }

            config = conf;
            nodes.Clear();

            GenerateLayerDistances();

            for (int i = 0; i < conf.layers.Count; i++)
                PlaceLayer(i);

            List<List<Vector2Int>> paths = GeneratePaths();

            RandomizeNodePositions();

            SetUpConnections(paths);

            RemoveCrossConnections();

            // select all the nodes with connections:
            List<Node> nodesList = nodes.SelectMany(n => n).Where(n => n.incoming.Count > 0 || n.outgoing.Count > 0).ToList();

            // pick a random name of the boss level for this map:
            string bossNodeName = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList().Random().name;
            return new Map(conf.name, bossNodeName, nodesList, new List<Vector2Int>());
        }

        private static void GenerateLayerDistances()
        {
            layerDistances = new List<float>();
            foreach (MapLayer layer in config.layers)
                layerDistances.Add(layer.distanceFromPreviousLayer.GetValue());
        }

        private static float GetDistanceToLayer(int layerIndex)
        {
            if (layerIndex < 0 || layerIndex > layerDistances.Count) return 0f;

            return layerDistances.Take(layerIndex + 1).Sum();
        }

        private static void PlaceLayer(int layerIndex)
        {
            MapLayer layer = config.layers[layerIndex];
            List<Node> nodesOnThisLayer = new List<Node>();

            // offset of this layer to make all the nodes centered:
            float offset = layer.nodesApartDistance * config.GridWidth / 2f;

            for (int i = 0; i < config.GridWidth; i++)
            {
                NodeType nodeType;
                string blueprintName;
                
                // GDD Implementation: Boss cố định ở node cuối cùng (floor 10)
                if (layerIndex == config.layers.Count - 1 && i == config.GridWidth / 2)
                {
                    // Boss node ở cuối cùng, giữa layer
                    nodeType = NodeType.Boss;
                    
                    // Get Boss Blueprint from configuration or use random default
                    // For boss node, we need to calculate the absolute floor based on current zone
                    // Since we're generating a map for a specific zone, we need to get the zone's boss floor
                    int absoluteFloor = 10; // Default to floor 10 for zone 1
                    
                    // Try to get current zone from MapManager if available
                    if (MapManager.Instance != null)
                    {
                        absoluteFloor = MapManager.Instance.currentZone * 10; // Zone 1 = Floor 10, Zone 2 = Floor 20, etc.
                    }
                    
                    NodeBlueprint bossBlueprint = GetBossBlueprintForFloor(absoluteFloor);
                    if (bossBlueprint != null)
                    {
                        blueprintName = bossBlueprint.name;
                        Debug.Log($"Using configured boss blueprint: {blueprintName} for floor {absoluteFloor}");
                    }
                    else
                    {
                        // Fallback to random boss blueprint
                        blueprintName = config.nodeBlueprints.Where(b => b.nodeType == NodeType.Boss).ToList().Random().name;
                        Debug.Log($"No boss config found for floor {absoluteFloor}, using random: {blueprintName}");
                    }
                }
                // Node đầu tiên mặc định là MinorEnemy (tất cả nodes ở layer 0)
                else if (layerIndex == 0)
                {
                    nodeType = NodeType.MinorEnemy;
                    blueprintName = config.nodeBlueprints.Where(b => b.nodeType == NodeType.MinorEnemy).ToList().Random().name;
                }
                else
                {
                    // GDD Node Type Probabilities theo Floor Region
                    nodeType = GetNodeTypeByGDDProbabilities(layerIndex);
                    blueprintName = config.nodeBlueprints.Where(b => b.nodeType == nodeType).ToList().Random().name;
                }
                
                Node node = new Node(nodeType, blueprintName, new Vector2Int(i, layerIndex))
                {
                    position = new Vector2(-offset + i * layer.nodesApartDistance, GetDistanceToLayer(layerIndex))
                };
                nodesOnThisLayer.Add(node);
            }

            nodes.Add(nodesOnThisLayer);
        }

        private static void RandomizeNodePositions()
        {
            for (int index = 0; index < nodes.Count; index++)
            {
                List<Node> list = nodes[index];
                MapLayer layer = config.layers[index];
                float distToNextLayer = index + 1 >= layerDistances.Count
                    ? 0f
                    : layerDistances[index + 1];
                float distToPreviousLayer = layerDistances[index];

                foreach (Node node in list)
                {
                    float xRnd = Random.Range(-0.5f, 0.5f);
                    float yRnd = Random.Range(-0.5f, 0.5f);

                    float x = xRnd * layer.nodesApartDistance;
                    float y = yRnd < 0 ? distToPreviousLayer * yRnd: distToNextLayer * yRnd;

                    node.position += new Vector2(x, y) * layer.randomizePosition;
                }
            }
        }

        private static void SetUpConnections(List<List<Vector2Int>> paths)
        {
            foreach (List<Vector2Int> path in paths)
            {
                for (int i = 0; i < path.Count - 1; ++i)
                {
                    Node node = GetNode(path[i]);
                    Node nextNode = GetNode(path[i + 1]);
                    node.AddOutgoing(nextNode.point);
                    nextNode.AddIncoming(node.point);
                }
            }
        }

        private static void RemoveCrossConnections()
        {
            for (int i = 0; i < config.GridWidth - 1; ++i)
                for (int j = 0; j < config.layers.Count - 1; ++j)
                {
                    Node node = GetNode(new Vector2Int(i, j));
                    if (node == null || node.HasNoConnections()) continue;
                    Node right = GetNode(new Vector2Int(i + 1, j));
                    if (right == null || right.HasNoConnections()) continue;
                    Node top = GetNode(new Vector2Int(i, j + 1));
                    if (top == null || top.HasNoConnections()) continue;
                    Node topRight = GetNode(new Vector2Int(i + 1, j + 1));
                    if (topRight == null || topRight.HasNoConnections()) continue;

                    // Debug.Log("Inspecting node for connections: " + node.point);
                    if (!node.outgoing.Any(element => element.Equals(topRight.point))) continue;
                    if (!right.outgoing.Any(element => element.Equals(top.point))) continue;

                    // Debug.Log("Found a cross node: " + node.point);

                    // we managed to find a cross node:
                    // 1) add direct connections:
                    node.AddOutgoing(top.point);
                    top.AddIncoming(node.point);

                    right.AddOutgoing(topRight.point);
                    topRight.AddIncoming(right.point);

                    float rnd = Random.Range(0f, 1f);
                    if (rnd < 0.2f)
                    {
                        // remove both cross connections:
                        // a) 
                        node.RemoveOutgoing(topRight.point);
                        topRight.RemoveIncoming(node.point);
                        // b) 
                        right.RemoveOutgoing(top.point);
                        top.RemoveIncoming(right.point);
                    }
                    else if (rnd < 0.6f)
                    {
                        // a) 
                        node.RemoveOutgoing(topRight.point);
                        topRight.RemoveIncoming(node.point);
                    }
                    else
                    {
                        // b) 
                        right.RemoveOutgoing(top.point);
                        top.RemoveIncoming(right.point);
                    }
                }
        }

        private static Node GetNode(Vector2Int p)
        {
            if (p.y >= nodes.Count) return null;
            if (p.x >= nodes[p.y].Count) return null;

            return nodes[p.y][p.x];
        }

        private static Vector2Int GetFinalNode()
        {
            int y = config.layers.Count - 1;
            if (config.GridWidth % 2 == 1)
                return new Vector2Int(config.GridWidth / 2, y);

            return Random.Range(0, 2) == 0
                ? new Vector2Int(config.GridWidth / 2, y)
                : new Vector2Int(config.GridWidth / 2 - 1, y);
        }

        private static List<List<Vector2Int>> GeneratePaths()
        {
            Vector2Int finalNode = GetFinalNode();
            var paths = new List<List<Vector2Int>>();
            int numOfStartingNodes = config.numOfStartingNodes.GetValue();
            int numOfPreBossNodes = config.numOfPreBossNodes.GetValue();

            List<int> candidateXs = new List<int>();
            for (int i = 0; i < config.GridWidth; i++)
                candidateXs.Add(i);

            candidateXs.Shuffle();
            IEnumerable<int> startingXs = candidateXs.Take(numOfStartingNodes);
            List<Vector2Int> startingPoints = (from x in startingXs select new Vector2Int(x, 0)).ToList();

            candidateXs.Shuffle();
            IEnumerable<int> preBossXs = candidateXs.Take(numOfPreBossNodes);
            List<Vector2Int> preBossPoints = (from x in preBossXs select new Vector2Int(x, finalNode.y - 1)).ToList();

            int numOfPaths = Mathf.Max(numOfStartingNodes, numOfPreBossNodes) + Mathf.Max(0, config.extraPaths);
            for (int i = 0; i < numOfPaths; ++i)
            {
                Vector2Int startNode = startingPoints[i % numOfStartingNodes];
                Vector2Int endNode = preBossPoints[i % numOfPreBossNodes];
                List<Vector2Int> path = Path(startNode, endNode);
                path.Add(finalNode);
                paths.Add(path);
            }

            return paths;
        }

        // Generates a random path bottom up.
        private static List<Vector2Int> Path(Vector2Int fromPoint, Vector2Int toPoint)
        {
            int toRow = toPoint.y;
            int toCol = toPoint.x;

            int lastNodeCol = fromPoint.x;

            List<Vector2Int> path = new List<Vector2Int> { fromPoint };
            List<int> candidateCols = new List<int>();
            for (int row = 1; row < toRow; ++row)
            {
                candidateCols.Clear();

                int verticalDistance = toRow - row;
                int horizontalDistance;

                int forwardCol = lastNodeCol;
                horizontalDistance = Mathf.Abs(toCol - forwardCol);
                if (horizontalDistance <= verticalDistance)
                    candidateCols.Add(lastNodeCol);

                int leftCol = lastNodeCol - 1;
                horizontalDistance = Mathf.Abs(toCol - leftCol);
                if (leftCol >= 0 && horizontalDistance <= verticalDistance)
                    candidateCols.Add(leftCol);

                int rightCol = lastNodeCol + 1;
                horizontalDistance = Mathf.Abs(toCol - rightCol);
                if (rightCol < config.GridWidth && horizontalDistance <= verticalDistance)
                    candidateCols.Add(rightCol);

                int randomCandidateIndex = Random.Range(0, candidateCols.Count);
                int candidateCol = candidateCols[randomCandidateIndex];
                Vector2Int nextPoint = new Vector2Int(candidateCol, row);

                path.Add(nextPoint);

                lastNodeCol = candidateCol;
            }

            path.Add(toPoint);

            return path;
        }

        // GDD Node Type Probabilities Implementation
        private static NodeType GetNodeTypeByGDDProbabilities(int layerIndex)
        {
            // Convert layer index to absolute floor (assuming 10 layers = 10 floors per zone)
            // Tầng 1 (layerIndex 0) đã được xử lý riêng, nên tầng 2 (layerIndex 1) là tầng tuyệt đối 2
            int absoluteFloor;
            if (MapManager.Instance != null)
            {
                // Lấy zone hiện tại để tính tầng tuyệt đối chính xác
                absoluteFloor = (MapManager.Instance.currentZone - 1) * 10 + (layerIndex + 1);
            }
            else
            {
                // Fallback nếu test scene
                absoluteFloor = layerIndex + 1;
            }

            // Xác định vùng
            FloorRegion region;
            if (absoluteFloor <= 20) region = FloorRegion.Early;
            else if (absoluteFloor <= 60) region = FloorRegion.Mid;
            else region = FloorRegion.Late;

            float randomValue = UnityEngine.Random.Range(0f, 1f);

            switch (region)
            {
                case FloorRegion.Early: // Tầng 1-20
                    if (randomValue < 0.55f) // 55% tổng cơ hội Combat
                    {
                        // Tầng 1 (absFloor=1): 0.05 + 0.002 = 0.052 (5.2%)
                        // Tầng 20 (absFloor=20): 0.05 + 0.04 = 0.09 (9%)
                        float eliteChance = Mathf.Min(0.25f, 0.05f + 0.002f * absoluteFloor);
                        return UnityEngine.Random.Range(0f, 1f) < eliteChance ? NodeType.EliteEnemy : NodeType.MinorEnemy;
                    }
                    // Các node còn lại giữ nguyên
                    if (randomValue < 0.75f) return NodeType.Event;     // 20% (0.75 - 0.55)
                    if (randomValue < 0.87f) return NodeType.RestSite;  // 12% (0.87 - 0.75)
                    if (randomValue < 0.95f) return NodeType.Store;     // 8% (0.95 - 0.87)
                    return NodeType.Mystery;                         // 5% (1.00 - 0.95)

                case FloorRegion.Mid: // Tầng 21-60
                                      // GDD MỚI: Combat 60%, Event 18%, Rest 10%, Shop 7%, Mystery 5%
                    if (randomValue < 0.60f)
                    {
                        float eliteChance = Mathf.Min(0.25f, 0.05f + 0.002f * absoluteFloor);
                        return UnityEngine.Random.Range(0f, 1f) < eliteChance ? NodeType.EliteEnemy : NodeType.MinorEnemy;
                    }
                    if (randomValue < 0.78f) return NodeType.Event;     // 18% (0.78 - 0.60)
                    if (randomValue < 0.88f) return NodeType.RestSite;  // 10% (0.88 - 0.78)
                    if (randomValue < 0.95f) return NodeType.Store;     // 7% (0.95 - 0.88)
                    return NodeType.Mystery;                           // 5% (1.00 - 0.95)

                case FloorRegion.Late: // Tầng 61-100
                                       // GDD MỚI (giả định): Combat 65%, Event 12%, Rest 8%, Shop 5%, Mystery 10%
                    if (randomValue < 0.65f)
                    {
                        float eliteChance = Mathf.Min(0.25f, 0.05f + 0.002f * absoluteFloor);
                        return UnityEngine.Random.Range(0f, 1f) < eliteChance ? NodeType.EliteEnemy : NodeType.MinorEnemy;
                    }
                    if (randomValue < 0.77f) return NodeType.Event;     // 12% (0.77 - 0.65)
                    if (randomValue < 0.85f) return NodeType.RestSite;  // 8% (0.85 - 0.77)
                    if (randomValue < 0.90f) return NodeType.Store;     // 5% (0.90 - 0.85)
                    return NodeType.Mystery;                           // 10% (1.00 - 0.90)

                default:
                    return NodeType.MinorEnemy;
            }
        }
        // Get Boss Blueprint for specific floor from configuration
        private static NodeBlueprint GetBossBlueprintForFloor(int floorNumber)
        {
            if (config == null) return null;
            
            // First check boss floor configs (specific floor configuration)
            if (config.bossFloorConfigs != null)
            {
                var bossConfig = config.bossFloorConfigs.FirstOrDefault(bc => bc.floorNumber == floorNumber);
                if (bossConfig != null && bossConfig.bossBlueprint != null)
                {
                    return bossConfig.bossBlueprint;
                }
            }
            
            // Then check zone configs (zone-based configuration)
            if (config.zoneConfigs != null)
            {
                int zoneNumber = (floorNumber - 1) / 10 + 1; // Convert floor to zone (1-10 = Zone 1, 11-20 = Zone 2, etc.)
                var zoneConfig = config.zoneConfigs.FirstOrDefault(zc => zc.zoneNumber == zoneNumber);
                if (zoneConfig != null && zoneConfig.zoneBossBlueprint != null)
                {
                    return zoneConfig.zoneBossBlueprint;
                }
            }
            
            // If no specific config found, return null (will use random fallback)
            return null;
        }
        
        // Debug method to test node probabilities
        public static void DebugNodeProbabilities(int testRuns = 1000)
        {
            Debug.Log("=== GDD Node Type Probabilities Test ===");
            
            for (int floor = 1; floor <= 100; floor += 10)
            {
                FloorRegion region;
                if (floor <= 20) region = FloorRegion.Early;
                else if (floor <= 60) region = FloorRegion.Mid;
                else region = FloorRegion.Late;
                
                var nodeCounts = new Dictionary<NodeType, int>();
                
                // Simulate node generation for this floor
                for (int i = 0; i < testRuns; i++)
                {
                    // Temporarily set layer index for testing
                    var nodeType = GetNodeTypeByGDDProbabilities(floor - 1);
                    if (!nodeCounts.ContainsKey(nodeType))
                        nodeCounts[nodeType] = 0;
                    nodeCounts[nodeType]++;
                }
                
                Debug.Log($"Floor {floor} ({region}):");
                foreach (var kvp in nodeCounts)
                {
                    float percentage = (float)kvp.Value / testRuns * 100f;
                    Debug.Log($"  {kvp.Key}: {percentage:F1}%");
                }
            }
        }
    }
}

