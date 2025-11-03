using UnityEngine;
using Presets;
using System.Linq;

/// <summary>
/// Component that handles loot dropping when an enemy dies
/// 
/// USAGE:
/// - Attach to enemy GameObjects to make them drop loot
/// - Uses LootConfigManager to determine drop rates based on floor and enemy type
/// - Automatically detects enemy type from EnemyInfo component or BattleManager
/// - Selects random items from GameManager.allItems by rarity
/// - Integrates with LootManager for loot management
/// 
/// SETUP:
/// 1. Attach to enemy GameObjects
/// 2. System automatically detects enemy type and floor
/// 3. System automatically drops loot on death
/// </summary>
public class LootDrop : MonoBehaviour
{
    [Header("Loot Configuration")]
    [Tooltip("Override enemy kind (leave as Normal to auto-detect from EnemyInfo or BattleManager)")]
    public EnemyKind overrideEnemyKind = EnemyKind.Normal;
    
    [Tooltip("If true, use overrideEnemyKind instead of auto-detecting")]
    public bool useOverrideKind = false;
    
    [Header("Drop Settings")]
    [Tooltip("Multiplier for drop chances (higher = more likely to drop). Only used if drop chance is not 100%")]
    [Range(0.1f, 3f)]
    public float dropChanceMultiplier = 1f;
    
    private Character character;
    private LootManager lootManager;
    private EnemyInfo enemyInfo;
    
    void Start()
    {
        // Get the Character component to listen for death
        character = GetComponent<Character>();
        if (character == null)
        {
            Debug.LogWarning("[LootDrop] No Character component found!");
            return;
        }
        
        // Find the LootManager in the scene
        lootManager = FindFirstObjectByType<LootManager>();
        if (lootManager == null)
        {
            Debug.LogWarning("[LootDrop] LootManager not found in scene!");
            return;
        }
        
        // Try to get EnemyInfo component
        enemyInfo = GetComponent<EnemyInfo>();
        
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
        // Check for fake death (resurrect gimmick)
        if (resurrectGimmick != null && !resurrectGimmick.hasResurrected)
        {
            // This is a fake death. Don't drop loot yet.
            return;
        }
        DropLoot();
    }
    
    /// <summary>
    /// Get the enemy kind for this enemy
    /// </summary>
    EnemyKind GetEnemyKind()
    {
        // Use override if set
        if (useOverrideKind)
        {
            return overrideEnemyKind;
        }
        
        // Try to get from EnemyInfo component
        if (enemyInfo != null)
        {
            return enemyInfo.enemyKind;
        }
        
        // EnemyInfo component should be added by BattleManager during spawn
        // If not found, fallback to name-based detection
        
        // Fallback: check by name
        string enemyName = gameObject.name;
        if (enemyName.Contains("Boss") || enemyName.Contains("BOSS"))
        {
            return EnemyKind.Boss;
        }
        else if (enemyName.Contains("Elite") || enemyName.Contains("ELITE"))
        {
            return EnemyKind.Elite;
        }
        
        // Default to Normal
        return EnemyKind.Normal;
    }
    
    /// <summary>
    /// Get the current absolute floor
    /// </summary>
    int GetAbsoluteFloor()
    {
        if (GameManager.Instance == null || GameManager.Instance.currentRunData == null)
        {
            return 1; // Fallback
        }
        
        var runData = GameManager.Instance.currentRunData;
        int zone = runData.mapData.currentZone;
        int floorInZone = runData.mapData.currentFloorInZone;
        int floorsPerZone = 10;
        
        return (zone - 1) * floorsPerZone + floorInZone;
    }
    
    /// <summary>
    /// Called when the enemy dies to drop loot
    /// </summary>
    public void DropLoot()
    {
        if (lootManager == null)
        {
            Debug.LogWarning("[LootDrop] LootManager not found!");
            return;
        }
        
        if (LootConfigManager.Instance == null)
        {
            Debug.LogWarning("[LootDrop] LootConfigManager not found! Make sure it's in the scene.");
            return;
        }
        
        // Get enemy kind and floor
        EnemyKind enemyKind = GetEnemyKind();
        int absoluteFloor = GetAbsoluteFloor();
        
        // Get rarity config for this enemy type and floor
        LootConfigSO.LootRarityConfig rarityConfig = LootConfigManager.Instance.GetRarityConfig(enemyKind, absoluteFloor);
        if (rarityConfig == null)
        {
            Debug.LogWarning($"[LootDrop] No rarity config found for {enemyKind} at floor {absoluteFloor}");
            return;
        }
        
        // Calculate number of drops
        int dropCount = Random.Range(rarityConfig.minDrops, rarityConfig.maxDrops + 1);
        
        // Roll for each drop slot
        int actualDrops = 0;
        float baseDropChance = rarityConfig.baseDropChance * dropChanceMultiplier;
        
        for (int i = 0; i < dropCount; i++)
        {
            // Boss enemies always drop (baseDropChance should be 1.0)
            // Other enemies roll based on baseDropChance
            if (enemyKind == EnemyKind.Boss || Random.Range(0f, 1f) <= baseDropChance)
            {
                // Get random rarity based on config
                ItemRarity rarity = rarityConfig.GetRandomRarity();
                
                // Spawn loot with this rarity
                Vector3 spawnPos = GetEnemyWorldPosition(); // Not used, but kept for compatibility
                lootManager.SpawnLootByRarity(rarity, spawnPos, 1);
                
                actualDrops++;
            }
        }
        
        if (actualDrops > 0)
        {
            Debug.Log($"[LootDrop] Dropped {actualDrops} items for {enemyKind} enemy at floor {absoluteFloor}");
        }
    }
    
    /// <summary>
    /// Get the enemy's world position by converting from UI coordinates
    /// </summary>
    /// <returns>World position where loot should spawn (not used in new system, but kept for compatibility)</returns>
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
                    worldPos = new Vector3(enemyPos.x, enemyPos.y - 30f, 0);
                    
                    Debug.Log($"[LootDrop] Found {targetEnemy.name} at pos: {enemyPos}, Placing loot at: {worldPos}");
                }
                else
                {
                    // Fallback: use player position + offset (same as before)
                    if (player != null)
                    {
                        Vector3 playerPos = player.transform.position;
                        worldPos = new Vector3(playerPos.x + 100f, playerPos.y - 30f, 0);
                        Debug.Log($"[LootDrop] No enemy found, using player pos: {playerPos}, Placing loot at: {worldPos}");
                    }
                    else
                    {
                        worldPos = new Vector3(500f, 120f, 0);
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
        DropLoot();
    }
}
