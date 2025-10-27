using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Equipment system for managing equipped items and stat bonuses
/// 
/// USAGE:
/// - Manages 8 equipment slots for different gear types
/// - Automatically applies/removes stat bonuses when items are equipped/unequipped
/// - Handles item swapping and validation
/// - Provides events for UI updates
/// 
/// SETUP:
/// 1. Attach to player GameObject
/// 2. Assign Character reference for stat management
/// 3. System automatically finds Inventory component
/// </summary>
public class Equipment : MonoBehaviour
{
    [Header("Equipment Slots (8 slots total)")]
    public GearItem[] equipmentSlots = new GearItem[8];
    
    [Header("References")]
    public Inventory inventory;
    public Character character;
    
    // Events for UI updates
    public System.Action<GearItem, GearType> OnItemEquipped;
    public System.Action<GearItem, GearType> OnItemUnequipped;
    public System.Action OnEquipmentChanged;
    
    void Start()
    {
        if (inventory == null)
            inventory = GetComponent<Inventory>();
        if (character == null)
            character = GetComponent<Character>();
    }
    
    public bool EquipItem(GearItem item)
    {
        if (item == null) return false;
        
        // Check if we have the item in inventory
        if (!inventory.RemoveItem(item))
        {
            return false;
        }
        
        // Find first available slot for this gear type
        int slotIndex = FindAvailableSlot(item.gearType);
        if (slotIndex == -1)
        {
            // No empty slots available, find first occupied slot of this gear type to swap with
            slotIndex = FindFirstOccupiedSlot(item.gearType);
            if (slotIndex == -1)
            {
                // No slots of this gear type exist at all
                inventory.AddItem(item); // Put it back in inventory
                return false;
            }
        }
        
        // Unequip current item in that slot
        GearItem oldItem = equipmentSlots[slotIndex];
        equipmentSlots[slotIndex] = item;
        
        // Add old item back to inventory if it exists
        if (oldItem != null)
        {
            inventory.AddItem(oldItem);
        }
        
        // Apply stat bonuses
        ApplyGearStats();
        
        OnItemEquipped?.Invoke(item, item.gearType);
        OnEquipmentChanged?.Invoke();
        
        Debug.Log($"[EQUIP] Equipped {item.itemName} in slot {slotIndex}" + (oldItem != null ? $" (swapped with {oldItem.itemName})" : ""));
        return true;
    }
    
    /// <summary>
    /// Equip item to a specific slot (for drag and drop)
    /// </summary>
    /// <param name="item">The item to equip</param>
    /// <param name="targetSlot">The specific slot to equip to</param>
    /// <returns>True if successful</returns>
    public bool EquipItemToSlot(GearItem item, int targetSlot)
    {
        if (item == null || targetSlot < 0 || targetSlot >= equipmentSlots.Length) return false;
        
        // Check if the item type matches the slot type
        GearType slotType = GetGearTypeFromSlot(targetSlot);
        if (item.gearType != slotType)
        {
            Debug.LogWarning($"[EQUIP] Cannot equip {item.itemName} (type: {item.gearType}) to slot {targetSlot} (type: {slotType})");
            return false;
        }
        
        // Check if we have the item in inventory
        if (!inventory.RemoveItem(item))
        {
            return false;
        }
        
        // Get the currently equipped item in that slot
        GearItem oldItem = equipmentSlots[targetSlot];
        
        // Equip the new item
        equipmentSlots[targetSlot] = item;
        
        // Add old item back to inventory if it exists
        if (oldItem != null)
        {
            inventory.AddItem(oldItem);
            Debug.Log($"[EQUIP] Swapped {oldItem.itemName} with {item.itemName} in slot {targetSlot}");
        }
        else
        {
            Debug.Log($"[EQUIP] Equipped {item.itemName} to empty slot {targetSlot}");
        }
        
        // Apply stat bonuses
        ApplyGearStats();
        
        OnItemEquipped?.Invoke(item, item.gearType);
        OnEquipmentChanged?.Invoke();
        
        return true;
    }
    
