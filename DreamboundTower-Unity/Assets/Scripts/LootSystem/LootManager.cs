using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Manages loot spawning and collection in the game
/// 
/// USAGE:
/// - Spawns loot items when enemies die
/// - Manages loot pickup and collection
/// - Handles loot despawning and auto-collection
/// - Integrates with inventory system
/// 
/// SETUP:
/// 1. Place in scene as singleton
/// 2. Assign loot pickup prefab
/// 3. Set loot despawn and auto-collect settings
/// 4. System automatically manages loot lifecycle
/// </summary>
public class LootManager : MonoBehaviour
{
    [Header("Loot Prefabs")]
    [Tooltip("Prefab for dropped items (should have LootPickup component)")]
    public GameObject lootPickupPrefab;
    
    [Header("Loot Settings")]
    [Tooltip("How long dropped items stay in the world before despawning")]
    public float lootDespawnTime = 60f;
    
    [Tooltip("Auto-collect all loot after this many seconds (0 = disabled)")]
    public float globalAutoCollectDelay = 0.1f; // Set to 0.1s for immediate collection
    
    
    [Header("References")]
    [Tooltip("Player transform for auto-collection")]
    public Transform playerTransform;
    
    [Tooltip("Inventory to add collected items to")]
    public Inventory playerInventory;
    
    // Singleton instance
    public static LootManager Instance { get; private set; }
    
