using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ShopTier_", menuName = "Dreambound Tower/Shop Tier Config")]
public class ShopTierConfigSO : ScriptableObject
{
    public string tierName; // Ví dụ: "Early Game (Floors 1-20)"
    public int minAbsoluteFloor;
    public int maxAbsoluteFloor;

    [Header("Rarity Weights")]
    public List<RarityWeight> rarityWeights;
}