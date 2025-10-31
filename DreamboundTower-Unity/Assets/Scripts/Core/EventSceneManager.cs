using System.Linq;
using UnityEngine;
using UnityEngine.UI; // Cần cho Image
using UnityEngine.SceneManagement;
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

    void Start()
    {
        EventDataSO eventToLoad = null;

        // CHECK 1: Are we in Debug Mode?
        if (debugEventToLoad != null)
        {
            Debug.LogWarning($"--- RUNNING EVENT SCENE IN DEBUG MODE --- Loading: {debugEventToLoad.eventID}");
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
                    Debug.Log($"[EVENT] Loading event from pendingEventID: {eventIDToLoad}");
                    // Find the event data in GameManager's list
                    eventToLoad = GameManager.Instance.allEvents.Find(e => e.eventID == eventIDToLoad);

                    if (eventToLoad == null)
                    {
                        Debug.LogError($"EVENT ID '{eventIDToLoad}' KHÔNG TÌM THẤY TRONG GameManager.allEvents! Quay lại Map.");
                        FinishEvent(true); // Add 'true' to skip saving
                        return;
                    }
                    Debug.Log($"[EVENT] Successfully loaded event: {eventToLoad.eventID}");
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
        }
        else
        {
            Debug.LogError("ScriptReader reference is missing in EventSceneManager!");
        }
    }
    public void FinishEvent(bool skipSaveAndPlayerUpdate = false)
    {
        Debug.Log("Event kết thúc!"); // Shorten the log for clarity

        string sceneToReturnTo = "Zone1"; // Default

        // Only try to save and update player if NOT skipping
        if (!skipSaveAndPlayerUpdate && GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            Debug.Log("Đang quay lại Map và lưu game..."); // Move log here
            var runData = GameManager.Instance.currentRunData;
            var mapData = runData.mapData;
            sceneToReturnTo = "Zone" + mapData.currentZone;

            // IMPORTANT: Save player state (inventory, equipment, HP, Mana) to RunData before saving to file
            GameManager.Instance.SavePlayerStateToRunData();
            
            // Status effects persist - they are stored in StatusEffectManager by Character reference
            // and will continue to work on the map as long as the Character instance persists
            if (GameManager.Instance.playerInstance != null)
            {
                var playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
                if (playerCharacter != null && StatusEffectManager.Instance != null)
                {
                    var activeEffects = StatusEffectManager.Instance.GetActiveEffects(playerCharacter);
                    if (activeEffects.Count > 0)
                    {
                        Debug.Log($"[EVENT] Player has {activeEffects.Count} active status effects that will persist: {string.Join(", ", activeEffects.Select(e => e.effectName))}");
                    }
                }
            }
            
            mapData.pendingEventID = "";
            RunSaveService.SaveRun(runData);
            Debug.Log("[SAVE SYSTEM] Event completed. Game saved.");
        }
        else if (skipSaveAndPlayerUpdate)
        {
            // Even in debug mode, still try to return to map if we have pending node info
            if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
            {
                var mapData = GameManager.Instance.currentRunData.mapData;
                if (!string.IsNullOrEmpty(mapData.pendingNodeSceneName))
                {
                    sceneToReturnTo = mapData.pendingNodeSceneName;
                    Debug.LogWarning($"[DEBUG MODE] Still returning to map: {sceneToReturnTo}");
                }
            }
            else
            {
                Debug.LogWarning("Bỏ qua việc lưu game và cập nhật player (Debug Mode hoặc lỗi GameManager).");
                return; // EXIT the function early only if we can't find a scene to return to
            }
        }
        else // Handle case where GameManager is missing but not explicitly skipping
        {
            Debug.LogError("Lỗi GameManager hoặc RunData bị null khi kết thúc Event! Không thể lưu. Quay về Zone1 mặc định.");
            // sceneToReturnTo remains "Zone1"
        }


        // Load the scene (always return to map, even in debug mode)
        SceneManager.LoadScene(sceneToReturnTo);
    }
}