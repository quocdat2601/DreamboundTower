using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Inventory system for managing player items
/// 
/// USAGE:
/// - Manages 20 inventory slots for storing items
/// - Provides methods to add, remove, and find items
/// - Handles inventory capacity and item management
/// - Provides events for UI updates
/// 
/// SETUP:
/// 1. Attach to player GameObject
/// 2. System automatically initializes with empty slots
/// 3. Use public methods to manage items
/// </summary>
public class Inventory : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int maxSlots = 20;
    
    [Header("Inventory Items")]
    public List<GearItem> items = new List<GearItem>();
    
    // Events for UI updates
    public System.Action<GearItem> OnItemAdded;
    public System.Action<GearItem> OnItemRemoved;
    public System.Action OnInventoryChanged;
    
    void Awake()
    {
        // Initialize empty slots
        while (items.Count < maxSlots)
        {
            items.Add(null);
        }
    }
    
    public bool AddItem(GearItem item)
    {
        // Ensure inventory is properly initialized
        EnsureInitialized();
        
        // Find first empty slot
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                OnItemAdded?.Invoke(item);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Add item without triggering events (useful for bulk operations)
    /// </summary>
    public bool AddItemSilent(GearItem item)
    {
        // Ensure inventory is properly initialized
        EnsureInitialized();
        
        // Find first empty slot
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Ensure the inventory is properly initialized with empty slots
    /// </summary>
    void EnsureInitialized()
    {
        if (items.Count < maxSlots)
        {
            while (items.Count < maxSlots)
            {
                items.Add(null);
            }
        }
    }
    
    public bool RemoveItem(GearItem item)
    {
        if (items.Count == 0)
        {
            return false;
        }
        
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == item)
            {
                items[i] = null;
                OnItemRemoved?.Invoke(item);
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        
        return false;
    }
    
    public bool RemoveItemAt(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < items.Count && items[slotIndex] != null)
        {
            GearItem item = items[slotIndex];
            items[slotIndex] = null;
            OnItemRemoved?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        return false;
    }
    
    public GearItem GetItemAt(int slotIndex)
    {
        if (slotIndex >= 0 && slotIndex < items.Count)
        {
            return items[slotIndex];
        }
        return null;
    }
    
    public bool HasSpace()
    {
        return items.Contains(null);
    }
    
    public int GetEmptySlotCount()
    {
        int count = 0;
        foreach (var item in items)
        {
            if (item == null) count++;
        }
        return count;
    }
}
