using UnityEngine;
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
    private EventDataSO debugEventToLoad; // Kéo file .asset của bạn vào đây

    void Start()
    {
        // Tương lai: Chúng ta sẽ lấy event từ GameManager
        // Hiện tại: Chúng ta sẽ dùng event debug ở trên để test
        if (debugEventToLoad == null)
        {
            Debug.LogError("BẠN QUÊN KÉO FILE EVENT (EventDataSO) VÀO KHE 'Debug Event To Load' TRÊN INSPECTOR!");
            return;
        }

        // 1. Thiết lập "Sân khấu" (Set Background)
        if (debugEventToLoad.backgroundImage != null)
        {
            backgroundImage.sprite = debugEventToLoad.backgroundImage;
            backgroundImage.gameObject.SetActive(true);
        }
        else
        {
            // Ẩn nếu event không có hình nền
            backgroundImage.gameObject.SetActive(false); 
        }

        // 2. "Mớm kịch bản" cho diễn viên (ScriptReader)
        // Đây là dòng quan trọng nhất
        scriptReader.SetStory(debugEventToLoad.inkStory);

        // (BƯỚC SAU: Chúng ta sẽ truyền chỉ số Player vào đây)
    }

    // (Hàm này sẽ được thêm vào sau)
    // public void FinishEvent()
    // {
    //     Debug.Log("Event kết thúc! Quay lại Map.");
    //     // GameManager.Instance.ChangeState(GameState.Map);
    // }
}