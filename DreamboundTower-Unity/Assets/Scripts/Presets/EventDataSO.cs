using UnityEngine;
using UnityEngine.UI;

// Enum này giúp chúng ta lọc event theo khu vực (Region)
public enum EventRegion
{
    // Dùng 0 làm giá trị "mặc định" hoặc "bất kỳ"
    Any = 0,

    // Các vùng cơ bản
    Early = 1,
    Mid = 2,
    Late = 3,

    // Các vùng gối nhau (từ CSV của bạn)
    EarlyMid = 4, // Dành cho "Early-Mid"
    MidLate = 5   // Dành cho "Mid-Late"
}

[CreateAssetMenu(fileName = "Event_New", menuName = "DreamboundTower/EventData")]
public class EventDataSO : ScriptableObject
{
    [Header("Data Linking")]
    public string eventID; // Ví dụ: "EVT_001", "EVT_002"

    [Header("Story Content")]
    public TextAsset inkStory; // File .json đã được Ink biên dịch
    public Sprite backgroundImage; // Hình nền sẽ hiển thị trong EventScene

    [Header("Filtering Logic")]
    public EventRegion region; // Event này xuất hiện ở khu vực nào?

    [Tooltip("Cờ (flag) cần có để event này xuất hiện (Để trống nếu không cần)")]
    public string prerequisiteFlag;
}