using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "TreasureTier_", menuName = "Dreambound Tower/Treasure Tier Config")]
public class TreasureTierConfigSO : ScriptableObject
{
    [Header("Floor Range")]
    [Tooltip("Tầng thấp nhất mà rương này có thể xuất hiện")]
    public int minAbsoluteFloor;
    [Tooltip("Tầng cao nhất mà rương này có thể xuất hiện")]
    public int maxAbsoluteFloor;

    [Header("Gold Reward")]
    [Tooltip("Lượng vàng TỐI THIỂU nhận được")]
    public int minGold;
    [Tooltip("Lượng vàng TỐI ĐA nhận được")]
    public int maxGold;

    [Header("Item Reward")]
    [Tooltip("Số lượng item sẽ rơi ra (ví dụ: 1)")]
    public int numberOfItems = 1;

    [Tooltip("Tỷ lệ (trọng số) rơi ra các item theo độ hiếm.")]
    public List<RarityWeight> rarityWeights;
}