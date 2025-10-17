using UnityEngine;

/// <summary>
/// Gear management system for testing and initializing equipment
/// 
/// USAGE:
/// - Manages references between Inventory, Equipment, and Character systems
/// - Provides test items for development and testing
/// - Handles system initialization and reference management
/// - Useful for debugging and development workflows
/// 
/// SETUP:
/// 1. Attach to a GameObject in the scene
/// 2. Assign Inventory, Equipment, and Character references
/// 3. Assign test items for development
/// 4. System automatically initializes on start
/// </summary>
public class GearManager : MonoBehaviour
{
    [Header("References")]
    public Inventory inventory;
    public Equipment equipment;
    public Character character;
    
    [Header("Test Items")]
    public GearItem testHelm;
    public GearItem testAmulet;
    public GearItem testArmor;
    public GearItem testWeapon;
    public GearItem testLegs;
    public GearItem testBoots;
    public GearItem testRing;
    
    void Start()
    {
        // Try to initialize immediately, but if it fails, try again later
        TryInitialize();
    }
    
    void TryInitialize()
    {
        // Find the scene instance components
        inventory = FindFirstObjectByType<Inventory>();
        equipment = FindFirstObjectByType<Equipment>();
        character = FindFirstObjectByType<Character>();
        
        if (inventory == null || equipment == null || character == null)
        {
            // Retry in 0.5 seconds if player not spawned yet
            Invoke(nameof(TryInitialize), 0.5f);
        }
    }
    
    // Public methods for UI to call
    public void EquipItem(GearItem item)
    {
        if (item != null)
        {
            equipment.EquipItem(item);
        }
    }
    
    public void UnequipItem(GearType gearType)
    {
        GearItem item = equipment.UnequipItem(gearType);
        if (item != null)
        {
            inventory.AddItem(item);
        }
    }
    
    public void UseItemFromInventory(int slotIndex)
    {
        GearItem item = inventory.GetItemAt(slotIndex);
        if (item != null)
        {
            // Try to equip the item
            equipment.EquipItem(item);
        }
    }
    
    // Context menu methods for testing - using GearType system
    [ContextMenu("Add Test Helm")]
    public void AddTestHelm() => AddTestItem(GearType.Helmet, testHelm);
    
    [ContextMenu("Add Test Amulet")]
    public void AddTestAmulet() => AddTestItem(GearType.Amulet, testAmulet);
    
    [ContextMenu("Add Test Armor")]
    public void AddTestArmor() => AddTestItem(GearType.ChestArmor, testArmor);
    
    [ContextMenu("Add Test Weapon")]
    public void AddTestWeapon() => AddTestItem(GearType.Weapon, testWeapon);
    
    [ContextMenu("Add Test Legs")]
    public void AddTestLegs() => AddTestItem(GearType.Pants, testLegs);
    
    [ContextMenu("Add Test Boots")]
    public void AddTestBoots() => AddTestItem(GearType.Boots, testBoots);
    
    [ContextMenu("Add Test Ring")]
    public void AddTestRing() => AddTestItem(GearType.Ring, testRing);
    
    [ContextMenu("Add All Test Items")]
    public void AddAllTestItems()
    {
        AddTestHelm();
        AddTestAmulet();
        AddTestArmor();
        AddTestWeapon();
        AddTestLegs();
        AddTestBoots();
        AddTestRing();
    }
    
    // Helper method to add test items by gear type
    private void AddTestItem(GearType gearType, GearItem testItem)
    {
        if (testItem != null && inventory != null)
        {
            inventory.AddItem(testItem);
            Debug.Log($"[GEAR MANAGER] Added test {gearType}: {testItem.itemName}");
        }
        else
        {
            Debug.LogWarning($"[GEAR MANAGER] Test {gearType} or inventory not assigned!");
        }
    }
    
    [ContextMenu("Clear Inventory")]
    public void ClearInventory()
    {
        if (inventory != null)
        {
            // Clear all items from inventory
            for (int i = 0; i < inventory.items.Count; i++)
            {
                inventory.items[i] = null;
            }
            inventory.OnInventoryChanged?.Invoke();
            Debug.Log("[GEAR MANAGER] Cleared inventory");
        }
        else
        {
            Debug.LogWarning("[GEAR MANAGER] Inventory not assigned!");
        }
    }
    
}
