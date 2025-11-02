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
        
        // Get random loot from the table
        List<LootTable.LootEntry> lootEntries = lootTable.GetRandomLoot(maxDrops);
        
        // Always add items directly to inventory (no ground spawning)
        // Ensure we have the current scene's inventory reference
        RefreshReferences();
        
        if (playerInventory != null)
        {
            bool anyItemAdded = false;
            
            foreach (var entry in lootEntries)
            {
                if (entry.item == null) continue;
                
                // Calculate quantity
                int quantity = Random.Range(entry.minQuantity, entry.maxQuantity + 1);
                if (quantity <= 0) quantity = 1;
                
                // Add items directly to inventory (silent mode to avoid UI errors)
                // Collect all items silently first, then trigger a single UI update
                for (int i = 0; i < quantity; i++)
                {
                    try
                    {
                        // Use silent add to avoid triggering UI updates immediately
                        bool success = playerInventory.AddItemSilent(entry.item);
                        if (success)
                        {
                            anyItemAdded = true;
                            Debug.Log($"[LOOT] Collected {entry.item.itemName} x{quantity}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"[LOOT] Error adding item {entry.item.itemName} to inventory: {e.Message}");
                        // Continue with next item even if this one fails
                    }
                }
            }
            
            // Trigger UI update once after all items are added
            // Use coroutine to wait a frame for UI components to be ready
            if (anyItemAdded)
            {
                StartCoroutine(DelayedInventoryUIUpdate());
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
        
        // Add to inventory (use silent mode to avoid UI errors after shop scene)
        if (playerInventory != null)
        {
            bool anyItemAdded = false;
            for (int i = 0; i < loot.quantity; i++)
            {
                try
                {
                    // Use silent add to avoid triggering UI updates immediately
                    bool success = playerInventory.AddItemSilent(loot.item);
                    if (success)
                    {
                        anyItemAdded = true;
                        Debug.Log($"[LOOT] Collected {loot.item.itemName} x{loot.quantity}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LOOT] Error adding item {loot.item.itemName} to inventory: {e.Message}");
                    // Continue with next item even if this one fails
                }
            }
            
            // Trigger UI update once after all items are added
            if (anyItemAdded)
            {
                StartCoroutine(DelayedInventoryUIUpdate());
            }
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
    /// Delayed UI update to allow UI components to initialize after scene transitions
    /// </summary>
    private IEnumerator DelayedInventoryUIUpdate()
    {
        // Wait a frame to ensure UI components are ready
        yield return new WaitForEndOfFrame();
        
        if (playerInventory == null) yield break;
        
        // Update UI references safely (with error handling)
        try
        {
            var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
            foreach (var ui in inventoryUIs)
            {
                if (ui == null) continue;
                
                try
                {
                    if (ui.inventory != playerInventory)
                    {
                        ui.UpdateInventoryReference(playerInventory);
                    }
                    else
                    {
                        ui.ForceRefreshUI();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LOOT] Error updating inventory UI: {e.Message}");
                    // Continue with next UI
                }
            }
            
            var dragDropSystem = FindFirstObjectByType<DragDropSystem>();
            if (dragDropSystem != null)
            {
                try
                {
                    dragDropSystem.UpdateInventoryReference(playerInventory);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[LOOT] Error updating DragDropSystem: {e.Message}");
                }
            }
            
            // Trigger inventory change event after UI is ready
            try
            {
                playerInventory.OnInventoryChanged?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[LOOT] Error triggering inventory change event: {e.Message}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[LOOT] Error during UI update: {e.Message}");
        }
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
