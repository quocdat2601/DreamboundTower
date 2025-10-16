using UnityEngine;

[CreateAssetMenu(fileName = "New Event", menuName = "Dreambound Tower/Event Data")]
public class EventDataSO : ScriptableObject
{
    [Tooltip("ID độc nhất để nhận dạng event, ví dụ: 'event_whispering_cave'")]
    public string eventID;

    [Tooltip("Kéo file Ink JSON đã được biên dịch vào đây")]
    public TextAsset inkJSON;

    [Tooltip("Hình ảnh nền sẽ hiển thị trong scene Event")]
    public Sprite backgroundImage;

    // (Tùy chọn cho sau này)
    [Header("Điều kiện xuất hiện")]
    [Tooltip("Event này chỉ xuất hiện từ tầng này trở lên")]
    public int minAbsoluteFloor = 1;
    [Tooltip("Event này chỉ xuất hiện ở các zone được chỉ định (để trống nếu không giới hạn)")]
    public int[] requiredZones;
}