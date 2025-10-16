using UnityEngine;

[System.Serializable]
public class RarityWeight
{
    public ItemRarity rarity;
    [Range(0, 100)]
    public float weight; // Dùng weight thay vì % để linh hoạt hơn
}