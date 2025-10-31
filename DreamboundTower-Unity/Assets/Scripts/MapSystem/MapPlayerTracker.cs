using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
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
            if (Locked) return;
            Locked = true;
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

        private static void EnterNode(MapNode mapNode)
        {
            Debug.Log("Entering node: " + mapNode.Node.blueprintName + " of type: " + mapNode.Node.nodeType);
            if (mapNode.Node.nodeType == NodeType.Event || mapNode.Node.nodeType == NodeType.Mystery)
            {
                Instance.SelectAndLoadEvent(mapNode); // Gọi hàm xử lý Event
                return; // Thoát khỏi EnterNode, vì SelectAndLoadEvent đã xử lý xong
            }
            //
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
                case NodeType.RestSite:
                    return "RestScene";
                case NodeType.Store:
                    return "ShopScene"; 
                case NodeType.Mystery:
                    return "MysteryScene";
                case NodeType.Event:
                    return "EventScene";
                default:
                    return "";
            }
        }

        private void SelectAndLoadEvent(MapNode node)
        {
            Debug.Log($"Đang xử lý Node Event: {node.Node.point}");

            if (GameManager.Instance == null || GameManager.Instance.currentRunData == null || GameManager.Instance.allEvents == null)
            {
                Debug.LogError("Lỗi GameManager hoặc RunData khi cố gắng xử lý Node Event!");
                // Quan trọng: Phải mở khóa lại nếu có lỗi
                Locked = false;
                return;
            }

            var runData = GameManager.Instance.currentRunData;
            var playerFlags = runData.currentRunEventFlags;
            var eventPool = runData.availableEventPool;

            // 1. Lọc các event hợp lệ
            List<EventDataSO> validEvents = new List<EventDataSO>();
            EventRegion currentRegion = GetCurrentRegion(runData.mapData.currentZone);

            // Tái tạo pool nếu cạn
            if (eventPool.Count == 0 && GameManager.Instance.allEvents.Count > 0)
            {
                Debug.LogWarning("Event Pool cạn! Đang tái tạo...");
                foreach (var evt in GameManager.Instance.allEvents) { eventPool.Add(evt.eventID); }
                Debug.Log($"Đã tái tạo pool với {eventPool.Count} events từ {GameManager.Instance.allEvents.Count} total events.");
            }

            Debug.Log($"[Event Selection] Current Zone: {runData.mapData.currentZone}, Region: {currentRegion}, Pool size: {eventPool.Count}, Player flags: [{string.Join(", ", playerFlags)}]");

            // Bắt đầu lọc
            int skippedRegion = 0;
            int skippedFlag = 0;
            foreach (string eventId in eventPool)
            {
                EventDataSO eventData = GameManager.Instance.allEvents.Find(e => e.eventID == eventId);
                if (eventData != null)
                {
                    // Kiểm tra Region (cần enum EventRegion đã định nghĩa trước đó)
                    bool regionMatch = eventData.region == EventRegion.Any || eventData.region == currentRegion ||
                                       (currentRegion == EventRegion.Early && eventData.region == EventRegion.EarlyMid) ||
                                       (currentRegion == EventRegion.Mid && (eventData.region == EventRegion.EarlyMid || eventData.region == EventRegion.MidLate)) ||
                                       (currentRegion == EventRegion.Late && eventData.region == EventRegion.MidLate);

                    // Kiểm tra Prerequisite Flag
                    bool flagMatch = string.IsNullOrEmpty(eventData.prerequisiteFlag) || playerFlags.Contains(eventData.prerequisiteFlag);

                    if (!regionMatch)
                    {
                        skippedRegion++;
                    }
                    else if (!flagMatch)
                    {
                        skippedFlag++;
                    }
                    else if (regionMatch && flagMatch)
                    {
                        validEvents.Add(eventData);
                    }
                }
            }

            Debug.Log($"[Event Selection] Found {validEvents.Count} valid events. Skipped {skippedRegion} (region), {skippedFlag} (flag).");

            // FALLBACK: Nếu không tìm thấy event nào hợp lệ, thử lại với region matching nhẹ hơn (bỏ qua flag requirement)
            if (validEvents.Count == 0 && eventPool.Count > 0)
            {
                Debug.LogWarning("[Event Selection] Không tìm thấy event nào hợp lệ với flag requirement. Thử lại chỉ với region matching...");
                validEvents.Clear();
                foreach (string eventId in eventPool)
                {
                    EventDataSO eventData = GameManager.Instance.allEvents.Find(e => e.eventID == eventId);
                    if (eventData != null)
                    {
                        bool regionMatch = eventData.region == EventRegion.Any || eventData.region == currentRegion ||
                                           (currentRegion == EventRegion.Early && eventData.region == EventRegion.EarlyMid) ||
                                           (currentRegion == EventRegion.Mid && (eventData.region == EventRegion.EarlyMid || eventData.region == EventRegion.MidLate)) ||
                                           (currentRegion == EventRegion.Late && eventData.region == EventRegion.MidLate);
                        
                        if (regionMatch)
                        {
                            validEvents.Add(eventData);
                        }
                    }
                }
                Debug.Log($"[Event Selection] Fallback: Tìm thấy {validEvents.Count} events chỉ với region matching.");
            }

            // 2. Chọn ngẫu nhiên từ danh sách hợp lệ
            if (validEvents.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, validEvents.Count);
                EventDataSO selectedEvent = validEvents[randomIndex];

                Debug.Log($"Đã chọn Event: {selectedEvent.eventID}");

                // 3. Gán ID vào RunData
                runData.mapData.pendingEventID = selectedEvent.eventID;

                // 4. Xóa ID khỏi Pool
                eventPool.Remove(selectedEvent.eventID);

                // 5. Lưu trạng thái "đang chờ" và lưu game
                runData.mapData.pendingNodePoint = node.Node.point;
                // QUAN TRỌNG: Lưu tên scene hiện tại để quay về
                runData.mapData.pendingNodeSceneName = SceneManager.GetActiveScene().name;
                RunSaveService.SaveRun(runData);
                Debug.Log("[SAVE SYSTEM] Entering Event node. Pending status set. Game saved.");

                // 6. Tải EventScene
                SceneManager.LoadScene("EventScene");
            }
            else
            {
                Debug.LogWarning($"[Event Selection] Không tìm thấy Event nào hợp lệ! Zone: {runData.mapData.currentZone}, Region: {currentRegion}, Pool size: {eventPool.Count}, Total events: {GameManager.Instance.allEvents.Count}. Node này sẽ không làm gì cả. Mở khóa Player.");
                // Mở khóa để người chơi đi tiếp
                Locked = false;
            }
        }

        // (Hàm GetCurrentRegion vẫn giữ nguyên hoặc đặt gần hàm SelectAndLoadEvent)
        private EventRegion GetCurrentRegion(int currentZone)
        {
            if (currentZone <= 3) return EventRegion.Early;
            if (currentZone <= 7) return EventRegion.Mid;
            return EventRegion.Late;
            // (Nhớ điều chỉnh logic zone này cho phù hợp)
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
