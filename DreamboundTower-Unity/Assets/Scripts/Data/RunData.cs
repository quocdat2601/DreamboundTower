[System.Serializable]
public class RunData
{
    public PlayerData playerData;
    public MapData mapData;
    // Chúng ta sẽ thêm các trường khác vào đây sau, ví dụ như cho Vấn đề #3

    public RunData()
    {
        playerData = new PlayerData();
        mapData = new MapData();
    }
}