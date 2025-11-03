using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MapData
{
    public int currentZone;
    public string currentMapJson; // Dữ liệu JSON của bản đồ Zone hiện tại
    public int currentFloorInZone;
    public List<Vector2Int> path; // Đường đi của người chơi trên bản đồ hiện tại

    // BIẾN MỚI: Lưu lại node đang chờ xử lý
    // Dùng Vector2Int(-1, -1) để đánh dấu là không có node nào đang chờ
    public Vector2Int pendingNodePoint;
    public string pendingNodeSceneName;
    public string lastKnownScene;
    public string pendingEnemyArchetypeId;
    public int pendingEnemyKind;
    public int pendingEnemyFloor;
    public string pendingEventID;

    public MapData()
    {
        // Luôn đảm bảo path được khởi tạo là một danh sách rỗng
        path = new List<Vector2Int>();

        // Cũng nên đặt giá trị mặc định cho các biến khác
        currentZone = 1;
        pendingNodePoint = new Vector2Int(-1, -1);
    }
}