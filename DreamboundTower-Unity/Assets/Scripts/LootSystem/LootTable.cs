using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ScriptableObject that defines what items an enemy can drop and their chances
/// 
/// USAGE:
/// - Create loot table assets to define enemy drop rates
/// - Used by LootDrop component to determine what items to drop
/// - Supports multiple items with individual drop chances
/// - Can be shared between multiple enemies
/// 
/// SETUP:
/// 1. Right-click in Project > Create > RPG > Loot Table
/// 2. Configure loot entries with items and drop chances
/// 3. Assign to LootDrop components on enemies
/// </summary>
[CreateAssetMenu(fileName = "NewLootTable", menuName = "RPG/Loot Table")]
public class LootTable : ScriptableObject
{
    [System.Serializable]
    public class LootEntry
    {
        [Header("Item")]
        [Tooltip("The item that can be dropped")]
        public GearItem item;
        
        [Header("Drop Chance")]
        [Tooltip("Chance for this item to drop (0-1)")]
        [Range(0f, 1f)]
        public float dropChance = 0.1f;
        
        [Header("Quantity")]
        [Tooltip("Minimum quantity to drop")]
        public int minQuantity = 1;
        
        [Tooltip("Maximum quantity to drop")]
        public int maxQuantity = 1;
        
        [Header("Rarity")]
        [Tooltip("Rarity level of this item (affects visual appearance)")]
        public ItemRarity rarity = ItemRarity.Common;
    }
    
    [Header("Loot Entries")]
    [Tooltip("List of items this enemy can drop")]
    public List<LootEntry> lootEntries = new List<LootEntry>();
    
    [Header("Table Settings")]
    [Tooltip("Can multiple of the same item drop?")]
    public bool allowDuplicateItems = false;
    
    [Tooltip("Maximum total items that can drop from this table")]
    public int maxTotalDrops = 5;
    
    /// <summary>
    /// Get a random item from this loot table based on drop chances
    /// </summary>
    /// <returns>Random loot entry, or null if nothing drops</returns>
    public LootEntry GetRandomLoot()
    {
        if (lootEntries == null || lootEntries.Count == 0)
            return null;
        
        // Calculate total weight (sum of all drop chances)
        float totalWeight = 0f;
        foreach (var entry in lootEntries)
        {
            if (entry.item != null)
            {
                totalWeight += entry.dropChance;
            }
        }
        
        if (totalWeight <= 0f)
            return null;
        
        // Roll for loot
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;
        
        foreach (var entry in lootEntries)
        {
            if (entry.item == null) continue;
            
            currentWeight += entry.dropChance;
            if (randomValue <= currentWeight)
            {
                return entry;
            }
        }
        
        // Fallback: return the last valid entry
        for (int i = lootEntries.Count - 1; i >= 0; i--)
        {
            if (lootEntries[i].item != null)
                return lootEntries[i];
        }
        
        return null;
    }
    
    /// <summary>
    /// Get multiple random items from this loot table
    /// </summary>
    /// <param name="maxDrops">Maximum number of drops</param>
    /// <returns>List of loot entries</returns>
    public List<LootEntry> GetRandomLoot(int maxDrops)
    {
        List<LootEntry> results = new List<LootEntry>();
        List<LootEntry> availableEntries = new List<LootEntry>(lootEntries);
        
        int dropsGenerated = 0;
        int attempts = 0;
        int maxAttempts = maxDrops * 3; // Prevent infinite loops
        
        while (dropsGenerated < maxDrops && attempts < maxAttempts && availableEntries.Count > 0)
        {
            attempts++;
            
            LootEntry loot = GetRandomLoot();
            if (loot != null)
            {
                results.Add(loot);
                dropsGenerated++;
                
                // Remove from available entries if duplicates not allowed
                if (!allowDuplicateItems)
                {
                    availableEntries.Remove(loot);
                }
            }
        }
        
        return results;
    }
    
}
