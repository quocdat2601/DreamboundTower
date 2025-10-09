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
            view.SetAttainableNodes();
            view.SetLineColors();
            mapNode.ShowSwirlAnimation();
            view.UpdateFloorDisplay();

            // --- LOGIC MỚI ĐƯỢC GOM LẠI ---
            // 1. Xác định scene sẽ tải dựa trên loại node
            string sceneToLoad = GetSceneNameForNodeType(mapNode.Node.nodeType);

            // 2. Nếu là node tự động hoàn thành (không cần chuyển scene)
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                HandleAutoCompletion(mapNode);
                return; // Kết thúc hàm ở đây
            }

            // 3. Nếu là node cần chuyển scene (combat, event...) -> ĐÂY LÀ ĐIỂM KHÔNG THỂ QUAY ĐẦU
            if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
            {
                var runData = GameManager.Instance.currentRunData;
                // Gán CẢ HAI thông tin pending cùng lúc
                runData.mapData.pendingNodePoint = mapNode.Node.point;
                runData.mapData.pendingNodeSceneName = sceneToLoad;

                // LƯU GAME 1 LẦN DUY NHẤT
                RunSaveService.SaveRun(runData);
                Debug.Log($"[SAVE SYSTEM] Point of no return: Committed to node {mapNode.Node.point} (Scene: {sceneToLoad}). Game saved.");
            }
            else
            {
                Debug.LogError("GameManager hoặc RunData không tồn tại! Không thể lưu trạng thái pending.");
                Locked = false; // Mở khóa để tránh bị kẹt
                return;
            }

            // --- CÁC PHẦN CŨ VẪN GIỮ NGUYÊN ---
            TryWriteCombatPreset(mapNode);

            // Dùng DOTween để chuyển scene sau độ trễ
            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() =>
            {
                SceneManager.LoadScene(sceneToLoad);
            });
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
                case NodeType.Treasure:
                case NodeType.Store:
                case NodeType.Mystery: // Tạm thời để auto complete
                default:
                    return "";
            }
        }

        private void HandleAutoCompletion(MapNode mapNode)
        {
            Debug.Log($"Auto-completing node: {mapNode.Node.nodeType} at {mapNode.Node.point}");

            // ----- PHẦN LOGIC MỚI - LƯU TRƯỚC KHI ANIMATION -----
            // Mục tiêu: Cập nhật và lưu trạng thái "pending" ngay lập tức để vá lỗi "Quit & Skip"
            if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
            {
                var runData = GameManager.Instance.currentRunData;

                // Ghi lại trạng thái pending
                runData.mapData.pendingNodePoint = mapNode.Node.point;
                // Vì đây là node auto-complete, không có scene name
                runData.mapData.pendingNodeSceneName = "";

                // LƯU GAME 1 LẦN DUY NHẤT TẠI ĐÂY
                RunSaveService.SaveRun(runData);
                Debug.Log($"[SAVE SYSTEM] Point of no return: Committed to auto-complete node {mapNode.Node.point}. Game saved.");
            }
            else
            {
                Debug.LogError("GameManager hoặc RunData không tồn tại! Không thể lưu trạng thái pending.");
                Locked = false;
                return;
            }

            // Dùng DOTween để tạo độ trễ cho hiệu ứng
            DOTween.Sequence().AppendInterval(enterNodeDelay).OnComplete(() =>
            {
                if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
                {
                    var runData = GameManager.Instance.currentRunData;
                    var completedNodePoint = runData.mapData.pendingNodePoint;

                    // ----- PHẦN SỬA LỖI -----
                    // 1. Cập nhật đường đi vào CẢ HAI nơi: RunData và MapManager.CurrentMap
                    if (runData.mapData.path.All(p => p != completedNodePoint))
                    {
                        runData.mapData.path.Add(completedNodePoint);
                        // ĐỒNG BỘ VỚI ĐỐI TƯỢNG ĐANG CHẠY
                        mapManager.CurrentMap.path.Add(completedNodePoint);
                    }

                    // 2. Xóa trạng thái "đang chờ" trong RunData
                    runData.mapData.pendingNodePoint = new Vector2Int(-1, -1);
                    runData.mapData.pendingNodeSceneName = null;

                    // 3. Lưu lại trạng thái đã hoàn thành
                    RunSaveService.SaveRun(runData);
                    Debug.Log($"[SAVE SYSTEM] Node {completedNodePoint} auto-completed. Game saved.");
                }

                // 4. Cập nhật lại giao diện bản đồ (bây giờ sẽ không bị lỗi nữa)
                view.SetAttainableNodes();
                view.SetLineColors();

                // 5. Mở khóa để người chơi có thể chọn node tiếp theo
                Locked = false;
            });
        }
        private void TryWriteCombatPreset(MapNode mapNode)
        {
            if (mapNode == null || mapManager == null) return;
            if (GameManager.Instance == null || GameManager.Instance.currentRunData == null) return;

            Presets.EnemyTemplateSO template = null;
            switch (mapNode.Node.nodeType)
            {
                case NodeType.MinorEnemy: template = mapManager.normalTemplate; break;
                case NodeType.EliteEnemy: template = mapManager.eliteTemplate; break;
                case NodeType.Boss: template = mapManager.bossTemplate; break;
                default: return;
            }

            if (template == null)
            {
                Debug.LogWarning($"[MAP] Enemy template not assigned for node type {mapNode.Node.nodeType}");
                return;
            }

            int floorInZone = mapNode.Node.point.y + 1;
            int absoluteFloor = (mapManager.currentZone - 1) * mapManager.totalFloorsPerZone + floorInZone;

            // Lấy RunData ra
            var runData = GameManager.Instance.currentRunData;

            // Ghi trực tiếp thông tin combat vào RunData
            runData.mapData.pendingEnemyArchetypeId = template.name;
            runData.mapData.pendingEnemyKind = (int)template.kind;
            runData.mapData.pendingEnemyFloor = absoluteFloor;

            Debug.Log($"[MAP] Wrote pending combat data to RunData: kind={template.kind}, archetype={template.name}, floor={absoluteFloor}");
        }

        private void PlayWarningThatNodeCannotBeAccessed()
        {
            Debug.Log("Selected node cannot be accessed");
        }
    }
}
