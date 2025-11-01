using Presets;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Cần cho Image
public class EventSceneManager : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField]
    private Image backgroundImage; // Kéo component Image của "background" vào đây
    [SerializeField]
    private ScriptReader scriptReader; // Kéo GameObject chứa "ScriptReader" vào đây

    [Header("DEBUGGING")]
    [Tooltip("TẤT CẢ TẠM THỜI: Hãy kéo 1 file EventDataSO (ví dụ: EVT_003.asset) vào đây để test")]
    [SerializeField]
    public EventDataSO debugEventToLoad; // Kéo file .asset của bạn vào đây

    [Header("Special Combat")]
    [Tooltip("Kéo file EnemyTemplateSO của RivalChild (RivalChild.asset) vào đây")]
    public EnemyTemplateSO rivalChildTemplate;
    [Tooltip("Kéo file EnemyTemplateSO của Spirit (Spirit.asset) vào đây")]
    public EnemyTemplateSO spiritTemplate;
    void Start()
    {
        EventDataSO eventToLoad = null;

        // CHECK 1: Are we in Debug Mode?
        if (debugEventToLoad != null)
        {
            Debug.LogWarning("--- RUNNING EVENT SCENE IN DEBUG MODE ---");
            eventToLoad = debugEventToLoad;
        }
        else // CHECK 2: Try getting the event from GameManager (Normal Mode)
        {
            if (GameManager.Instance != null && GameManager.Instance.currentRunData != null && GameManager.Instance.currentRunData.mapData != null)
            {
                string eventIDToLoad = GameManager.Instance.currentRunData.mapData.pendingEventID;
                GameManager.Instance.currentRunData.mapData.pendingEventID = ""; // Clear immediately

                if (!string.IsNullOrEmpty(eventIDToLoad))
                {
                    // Find the event data in GameManager's list
                    eventToLoad = GameManager.Instance.allEvents.Find(e => e.eventID == eventIDToLoad);

                    if (eventToLoad == null)
                    {
                        Debug.LogError($"EVENT ID '{eventIDToLoad}' KHÔNG TÌM THẤY TRONG GameManager.allEvents! Quay lại Map.");
                        FinishEvent(true); // Add 'true' to skip saving
                        return;
                    }
                }
            }
        }

        // CHECK 3: Did we successfully find an event to load (either debug or normal)?
        if (eventToLoad == null)
        {
            Debug.LogError("KHÔNG CÓ EVENT ID NÀO HỢP LỆ ĐỂ TẢI! (Debug slot trống VÀ GameManager không có pendingID). Quay lại Map.");
            FinishEvent(true); // Add 'true' to skip saving
            return;
        }

        // --- Setup Scene using eventToLoad ---

        // Set Background
        if (backgroundImage != null) // Add null check for safety
        {
            if (eventToLoad.backgroundImage != null)
            {
                backgroundImage.sprite = eventToLoad.backgroundImage;
                backgroundImage.gameObject.SetActive(true);
            }
            else
            {
                backgroundImage.gameObject.SetActive(false);
            }
        }

        // Set Story in ScriptReader
        if (scriptReader != null) // Add null check
        {
            scriptReader.SetStory(eventToLoad.inkStory);
            scriptReader.SetEventManagerReference(this);
        }
        else
        {
            Debug.LogError("ScriptReader reference is missing in EventSceneManager!");
        }
    }
    // Trong EventSceneManager.cs

    public void FinishEvent(bool skipSaveAndPlayerUpdate = false)
    {
        Debug.Log("Event kết thúc!");

        // --- XỬ LÝ KHI KẾT THÚC BÌNH THƯỜNG (KHÔNG SKIP) ---
        if (!skipSaveAndPlayerUpdate && GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            var runData = GameManager.Instance.currentRunData;
            var mapData = runData.mapData;

            // 1. LẤY ĐÚNG TÊN SCENE MAP ĐỂ QUAY VỀ (TỪ Pending State)
            string sceneToReturnTo = mapData.pendingNodeSceneName;
            Vector2Int completedNodePoint = mapData.pendingNodePoint; // Lưu lại điểm node đã xong

            // Kiểm tra an toàn nếu tên scene bị rỗng (dù không nên xảy ra)
            if (string.IsNullOrEmpty(sceneToReturnTo))
            {
                Debug.LogError("FinishEvent: pendingNodeSceneName bị rỗng! Fallback về Zone hiện tại.");
                sceneToReturnTo = $"Zone{mapData.currentZone}"; // Dùng Zone hiện tại làm dự phòng
                                                                // Vẫn nên xóa pending state lỗi
                mapData.pendingNodePoint = new Vector2Int(-1, -1);
                mapData.pendingNodeSceneName = null;
            }
            else
            {
                Debug.Log($"Đang chuẩn bị quay về Map scene: {sceneToReturnTo}");
                // 2. XÓA TRẠNG THÁI PENDING (Rất quan trọng)
                mapData.pendingNodePoint = new Vector2Int(-1, -1);
                mapData.pendingNodeSceneName = null;
            }

            // 3. CẬP NHẬT PATH (Đánh dấu node đã hoàn thành)
            // Chỉ thêm nếu điểm hợp lệ và chưa có trong path
            if (completedNodePoint.x != -1 && !mapData.path.Contains(completedNodePoint))
            {
                mapData.path.Add(completedNodePoint);
                Debug.Log($"Đã thêm node {completedNodePoint} vào path.");
            }


            // 4. CẬP NHẬT TRẠNG THÁI PLAYER (HP/Mana - giữ nguyên)
            if (GameManager.Instance.playerInstance != null)
            {
                var playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
                if (playerCharacter != null)
                {
                    runData.playerData.currentHP = playerCharacter.currentHP;
                    runData.playerData.currentMana = playerCharacter.currentMana;
                }
            }

            // 5. XÓA PENDING EVENT ID (Giữ nguyên)
            mapData.pendingEventID = ""; // Đảm bảo ID event không còn treo

            // 6. LƯU GAME
            RunSaveService.SaveRun(runData);
            Debug.Log("[SAVE SYSTEM] Event completed. Game saved.");

            // 7. TẢI SCENE MAP
            SceneManager.LoadScene(sceneToReturnTo);
            // --- KẾT THÚC XỬ LÝ BÌNH THƯỜNG ---
        }
        // --- XỬ LÝ KHI SKIP HOẶC LỖI GAMEMANAGER ---
        else if (skipSaveAndPlayerUpdate)
        {
            Debug.LogWarning("Bỏ qua việc lưu và quay về Map (Debug Mode hoặc lỗi Event ID).");
            // Không làm gì thêm, để Scene Event đứng yên cho debug
            // scriptReader?.ShowMessage("DEBUG EVENT FINISHED."); // Ví dụ hiển thị thông báo
        }
        else // Trường hợp GameManager bị null khi không skip
        {
            Debug.LogError("Lỗi GameManager hoặc RunData bị null khi kết thúc Event! Không thể lưu. KHÔNG chuyển scene.");
            // Ở lại Scene Event để tránh lỗi nặng hơn
        }
    }
}