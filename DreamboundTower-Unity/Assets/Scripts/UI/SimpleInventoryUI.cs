using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class SimpleInventoryUI : MonoBehaviour
{
    [Header("Inventory Slots (20 slots)")]
    public List<Button> inventorySlotButtons = new List<Button>();
    public List<Image> inventorySlotIcons = new List<Image>();
    
    [Header("Equipment Slots (8 slots)")]
    public List<Button> equipmentSlotButtons = new List<Button>();
    public List<Image> equipmentSlotIcons = new List<Image>();
    
    [Header("References")]
    public Inventory inventory;
    public Equipment equipment;
    
    void Start()
    {
        // Setup button listeners first (these don't need inventory references)
        SetupInventoryButtons();
        SetupEquipmentButtons();
        
        // Try to initialize immediately, but if it fails, try again later
        TryInitialize();
    }
    
    void TryInitialize()
    {
        // Find the scene instance components
        inventory = FindFirstObjectByType<Inventory>();
        equipment = FindFirstObjectByType<Equipment>();
        
        if (inventory != null && equipment != null)
        {
            // Subscribe to events
            inventory.OnInventoryChanged += UpdateInventoryUI;
            equipment.OnEquipmentChanged += UpdateEquipmentUI;
            
            // Initial update
            UpdateAllUI();
        }
        else
        {
            // Retry in 0.5 seconds if player not spawned yet
            Invoke(nameof(TryInitialize), 0.5f);
        }
    }
    
    void SetupInventoryButtons()
    {
        for (int i = 0; i < inventorySlotButtons.Count; i++)
        {
            int slotIndex = i; // Capture for closure
            inventorySlotButtons[i].onClick.AddListener(() => OnInventorySlotClicked(slotIndex));
        }
    }
    
    void SetupEquipmentButtons()
    {
        for (int i = 0; i < equipmentSlotButtons.Count; i++)
        {
            int slotIndex = i; // Capture for closure
            equipmentSlotButtons[i].onClick.AddListener(() => OnEquipmentSlotClicked(slotIndex));
        }
    }
    
    void OnInventorySlotClicked(int slotIndex)
    {
        if (inventory == null) return;
        
        GearItem item = inventory.GetItemAt(slotIndex);
        
        if (item != null)
        {
            // Try to equip the item
            if (equipment != null)
            {
                equipment.EquipItem(item);
            }
        }
    }
    
    void OnEquipmentSlotClicked(int slotIndex)
    {
        if (equipment == null) return;
        
        // Get the item from the specific slot
        GearItem item = equipment.GetEquippedItemFromSlot(slotIndex);
        
        if (item != null)
        {
            // Unequip the item from the specific slot
            GearItem unequippedItem = equipment.UnequipItemFromSlot(slotIndex);
            
            // Add the unequipped item back to inventory
            if (unequippedItem != null && inventory != null)
            {
                inventory.AddItem(unequippedItem);
            }
        }
    }
    
    
    void UpdateInventoryUI()
    {
        if (inventory == null) return;
        
        for (int i = 0; i < Mathf.Min(inventorySlotIcons.Count, inventory.items.Count); i++)
        {
            GearItem item = inventory.items[i];
            UpdateInventorySlot(i, item);
        }
    }
    
    void UpdateEquipmentUI()
    {
        if (equipment == null) return;
        
        // Update equipment slots based on equipped items
        for (int i = 0; i < equipmentSlotIcons.Count; i++)
        {
            GearItem item = equipment.GetEquippedItemFromSlot(i);
            UpdateEquipmentSlot(i, item);
        }
    }
    
    void UpdateInventorySlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= inventorySlotIcons.Count) return;
        
        Image icon = inventorySlotIcons[slotIndex];
        
        if (item != null)
        {
            icon.sprite = item.icon;
            icon.color = Color.white;
        }
        else
        {
            icon.sprite = null;
            icon.color = Color.clear;
        }
        
        // Force Canvas refresh to ensure UI updates immediately
        Canvas.ForceUpdateCanvases();
    }
    
    void UpdateEquipmentSlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= equipmentSlotIcons.Count) return;
        
        Image icon = equipmentSlotIcons[slotIndex];
        
        if (item != null)
        {
            icon.sprite = item.icon;
            icon.color = Color.white;
        }
        else
        {
            icon.sprite = null;
            icon.color = Color.clear;
        }
        
    }
    
    void UpdateAllUI()
    {
        UpdateInventoryUI();
        UpdateEquipmentUI();
    }
    
    /// <summary>
    /// Force refresh the entire UI (useful for debugging)
    /// </summary>
    [ContextMenu("Force Refresh UI")]
    public void ForceRefreshUI()
    {
        UpdateAllUI();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(GetComponent<RectTransform>());
        
        foreach (var icon in inventorySlotIcons)
        {
            if (icon != null)
            {
                icon.SetAllDirty();
            }
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= UpdateInventoryUI;
        }
        if (equipment != null)
        {
            equipment.OnEquipmentChanged -= UpdateEquipmentUI;
        }
    }
}
