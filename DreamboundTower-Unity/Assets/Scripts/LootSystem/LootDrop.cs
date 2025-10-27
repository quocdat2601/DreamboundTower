using UnityEngine;

/// <summary>
/// Component that handles loot dropping when an enemy dies
/// 
/// USAGE:
/// - Attach to enemy GameObjects to make them drop loot
/// - Uses LootTable to determine what items to drop
/// - Handles drop chance calculations and loot spawning
/// - Integrates with LootManager for loot management
/// 
/// SETUP:
/// 1. Attach to enemy GameObjects
/// 2. Assign LootTable reference
/// 3. Set drop chance and multiplier values
/// 4. System automatically drops loot on death
/// </summary>
public class LootDrop : MonoBehaviour
{
    [Header("Loot Configuration")]
    [Tooltip("The loot table that defines what this enemy can drop")]
    public LootTable lootTable;
    
    [Tooltip("Base chance for this enemy to drop any loot (0-1)")]
    [Range(0f, 1f)]
    public float baseDropChance = 0.7f;
    
    [Tooltip("Multiplier for drop chances (higher = more likely to drop)")]
    [Range(0.1f, 3f)]
    public float dropChanceMultiplier = 1f;
    
    [Header("Drop Settings")]
    [Tooltip("Maximum number of items this enemy can drop")]
    public int maxDrops = 3;
    
    [Tooltip("Should drops be scattered around the enemy position?")]
    public bool scatterDrops = false;
    
    [Tooltip("Radius for scattering drops")]
    public float scatterRadius = 0.1f;
    
    private Character character;
    private LootManager lootManager;
    //private ResurrectBehavior resurrectGimmick;
    void Start()
    {
        // Get the Character component to listen for death
        character = GetComponent<Character>();
        if (character == null)
        {
            return;
        }
        
        // Find the LootManager in the scene
        lootManager = FindFirstObjectByType<LootManager>();

        // Subscribe to character death event
        character.OnDeath += OnCharacterDeath;
    }
    
    
    void OnDestroy()
    {
        // Unsubscribe from death event
        if (character != null)
        {
            character.OnDeath -= OnCharacterDeath;
        }
    }
    
    /// <summary>
    /// Called when the character dies
    /// </summary>
    /// <param name="deadCharacter">The character that died</param>
    void OnCharacterDeath(Character deadCharacter)
    {
        ResurrectBehavior resurrectGimmick = GetComponent<ResurrectBehavior>();
        // Logic kiểm tra của bạn vẫn giữ nguyên
        if (resurrectGimmick != null && !resurrectGimmick.hasResurrected)
        {
            // Đây là cái chết "giả". Không làm gì cả.
            return;
        }
        DropLoot();
    }
    
    /// <summary>
    /// Called when the enemy dies to drop loot
    /// </summary>
    public void DropLoot()
    {
        if (lootTable == null)
        {
            return;
        }
        
        
        // Calculate final drop chance
        float finalDropChance = baseDropChance * dropChanceMultiplier;
        
        // Roll for drops
        int dropsToSpawn = 0;
        for (int i = 0; i < maxDrops; i++)
        {
            if (Random.Range(0f, 1f) <= finalDropChance)
            {
                dropsToSpawn++;
            }
        }
        
        if (dropsToSpawn > 0)
        {
            // Get the enemy's UI position and convert to world coordinates
            Vector3 spawnPosition = GetEnemyWorldPosition();
            
            lootManager.SpawnLoot(lootTable, spawnPosition, dropsToSpawn, scatterDrops, scatterRadius);
        }
    }
    
    /// <summary>
    /// Get the enemy's world position by converting from UI coordinates
    /// </summary>
    /// <returns>World position where loot should spawn</returns>
    Vector3 GetEnemyWorldPosition()
    {
        // Check if this enemy is in a UI Canvas
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        
        if (parentCanvas != null)
        {
            // This is a UI element, we need to convert UI coordinates to world coordinates
            RectTransform rectTransform = GetComponent<RectTransform>();
            
            if (rectTransform != null)
            {
                // Get the enemy's position in screen space
                Vector3[] worldCorners = new Vector3[4];
                rectTransform.GetWorldCorners(worldCorners);
                
                // Get the center of the enemy UI element
                Vector3 enemyCenter = (worldCorners[0] + worldCorners[2]) * 0.5f;
                
                // Find the player to use as a reference point
                Character player = null;
                var allCharacters = FindObjectsByType<Character>(FindObjectsSortMode.None);
                foreach (var character in allCharacters)
                {
                    if (character.GetComponent<Inventory>() != null)
                    {
                        player = character;
                        break;
                    }
                }
                
                Vector3 worldPos;
                
                // Find the specific enemies (Enemy_0 and Enemy_1) like we do for the player
                var allChars = FindObjectsByType<Character>(FindObjectsSortMode.None);
                Character targetEnemy = null;
                
                // Look for Enemy_0 or Enemy_1 specifically
                foreach (var character in allChars)
                {
                    if (character.name.Contains("Enemy_0") || character.name.Contains("Enemy_1"))
                    {
                        targetEnemy = character;
                        break;
                    }
                }
                
                if (targetEnemy != null)
                {
                    // Use the enemy's actual world position (same approach as player)
                    Vector3 enemyPos = targetEnemy.transform.position;
                    worldPos = new Vector3(enemyPos.x, enemyPos.y - 30f, 0); // Reduced from 50f to 30f to make it higher
                    
                    Debug.Log($"[LootDrop] Found {targetEnemy.name} at pos: {enemyPos}, Placing loot at: {worldPos}");
                }
                else
                {
                    // Fallback: use player position + offset (same as before)
                    if (player != null)
                    {
                        Vector3 playerPos = player.transform.position;
                        worldPos = new Vector3(playerPos.x + 100f, playerPos.y - 30f, 0); // Also reduced from 50f to 30f
                        Debug.Log($"[LootDrop] No enemy found, using player pos: {playerPos}, Placing loot at: {worldPos}");
                    }
                    else
                    {
                        worldPos = new Vector3(500f, 120f, 0); // Increased from 100f to 120f
                        Debug.Log($"[LootDrop] No characters found, using fallback: {worldPos}");
                    }
                }
                
                return worldPos;
            }
        }
        
        // Fallback: if not in UI, use transform position directly
        Vector3 fallbackPos = transform.position;
        fallbackPos.y -= 30.0f;
        fallbackPos.z = 0;
        
        return fallbackPos;
    }
    
    /// <summary>
    /// Force drop loot (useful for testing or special cases)
    /// </summary>
    [ContextMenu("Force Drop Loot")]
    public void ForceDropLoot()
    {
        if (lootTable != null && lootManager != null)
        {
            Vector3 worldPos = GetEnemyWorldPosition();
            lootManager.SpawnLoot(lootTable, worldPos, maxDrops, scatterDrops, scatterRadius);
        }
    }
}