    /// <summary>
    /// Find the first available slot for a specific gear type
    /// </summary>
    /// <param name="gearType">The gear type to find a slot for</param>
    /// <returns>Slot index, or -1 if no available slot</returns>
    int FindAvailableSlot(GearType gearType)
    {
        // Define which slots can hold which gear types
        // This is the single source of truth for equipment slot mapping
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            GearType slotType = GetGearTypeFromSlot(i);
            if (slotType == gearType && equipmentSlots[i] == null)
            {
                return i;
            }
        }
        return -1; // No available slot
    }
    
    /// <summary>
    /// Find the first occupied slot for a specific gear type (for swapping)
    /// </summary>
    int FindFirstOccupiedSlot(GearType gearType)
    {
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            GearType slotType = GetGearTypeFromSlot(i);
            if (slotType == gearType && equipmentSlots[i] != null)
            {
                return i;
            }
        }
        return -1; // No occupied slot found
    }
    
    /// <summary>
    /// Get the gear type for a specific slot index
    /// </summary>
    /// <param name="slotIndex">The slot index</param>
    /// <returns>The gear type for that slot</returns>
    public GearType GetGearTypeFromSlot(int slotIndex)
    {
        switch (slotIndex)
        {
            case 0: return GearType.Helmet;     // Helmet
            case 1: return GearType.Amulet;     // Amulet
            case 2: return GearType.ChestArmor; // Chest armor
            case 3: return GearType.Weapon;     // Weapon
            case 4: return GearType.Pants;      // Pants
            case 5: return GearType.Ring;       // Ring
            case 6: return GearType.Boots;      // Boots
            case 7: return GearType.Ring;       // Ring
            default: return GearType.Weapon;
        }
    }
    
    public GearItem UnequipItem(GearType gearType)
    {
        // Find the first equipped item of this type
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null && equipmentSlots[i].gearType == gearType)
            {
                GearItem item = equipmentSlots[i];
                equipmentSlots[i] = null;
                
                // Remove stat bonuses
                RemoveGearStats(item);
                
                OnItemUnequipped?.Invoke(item, gearType);
                OnEquipmentChanged?.Invoke();
                
                Debug.Log($"[EQUIP] Unequipped {item.itemName} from slot {i}");
                return item;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Unequip item from a specific slot
    /// </summary>
    /// <param name="slotIndex">The slot index to unequip from</param>
    /// <returns>The unequipped item, or null if slot was empty</returns>
    public GearItem UnequipItemFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equipmentSlots.Length)
            return null;
        
        GearItem item = equipmentSlots[slotIndex];
        if (item != null)
        {
            equipmentSlots[slotIndex] = null;
            
            // Remove stat bonuses
            RemoveGearStats(item);
            
            OnItemUnequipped?.Invoke(item, item.gearType);
            OnEquipmentChanged?.Invoke();
            
            Debug.Log($"[EQUIP] Unequipped {item.itemName} from slot {slotIndex}");
        }
        
        return item;
    }
    
    public GearItem GetEquippedItem(GearType gearType)
    {
        // Find the first equipped item of this type
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null && equipmentSlots[i].gearType == gearType)
            {
                return equipmentSlots[i];
            }
        }
        return null;
    }
    
    /// <summary>
    /// Get equipped item from a specific slot
    /// </summary>
    /// <param name="slotIndex">The slot index</param>
    /// <returns>The equipped item, or null if slot is empty</returns>
    public GearItem GetEquippedItemFromSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equipmentSlots.Length)
            return null;
        
        return equipmentSlots[slotIndex];
    }
    
    
    public void ApplyGearStats()
    {
        if (character == null) return;
        
        // Reset to base stats first
        character.ResetToBaseStats();
        
        // Apply all equipped gear bonuses from all slots
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null)
            {
                character.AddGearBonus(equipmentSlots[i]);
            }
        }
    }
    
    public void RemoveGearStats(GearItem item)
    {
        if (character == null || item == null) return;
        
        character.RemoveGearBonus(item);
    }
    
    public int GetTotalAttackBonus()
    {
        int total = 0;
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null)
                total += equipmentSlots[i].attackBonus;
        }
        return total;
    }
    
    public int GetTotalDefenseBonus()
    {
        int total = 0;
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null)
                total += equipmentSlots[i].defenseBonus;
        }
        return total;
    }
    
    public int GetTotalHPBonus()
    {
        int total = 0;
        for (int i = 0; i < equipmentSlots.Length; i++)
        {
            if (equipmentSlots[i] != null)
                total += equipmentSlots[i].hpBonus;
        }
        return total;
    }
}
