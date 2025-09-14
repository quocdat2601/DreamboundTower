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
    public GearItem testSword;
    public GearItem testArmor;
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
    
}
