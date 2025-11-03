using System.Collections.Generic;

[System.Serializable]
public class RunData
{
    public PlayerData playerData;
    public MapData mapData;
    public HashSet<string> currentRunEventFlags = new HashSet<string>();
    public List<string> availableEventPool;
    public PlayerData checkpointPlayerData;
    public RunData()
    {
        playerData = new PlayerData();
        mapData = new MapData();
        availableEventPool = new List<string>();
        currentRunEventFlags = new HashSet<string>();
        checkpointPlayerData = new PlayerData();
    }
}