    // Active loot in the scene
    public List<LootPickup> activeLoot = new List<LootPickup>();
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // Start coroutine to keep trying until we find the player
        StartCoroutine(FindPlayerCoroutine());
    }
    
    void OnEnable()
    {
        // Don't refresh references here - let the coroutine handle it
        // This prevents the warning messages at startup
    }
    
    void RefreshReferences()
    {
        // Try to find player first (inventory is attached to player)
        // Look for a Character that has an Inventory component (player) vs enemies
        var allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        Character player = null;
        
        foreach (var character in allCharacters)
        {
            if (character.GetComponent<Inventory>() != null)
            {
                player = character;
                break; // Found the player (has inventory)
            }
        }
        
        if (player != null)
        {
            playerTransform = player.transform;
            
            // Find inventory attached to the player
            var foundInventory = player.GetComponent<Inventory>();
            if (foundInventory != null)
            {
                playerInventory = foundInventory;
            }
        }
        
    }
    
    void Update()
    {
        // Clean up null references
        activeLoot.RemoveAll(loot => loot == null);
    }
    
    /// <summary>
    /// Spawn loot from a loot table at a specific position
    /// </summary>
    /// <param name="lootTable">The loot table to use</param>
    /// <param name="position">World position to spawn at</param>
    /// <param name="maxDrops">Maximum number of items to drop</param>
    /// <param name="scatter">Should items be scattered around the position?</param>
    /// <param name="scatterRadius">Radius for scattering</param>
    public void SpawnLoot(LootTable lootTable, Vector3 position, int maxDrops = 1, bool scatter = false, float scatterRadius = 0.5f)
    {
        if (lootTable == null)
        {
            return;
        }
        
        if (lootPickupPrefab == null)
        {
            return;
        }
        
        // Get random loot from the table
        List<LootTable.LootEntry> lootEntries = lootTable.GetRandomLoot(maxDrops);
        
        foreach (var entry in lootEntries)
        {
            if (entry.item == null) continue;
            
            // Use the position passed from LootDrop (which already handles BattleArea positioning)
            Vector3 spawnPos = position;
            
            // Add scatter if enabled
            if (scatter)
            {
                Vector2 randomOffset = Random.insideUnitCircle * scatterRadius;
                spawnPos += new Vector3(randomOffset.x, 0, randomOffset.y);
            }
            
            // Spawn the loot pickup
            GameObject lootObj = Instantiate(lootPickupPrefab, spawnPos, Quaternion.identity);
            LootPickup pickup = lootObj.GetComponent<LootPickup>();
            
            if (pickup != null)
            {
                // Configure the pickup with real item data
                pickup.SetupLoot(entry.item, entry.minQuantity, entry.maxQuantity, entry.rarity);
                pickup.SetDespawnTime(lootDespawnTime);
                
                // Set auto-collect delay if enabled globally
                if (globalAutoCollectDelay > 0)
                {
                    pickup.autoCollectDelay = globalAutoCollectDelay;
                }
                
                
                // Add to active loot list
                activeLoot.Add(pickup);
                
            }
            else
            {
                Destroy(lootObj);
            }
        }
    }
    
    
    /// <summary>
    /// Collect a specific loot pickup
    /// </summary>
    /// <param name="loot">The loot pickup to collect</param>
    public void CollectLoot(LootPickup loot)
    {
        if (loot == null) return;
        
        // Always ensure we have the current scene's inventory reference
        RefreshReferences();
        
        // Add to inventory
        if (playerInventory != null)
        {
            for (int i = 0; i < loot.quantity; i++)
            {
                bool success = playerInventory.AddItem(loot.item);
                if (success)
                {
                    Debug.Log($"[LOOT] Collected {loot.item.itemName} x{loot.quantity}");
                }
            }
            
            // Force UI update by finding all inventory UI components
            var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
            foreach (var ui in inventoryUIs)
            {
                // Make sure the UI is using the same inventory instance
                if (ui.inventory != playerInventory)
                {
                    ui.UpdateInventoryReference(playerInventory);
                }
                else
                {
                    ui.ForceRefreshUI();
                }
            }
            
            // Also update DragDropSystem inventory reference
            var dragDropSystem = FindFirstObjectByType<DragDropSystem>();
            if (dragDropSystem != null)
            {
                dragDropSystem.UpdateInventoryReference(playerInventory);
            }
            
            // The inventory's OnInventoryChanged event should handle SimpleInventoryUI updates
            // No need to manually call UpdateInventoryUI() since it's private
        }
        
        // Remove from active loot and destroy (with small delay to ensure UI updates)
        activeLoot.Remove(loot);
        StartCoroutine(DestroyLootAfterFrame(loot.gameObject));
    }
    
    /// <summary>
    /// Destroy loot GameObject after one frame to ensure UI updates properly
    /// </summary>
    System.Collections.IEnumerator DestroyLootAfterFrame(GameObject lootObject)
    {
        yield return null; // Wait one frame
        if (lootObject != null)
        {
            Destroy(lootObject);
        }
    }
    
    /// <summary>
    /// Coroutine to keep trying to find the player until successful
    /// </summary>
    System.Collections.IEnumerator FindPlayerCoroutine()
    {
        while (playerInventory == null)
        {
            RefreshReferences();
            
            if (playerInventory != null)
            {
                // Clear any existing items when we find a new inventory
                ClearInventory();
                break;
            }
            
            // Wait 0.5 seconds before trying again
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    /// <summary>
    /// Reset references when scene changes
    /// </summary>
    void OnDisable()
    {
        // Reset references when this object is disabled (scene change)
        playerInventory = null;
        playerTransform = null;
    }
    
    /// <summary>
    /// Clear the inventory when a new scene loads
    /// </summary>
    void ClearInventory()
    {
        if (playerInventory != null)
        {
            // Clear all items from the inventory
            for (int i = 0; i < playerInventory.items.Count; i++)
            {
                playerInventory.items[i] = null;
            }
            
            // Trigger UI update to reflect the cleared inventory
            playerInventory.OnInventoryChanged?.Invoke();
        }
    }
    
    
    
    /// <summary>
    /// Clear all active loot (useful for scene transitions)
    /// </summary>
    public void ClearAllLoot()
    {
        foreach (var loot in activeLoot)
        {
            if (loot != null)
                Destroy(loot.gameObject);
        }
        activeLoot.Clear();
    }
    
    /// <summary>
    /// Get the number of active loot items
    /// </summary>
    public int GetActiveLootCount()
    {
        return activeLoot.Count;
    }
    
    /// <summary>
    /// Wait for all loot to be collected before proceeding
    /// This ensures all loot is picked up (manually or via auto-collect) before battle ends
    /// </summary>
    /// <returns>Coroutine that waits until all loot is collected</returns>
    public IEnumerator WaitForAllLootCollected()
    {
        // Clean up null references first
        activeLoot.RemoveAll(loot => loot == null);
        
        // If no active loot, we're done
        if (activeLoot.Count == 0)
        {
            yield break;
        }
        
        // Find the maximum remaining auto-collect time among active loot
        float maxRemainingTime = 0f;
        foreach (var loot in activeLoot)
        {
            if (loot != null)
            {
                float remaining = loot.RemainingAutoCollectTime();
                if (remaining > maxRemainingTime)
                {
                    maxRemainingTime = remaining;
                }
            }
        }
        
        // If there's auto-collect enabled, wait until the longest remaining delay has passed
        if (maxRemainingTime > 0)
        {
            Debug.Log($"[LOOT] Waiting for auto-collect delays (max remaining: {maxRemainingTime}s)");
            yield return new WaitForSeconds(maxRemainingTime + 0.5f); // Add small buffer
        }
        
        // Now wait until all loot is actually collected (might have been manually collected or auto-collected)
        float waitStartTime = Time.time;
        float maxWaitTime = 30f; // Safety timeout - don't wait forever
        
        while (activeLoot.Count > 0 && (Time.time - waitStartTime) < maxWaitTime)
        {
            // Clean up null references each frame
            activeLoot.RemoveAll(loot => loot == null);
            
            if (activeLoot.Count == 0)
            {
                break;
            }
            
            yield return null; // Wait one frame
        }
        
        if (activeLoot.Count > 0)
        {
            Debug.LogWarning($"[LOOT] Some loot was not collected before battle end. Remaining: {activeLoot.Count}");
        }
        else
        {
            Debug.Log("[LOOT] All loot collected!");
        }
    }
    
    
}
