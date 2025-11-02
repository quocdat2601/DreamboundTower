using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Presets;

/// <summary>
/// Singleton manager that handles loot configuration queries based on floor and enemy type
/// 
/// USAGE:
/// - Place in scene as singleton
/// - Assign all LootConfigSO assets to the configs list
/// - Automatically selects the correct config based on floor
/// - Provides methods to get drop rates and rarity distributions
/// 
/// SETUP:
/// 1. Place LootConfigManager in scene
/// 2. Assign all LootConfigSO assets to the configs list
/// 3. System automatically queries based on floor
/// </summary>
public class LootConfigManager : MonoBehaviour
{
    [Header("Loot Configs")]
    [Tooltip("All loot configuration assets, ordered by floor range")]
    public List<LootConfigSO> configs = new List<LootConfigSO>();
    
    // Singleton instance
    public static LootConfigManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        // Sort configs by minAbsoluteFloor for faster lookup
        if (configs != null && configs.Count > 0)
        {
            configs = configs.OrderBy(c => c != null ? c.minAbsoluteFloor : int.MaxValue).ToList();
        }
    }
    
    /// <summary>
    /// Get the loot config for a specific floor
    /// </summary>
    public LootConfigSO GetConfigForFloor(int absoluteFloor)
    {
        if (configs == null || configs.Count == 0)
        {
            Debug.LogWarning("[LootConfigManager] No loot configs assigned!");
            return null;
        }
        
        // Find the config that contains this floor
        // Since configs are sorted, we can use LastOrDefault to get the most specific (latest) config
        LootConfigSO matchingConfig = configs
            .Where(c => c != null && absoluteFloor >= c.minAbsoluteFloor && absoluteFloor <= c.maxAbsoluteFloor)
            .OrderByDescending(c => c.minAbsoluteFloor) // Get the most specific config (highest min floor)
            .FirstOrDefault();
        
        if (matchingConfig == null)
        {
            // Fallback: use the last config (highest floor range)
            matchingConfig = configs.Where(c => c != null).OrderByDescending(c => c.maxAbsoluteFloor).FirstOrDefault();
            
            if (matchingConfig != null)
            {
                Debug.LogWarning($"[LootConfigManager] No config found for floor {absoluteFloor}, using fallback config (floors {matchingConfig.minAbsoluteFloor}-{matchingConfig.maxAbsoluteFloor})");
            }
        }
        
        return matchingConfig;
    }
    
    /// <summary>
    /// Get the rarity config for a specific enemy kind and floor
    /// </summary>
    public LootConfigSO.LootRarityConfig GetRarityConfig(EnemyKind enemyKind, int absoluteFloor)
    {
        LootConfigSO config = GetConfigForFloor(absoluteFloor);
        if (config == null)
        {
            return null;
        }
        
        return config.GetConfigForEnemyKind(enemyKind);
    }
    
    /// <summary>
    /// Get drop count for an enemy type at a specific floor
    /// </summary>
    public int GetDropCount(EnemyKind enemyKind, int absoluteFloor)
    {
        LootConfigSO.LootRarityConfig rarityConfig = GetRarityConfig(enemyKind, absoluteFloor);
        if (rarityConfig == null)
        {
            return 1; // Fallback
        }
        
        return Random.Range(rarityConfig.minDrops, rarityConfig.maxDrops + 1);
    }
    
    /// <summary>
    /// Get base drop chance for an enemy type at a specific floor
    /// </summary>
    public float GetDropChance(EnemyKind enemyKind, int absoluteFloor)
    {
        LootConfigSO.LootRarityConfig rarityConfig = GetRarityConfig(enemyKind, absoluteFloor);
        if (rarityConfig == null)
        {
            return 0.7f; // Fallback
        }
        
        return rarityConfig.baseDropChance;
    }
    
    /// <summary>
    /// Get a random rarity for an enemy type at a specific floor
    /// </summary>
    public ItemRarity GetRandomRarity(EnemyKind enemyKind, int absoluteFloor)
    {
        LootConfigSO.LootRarityConfig rarityConfig = GetRarityConfig(enemyKind, absoluteFloor);
        if (rarityConfig == null)
        {
            return ItemRarity.Common; // Fallback
        }
        
        return rarityConfig.GetRandomRarity();
    }
}

