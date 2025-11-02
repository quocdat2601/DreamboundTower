using System.Collections.Generic;
using UnityEngine;
using Presets;

/// <summary>
/// ScriptableObject that defines loot drop rates for different enemy types across floor ranges
/// 
/// USAGE:
/// - Create loot config assets to define enemy drop rates by floor range
/// - Used by LootConfigManager to determine drop rates
/// - Supports different drop rates for Normal, Elite, and Boss enemies
/// - Can have multiple configs for different floor ranges
/// 
/// SETUP:
/// 1. Right-click in Project > Create > Dreambound Tower > Loot Config
/// 2. Configure floor range (minAbsoluteFloor, maxAbsoluteFloor)
/// 3. Configure drop rates for Normal, Elite, and Boss enemies
/// 4. Add to LootConfigManager's config list
/// </summary>
[CreateAssetMenu(fileName = "LootConfig_", menuName = "Dreambound Tower/Loot Config")]
public class LootConfigSO : ScriptableObject
{
    [Header("Floor Range")]
    [Tooltip("Minimum absolute floor for this config (inclusive)")]
    public int minAbsoluteFloor = 1;
    
    [Tooltip("Maximum absolute floor for this config (inclusive)")]
    public int maxAbsoluteFloor = 10;
    
    [Header("Normal Enemy Drop Rates")]
    public LootRarityConfig normalEnemyConfig;
    
    [Header("Elite Enemy Drop Rates")]
    public LootRarityConfig eliteEnemyConfig;
    
    [Header("Boss Enemy Drop Rates")]
    public LootRarityConfig bossEnemyConfig;
    
    [System.Serializable]
    public class LootRarityConfig
    {
        [Header("Rarity Weights")]
        [Tooltip("Weight for Common rarity (0-100)")]
        [Range(0, 100)]
        public float commonWeight = 70f;
        
        [Tooltip("Weight for Uncommon rarity (0-100)")]
        [Range(0, 100)]
        public float uncommonWeight = 25f;
        
        [Tooltip("Weight for Rare rarity (0-100)")]
        [Range(0, 100)]
        public float rareWeight = 5f;
        
        [Tooltip("Weight for Epic rarity (0-100)")]
        [Range(0, 100)]
        public float epicWeight = 0f;
        
        [Tooltip("Weight for Legendary rarity (0-100)")]
        [Range(0, 100)]
        public float legendaryWeight = 0f;
        
        [Header("Drop Settings")]
        [Tooltip("Minimum number of items this enemy can drop")]
        public int minDrops = 1;
        
        [Tooltip("Maximum number of items this enemy can drop")]
        public int maxDrops = 2;
        
        [Tooltip("Base chance for each drop slot (0-1). For Boss, should be 1.0 (guaranteed)")]
        [Range(0f, 1f)]
        public float baseDropChance = 0.7f;
        
        /// <summary>
        /// Get a random rarity based on the configured weights
        /// </summary>
        public ItemRarity GetRandomRarity()
        {
            float totalWeight = commonWeight + uncommonWeight + rareWeight + epicWeight + legendaryWeight;
            
            if (totalWeight <= 0f)
            {
                return ItemRarity.Common; // Fallback
            }
            
            float randomValue = Random.Range(0f, totalWeight);
            float currentWeight = 0f;
            
            currentWeight += commonWeight;
            if (randomValue <= currentWeight) return ItemRarity.Common;
            
            currentWeight += uncommonWeight;
            if (randomValue <= currentWeight) return ItemRarity.Uncommon;
            
            currentWeight += rareWeight;
            if (randomValue <= currentWeight) return ItemRarity.Rare;
            
            currentWeight += epicWeight;
            if (randomValue <= currentWeight) return ItemRarity.Epic;
            
            return ItemRarity.Legendary;
        }
    }
    
    /// <summary>
    /// Get the loot config for a specific enemy kind
    /// </summary>
    public LootRarityConfig GetConfigForEnemyKind(EnemyKind kind)
    {
        switch (kind)
        {
            case EnemyKind.Normal:
                return normalEnemyConfig;
            case EnemyKind.Elite:
                return eliteEnemyConfig;
            case EnemyKind.Boss:
                return bossEnemyConfig;
            default:
                return normalEnemyConfig;
        }
    }
}

