using System;
using System.Linq;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine;

namespace Map
{
    public class MapPlayerTracker : MonoBehaviour
    {
        public bool lockAfterSelecting = false;
        public float enterNodeDelay = 1f;
        public MapManager mapManager;
        public MapView view;

        public static MapPlayerTracker Instance;

        public bool Locked { get; set; }

        private void Awake()
        {
            Instance = this;
        }

        public void SelectNode(MapNode mapNode)
        {
            if (Locked) return;

            // Debug.Log("Selected node: " + mapNode.Node.point);

            if (mapManager.CurrentMap.path.Count == 0)
            {
                // player has not selected the node yet, he can select any of the nodes with y = 0
                if (mapNode.Node.point.y == 0)
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
            else
            {
                Vector2Int currentPoint = mapManager.CurrentMap.path[mapManager.CurrentMap.path.Count - 1];
                Node currentNode = mapManager.CurrentMap.GetNode(currentPoint);

                if (currentNode != null && currentNode.outgoing.Any(point => point.Equals(mapNode.Node.point)))
                    SendPlayerToNode(mapNode);
                else
                    PlayWarningThatNodeCannotBeAccessed();
            }
        }

        private void SendPlayerToNode(MapNode mapNode)
        {
            Locked = lockAfterSelecting;
            
            // TEMPORARY: For testing map progression, treat all nodes the same
            // For non-combat nodes, mark as completed immediately
            // if (mapNode.Node.nodeType != NodeType.MinorEnemy)
            // {
                mapManager.CurrentMap.path.Add(mapNode.Node.point);
                // sync currentFloor with selected node's layer (y + 1)
                int floorInZone = mapNode.Node.point.y + 1;
                floorInZone = Mathf.Clamp(floorInZone, 1, mapManager.totalFloorsPerZone);
                mapManager.SetCurrentZoneFloor(mapManager.currentZone, floorInZone);
                mapManager.SaveMap();
                view.SetAttainableNodes();
                view.SetLineColors();
                mapNode.ShowSwirlAnimation();
                
                // Update floor display based on current position
                view.UpdateFloorDisplay();
                
                // Check if this is a boss node - advance floor after animation delay to avoid DOTween errors
                if (mapNode.Node.nodeType == NodeType.Boss)
                {
                    DOTween.Sequence()
                        .AppendInterval(enterNodeDelay)
                        .OnComplete(() =>
                        {
                            Debug.Log("Boss defeated! Advancing floor...");
                            mapManager.AdvanceFloor();
                        });
                }
            // }
            
            // Always save map before leaving scene
            mapManager.SaveMap();

            // Resolve enemy preset for next combat (normal/elite/boss)
            TryWriteCombatPreset(mapNode);

            // Delay scene handling to allow swirl animation to finish
            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() => EnterNode(mapNode));
        }

        private static void EnterNode(MapNode mapNode)
        {
            // we have access to blueprint name here as well
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);
            // load appropriate scene with context based on nodeType:
            // or show appropriate GUI over the map: 
            // if you choose to show GUI in some of these cases, do not forget to set "Locked" in MapPlayerTracker back to false
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy:
                    // TEMPORARY: Comment out scene transition for testing
                    // Save where to return and which node is pending completion
                    MapTravel.BeginNodeBattle(mapNode.Node.point, SceneManager.GetActiveScene().name, nameof(NodeType.MinorEnemy));
                    SceneManager.LoadScene("MainGame", LoadSceneMode.Single);
                    //Debug.Log("MinorEnemy node completed (scene transition disabled for testing)");
                    break;
                case NodeType.EliteEnemy:
                    //SceneManager.LoadScene("Demo", LoadSceneMode.Single);
                    break;
                case NodeType.RestSite:
                    //SceneManager.LoadScene("Demo", LoadSceneMode.Single);
                    break;
                case NodeType.Treasure:
                    //SceneManager.LoadScene("Demo", LoadSceneMode.Single);
                    break;
                case NodeType.Store:
                    //SceneManager.LoadScene("Demo", LoadSceneMode.Single);
                    break;
                case NodeType.Boss:
                    //SceneManager.LoadScene("Demo", LoadSceneMode.Single);
                    break;
                case NodeType.Mystery:
                    //SceneManager.LoadScene("Demo", LoadSceneMode.Single);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void TryWriteCombatPreset(MapNode mapNode)
        {
            if (mapNode == null || mapManager == null) return;
            Presets.EnemyTemplateSO template = null;
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy:
                    template = mapManager.normalTemplate; break;
                case NodeType.EliteEnemy:
                    template = mapManager.eliteTemplate; break;
                case NodeType.Boss:
                    template = mapManager.bossTemplate; break;
                default:
                    return;
            }
            if (template == null)
            {
                Debug.LogWarning("[MAP] Enemy template not assigned on MapManager for node type " + mapNode.Node.nodeType);
                return;
            }

            int floorInZone = mapNode.Node.point.y + 1;
            int absoluteFloor = (mapManager.currentZone - 1) * mapManager.totalFloorsPerZone + floorInZone;

            var cfg = Resources.Load<Presets.CombatConfigSO>("CombatConfig");
            if (cfg == null)
            {
                Debug.LogWarning("[MAP] Resources/CombatConfig.asset not found. Create one via Create → Presets → CombatConfig and place under Assets/Resources.");
                return;
            }
            var stats = template.GetStatsAtFloor(absoluteFloor);
            cfg.enemyKind = template.kind;
            cfg.enemyStats = stats;
            cfg.absoluteFloor = absoluteFloor;
            cfg.enemyArchetypeId = template.name;
            // Also mirror to PlayerPrefs to be scene-load safe
            MapTravel.SetPendingEnemy((int)template.kind, stats.HP, stats.STR, stats.DEF, stats.MANA, stats.INT, stats.AGI, absoluteFloor, template.name);
            Debug.Log($"[MAP] Wrote CombatConfig: floor={absoluteFloor}, kind={cfg.enemyKind}, archetype={cfg.enemyArchetypeId}, HP={stats.HP} STR={stats.STR} DEF={stats.DEF}");
        }

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
    }
}
