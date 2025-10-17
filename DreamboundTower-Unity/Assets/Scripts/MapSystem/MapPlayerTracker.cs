using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
            //Locked = lockAfterSelecting;
            if (Locked) return;
            Locked = true;

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

            // Always save map before leaving scene
            //mapManager.SaveMap();

            // Resolve enemy preset for next combat (normal/elite/boss)
            TryWriteCombatPreset(mapNode);

            // Delay scene handling to allow swirl animation to finish
            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() => EnterNode(mapNode));
        }

        // Trong file MapPlayerTracker.cs

        private static void EnterNode(MapNode mapNode)
        {
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);

            // Lấy scene sẽ được tải dựa trên loại node
            string sceneToLoad = Instance.GetSceneNameForNodeType(mapNode.Node.nodeType);

            // Nếu không có scene nào cần tải (ví dụ: node Rest, Shop hiển thị UI tại chỗ)
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.Log("Node type does not require scene change. Unlocking player.");
                // TODO: Mở UI của shop/rest site ở đây
                Instance.Locked = false; // Mở khóa để người chơi có thể tương tác tiếp
                return;
            }

            // --- LOGIC MỚI ĐỂ CHUYỂN SCENE ---

            // 1. Lấy RunData ra
            var runData = GameManager.Instance.currentRunData;

            // 2. Ghi lại thông tin "đang chờ xử lý" vào RunData
            runData.mapData.pendingNodePoint = mapNode.Node.point;
            runData.mapData.pendingNodeSceneName = SceneManager.GetActiveScene().name; // Scene để quay về

            // 3. LƯU LẠI TOÀN BỘ RUNDATA NGAY TRƯỚC KHI RỜI ĐI
            // Đây là bước cực kỳ quan trọng để đảm bảo dữ liệu là mới nhất
            RunSaveService.SaveRun(runData);
            Debug.Log("[MapPlayerTracker] Saved pending node state to RunData before loading combat scene.");

            // 4. Tải scene mới
            SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
        }

        private string GetSceneNameForNodeType(NodeType type)
        {
            switch (type)
            {
                case NodeType.MinorEnemy:
                case NodeType.EliteEnemy:
                case NodeType.Boss:
                    return "MainGame";
                // case NodeType.Mystery:
                //     return "EventScene";

                // Các node không cần chuyển scene sẽ trả về chuỗi rỗng
                case NodeType.RestSite:
                    return "RestScene";
                case NodeType.Treasure:
                case NodeType.Store:
                    return "ShopScene";
                case NodeType.Mystery: // Tạm thời để auto complete
                default:
                    return "";
            }
        }

        private void TryWriteCombatPreset(MapNode mapNode)
        {
            if (mapNode == null || mapManager == null || GameManager.Instance == null) return;

            // 1. Xác định "Cấp bậc" (Kind) cần tìm dựa trên loại node
            Presets.EnemyKind requiredKind;
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy:
                    requiredKind = Presets.EnemyKind.Normal;
                    break;
                case NodeType.EliteEnemy:
                    requiredKind = Presets.EnemyKind.Elite;
                    break;
                case NodeType.Boss:
                    requiredKind = Presets.EnemyKind.Boss;
                    break;
                default:
                    return; // Không phải node combat, không làm gì cả
            }

            // 2. Lọc "Kho" quái vật trong GameManager để tìm các "Binh chủng" phù hợp
            int absoluteFloor = mapManager.GetAbsoluteFloorFromNodePosition(mapNode.Node.point);
            var runData = GameManager.Instance.currentRunData;
            runData.mapData.pendingEnemyArchetypeId = "";
            runData.mapData.pendingEnemyKind = (int)requiredKind;
            runData.mapData.pendingEnemyFloor = absoluteFloor;

            Debug.Log($"[MAP] Wrote pending combat encounter: kind={requiredKind}, floor={absoluteFloor}");
        }

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
    }
}
