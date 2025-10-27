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

            // ... (rest of the saving/player update logic remains the same) ...
            if (GameManager.Instance.playerInstance != null)
            {
                var playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
                if (playerCharacter != null)
                {
                    runData.playerData.currentHP = playerCharacter.currentHP;
                    runData.playerData.currentMana = playerCharacter.currentMana;
                }
            }
            else { /* Log warning */ }
            mapData.pendingEventID = "";
            RunSaveService.SaveRun(runData);
            Debug.Log("[SAVE SYSTEM] Event completed. Game saved.");
        }
        else if (skipSaveAndPlayerUpdate)
        {
            Debug.LogWarning("Bỏ qua việc lưu game và cập nhật player (Debug Mode hoặc lỗi GameManager). KHÔNG chuyển scene.");
            // You might want to add code here to re-enable interaction or show a "Debug Finished" message
            // For example:
            // scriptReader.dialogueBox.text = "DEBUG EVENT FINISHED. EXIT PLAY MODE.";
            return; // EXIT the function early in debug mode
        }
        else // Handle case where GameManager is missing but not explicitly skipping
        {
            Debug.LogError("Lỗi GameManager hoặc RunData bị null khi kết thúc Event! Không thể lưu. Quay về Zone1 mặc định.");
            // sceneToReturnTo remains "Zone1"
        }


        // Load the scene ONLY IF NOT in skip/debug mode
        // ADD THIS 'IF' CHECK:
        if (!skipSaveAndPlayerUpdate)
        {
            SceneManager.LoadScene(sceneToReturnTo);
        }
    }
}