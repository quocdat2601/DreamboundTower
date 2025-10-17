using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Enhanced inventory UI with drag and drop support
/// 
/// USAGE:
/// - Manages visual representation of inventory and equipment slots
/// - Updates UI when items are moved, equipped, or unequipped
/// - Handles slot highlighting and visual feedback
/// - Automatically finds Inventory and Equipment components
/// 
/// SETUP:
/// 1. Attach to UI Canvas or Panel
/// 2. Assign inventory and equipment slot Image references
/// 3. System automatically finds required components
/// </summary>
public class DragDropInventoryUI : MonoBehaviour
{
    [Header("Inventory Slots (20 slots)")]
    public List<Image> inventorySlots = new List<Image>();
    
    [Header("Equipment Slots (8 slots)")]
    public List<Image> equipmentSlots = new List<Image>();
    
    [Header("References")]
    public Inventory inventory;
    public Equipment equipment;
    public DragDropSystem dragDropSystem;
    
    [Header("Prefabs")]
    public GameObject inventoryItemPrefab;
    public GameObject equipmentItemPrefab;
    
    // Runtime objects
    private List<GameObject> inventoryItemObjects = new List<GameObject>();
    private List<GameObject> equipmentItemObjects = new List<GameObject>();
    
    void Start()
    {
        // Components should be assigned in inspector
        
        // Subscribe to events
        if (inventory != null)
        {
            inventory.OnInventoryChanged += UpdateInventoryUI;
        }
        
        if (equipment != null)
        {
            equipment.OnEquipmentChanged += UpdateEquipmentUI;
        }
        
        // Initialize UI
        InitializeInventorySlots();
        InitializeEquipmentSlots();
        UpdateAllUI();
        
        // Force initial equipment UI update after a short delay to ensure everything is set up
        Invoke(nameof(ForceEquipmentUpdate), 0.1f);
        
        // Also try to find the correct Equipment reference after a longer delay
        Invoke(nameof(FindCorrectEquipmentReference), 1.0f);
        
        // Try again after an even longer delay to ensure the PlayerPrefab is fully instantiated
        Invoke(nameof(FindCorrectEquipmentReference), 2.0f);
        
        // Also try to find the correct inventory reference
        Invoke(nameof(FindCorrectInventoryReference), 1.0f);
        Invoke(nameof(FindCorrectInventoryReference), 2.0f);
    }
    
    /// <summary>
    /// Find the correct Equipment reference (the instantiated one, not the prefab)
    /// </summary>
    public void FindCorrectEquipmentReference()
    {
        // Find all Equipment components
        Equipment[] allEquipment = FindObjectsByType<Equipment>(FindObjectsSortMode.None);
        
        if (allEquipment.Length == 0) return;
        
        // Look for Equipment components that have equipped items (the instantiated ones)
        foreach (var equip in allEquipment)
        {
            // Check if this Equipment has any equipped items
            bool hasItems = equip.equipmentSlots.Any(item => item != null);
            if (hasItems)
            {
                // Only switch if we don't already have the correct reference
                if (equipment != equip)
                {
                    // Unsubscribe from old equipment
                    if (equipment != null)
                    {
                        equipment.OnEquipmentChanged -= UpdateEquipmentUI;
                    }
                    
                    // Subscribe to new equipment
                    equipment = equip;
                    equipment.OnEquipmentChanged += UpdateEquipmentUI;
                    
                    // Initialize equipment slots now that we have the correct reference
                    InitializeEquipmentSlots();
                    
                    // Force UI update
                    UpdateEquipmentUI();
                }
                break;
            }
        }
    }
    
    /// <summary>
    /// Force equipment UI update - called after initialization to ensure proper setup
    /// </summary>
    void ForceEquipmentUpdate()
    {
        if (equipment != null)
        {
            UpdateEquipmentUI();
        }
    }
    
    /// <summary>
    /// Find the correct Inventory reference (the instantiated one, not the prefab)
    /// </summary>
    public void FindCorrectInventoryReference()
    {
        // Find all Inventory components
        Inventory[] allInventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        
        if (allInventories.Length == 0) return;
        
        // Look for Inventory components that have items (the instantiated ones)
        foreach (var inv in allInventories)
        {
            // Check if this Inventory has any items
            bool hasItems = inv.items.Count > 0;
            if (hasItems)
            {
                // Only switch if we don't already have the correct reference
                if (inventory != inv)
                {
                    // Unsubscribe from old inventory
                    if (inventory != null)
                    {
                        inventory.OnInventoryChanged -= UpdateInventoryUI;
                    }
                    
                    // Subscribe to new inventory
                    inventory = inv;
                    inventory.OnInventoryChanged += UpdateInventoryUI;
                    
                    // Force UI update
                    UpdateInventoryUI();
                    
                }
                    break;
            }
        }
    }
    
    /// <summary>
    /// Initialize inventory slots with draggable items
    /// </summary>
    void InitializeInventorySlots()
    {
        // Clear existing objects
        foreach (GameObject obj in inventoryItemObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        inventoryItemObjects.Clear();
        
        // Add drag and drop components directly to slot images
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] != null)
            {
                AddDragDropToSlot(inventorySlots[i].gameObject, i, true);
            }
        }
    }
    
    /// <summary>
    /// Initialize equipment slots with draggable items
    /// </summary>
    void InitializeEquipmentSlots()
    {
        // Only initialize if equipment reference is available
        if (equipment == null) return;
        
        // Clear existing objects
        foreach (GameObject obj in equipmentItemObjects)
        {
            if (obj != null) DestroyImmediate(obj);
        }
        equipmentItemObjects.Clear();
        
        // Add drag and drop components to equipment slots
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            if (equipmentSlots[i] != null)
            {
                AddDragDropToSlot(equipmentSlots[i].gameObject, i, false);
            }
        }
    }
    
    /// <summary>
    /// Add drag and drop components to a slot
    /// </summary>
    void AddDragDropToSlot(GameObject slotObject, int slotIndex, bool isInventorySlot)
    {
        // Get DraggableItem component (should already exist)
        DraggableItem draggableItem = slotObject.GetComponent<DraggableItem>();
        if (draggableItem != null)
        {
            draggableItem.SetItemData(null, slotIndex, isInventorySlot);
        }
        
        // Get DropZone component (should already exist)
        DropZone dropZone = slotObject.GetComponent<DropZone>();
        if (dropZone != null)
        {
            dropZone.slotIndex = slotIndex;
            dropZone.isInventorySlot = isInventorySlot;
        }
        
        // Set accepted gear types for equipment slots
        if (!isInventorySlot && equipment != null)
        {
            GearType slotGearType = equipment.GetGearTypeFromSlot(slotIndex);
            if (dropZone != null)
            {
                dropZone.SetAcceptedGearTypes(new List<GearType> { slotGearType });
            }
        }
    }
    
    /// <summary>
    /// Update inventory UI
    /// </summary>
    void UpdateInventoryUI()
    {
        if (inventory == null) return;
        
        // Update ALL inventory slots, not just the ones with items
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            GearItem item = (i < inventory.items.Count) ? inventory.items[i] : null;
            UpdateInventorySlot(i, item);
        }
    }
    
    /// <summary>
    /// Update equipment UI
    /// </summary>
    void UpdateEquipmentUI()
    {
        if (equipment == null) return;
        
        // Update ALL equipment slots, not just the ones with items
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            GearItem item = (i < equipment.equipmentSlots.Length) ? equipment.equipmentSlots[i] : null;
            UpdateEquipmentSlot(i, item);
        }
    }
    
    /// <summary>
    /// Update a specific inventory slot
    /// </summary>
    void UpdateInventorySlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= inventorySlots.Count) return;
        
        // Update the actual slot Image
        Image slotImage = inventorySlots[slotIndex];
        
        // Find DraggableItem component (could be on the slot image or its children)
        DraggableItem draggableItem = slotImage.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = slotImage.GetComponentInChildren<DraggableItem>();
        }
        
        // Find the item Image component (not the slot background Image)
        Image itemImage = null;
        
        // Look for an Image component that's not the slot background
        Image[] allImages = slotImage.GetComponentsInChildren<Image>();
        foreach (Image img in allImages)
        {
            if (img != slotImage) // Not the slot background
            {
                itemImage = img;
                break;
            }
        }
        
        // If no child Image found, create one
        if (itemImage == null)
        {
            itemImage = CreateItemIconImage(slotImage);
        }
        
        // Update the item Image
        if (itemImage != null)
        {
            if (item != null && item.icon != null)
            {
                itemImage.sprite = item.icon;
                itemImage.color = Color.white;
                itemImage.enabled = true;
                itemImage.raycastTarget = true;
                itemImage.maskable = true;
                itemImage.transform.SetAsLastSibling();
            }
            else
            {
                itemImage.sprite = null;
                itemImage.color = Color.clear;
            }
        }
        
        // Still update the DraggableItem for drag functionality
        if (draggableItem != null)
        {
            draggableItem.SetItemData(item, slotIndex, true);
            
            // Always ensure the DraggableItem has the correct dragDropSystem reference
            draggableItem.dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
    }
    
    /// <summary>
    /// Update a specific equipment slot
    /// </summary>
    void UpdateEquipmentSlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= equipmentSlots.Count) return;
        
        // Update the actual slot Image
        Image slotImage = equipmentSlots[slotIndex];
        
        // Find DraggableItem component (could be on the slot image or its children)
        DraggableItem draggableItem = slotImage.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = slotImage.GetComponentInChildren<DraggableItem>();
        }
        
        // Find the item Image component (not the slot background Image) - same logic as inventory
        Image itemImage = null;
        
        // Look for an Image component that's not the slot background
        Image[] allImages = slotImage.GetComponentsInChildren<Image>();
        foreach (Image img in allImages)
        {
            if (img != slotImage) // Not the slot background
            {
                itemImage = img;
                break;
            }
        }
        
        // If no child Image found, create one
        if (itemImage == null)
        {
            itemImage = CreateItemIconImage(slotImage);
        }
        
        // Update the item Image directly
        if (itemImage != null)
        {
            if (item != null && item.icon != null)
            {
                itemImage.sprite = item.icon;
                itemImage.color = Color.white;
                itemImage.enabled = true;
                itemImage.raycastTarget = true;
                itemImage.maskable = true;
                itemImage.transform.SetAsLastSibling();
                
                // Force the image to be visible
                itemImage.gameObject.SetActive(true);
            }
            else
            {
                itemImage.sprite = null;
                itemImage.color = Color.clear;
                itemImage.enabled = false;
            }
        }
        
        // Update DraggableItem data (this will also try to update visuals, but we've already done it above)
        if (draggableItem != null)
        {
            draggableItem.SetItemData(item, slotIndex, false);
            
            // Always ensure the DraggableItem has the correct dragDropSystem reference
            draggableItem.dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
    }
    
    /// <summary>
    /// Create an Image component for displaying item icons
    /// </summary>
    Image CreateItemIconImage(Image slotImage)
    {
        // Create a child GameObject for the item icon
        GameObject itemIconObj = new GameObject("ItemIcon");
        itemIconObj.transform.SetParent(slotImage.transform, false);
        
        // Add Image component
        Image itemIcon = itemIconObj.AddComponent<Image>();
        itemIcon.color = Color.clear; // Start invisible
        
        // Set up RectTransform to fill the parent slot
        RectTransform rectTransform = itemIconObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        
        // Make it slightly smaller than the slot to show the slot background
        rectTransform.offsetMin = new Vector2(4, 4);
        rectTransform.offsetMax = new Vector2(-4, -4);
        
        return itemIcon;
    }
    
    /// <summary>
    /// Update all UI elements
    /// </summary>
    void UpdateAllUI()
    {
        UpdateInventoryUI();
        UpdateEquipmentUI();
    }
    
    /// <summary>
    /// Force refresh the entire UI
    /// </summary>
    [ContextMenu("Force Refresh UI")]
    public void ForceRefreshUI()
    {
        UpdateAllUI();
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Force refresh equipment UI only
    /// </summary>
    [ContextMenu("Force Refresh Equipment UI")]
    public void ForceRefreshEquipmentUI()
    {
        UpdateEquipmentUI();
        Canvas.ForceUpdateCanvases();
    }
    
    /// <summary>
    /// Debug method to check equipment slot setup
    /// </summary>
    [ContextMenu("Debug Equipment Slots")]
    public void DebugEquipmentSlots()
    {
        if (equipment == null)
        {
            Debug.Log("[DragDropInventoryUI] Equipment reference is null");
            return;
        }
        
        Debug.Log($"[DragDropInventoryUI] Equipment slots count: {equipment.equipmentSlots.Length}");
        for (int i = 0; i < equipment.equipmentSlots.Length; i++)
        {
            GearItem item = equipment.equipmentSlots[i];
            if (item != null)
            {
                Debug.Log($"[DragDropInventoryUI] Slot {i}: {item.itemName} (icon: {(item.icon != null ? "exists" : "null")})");
            }
            else
            {
                Debug.Log($"[DragDropInventoryUI] Slot {i}: empty");
            }
        }
    }
    
    
    
    /// <summary>
    /// Update the inventory reference and resubscribe to events
    /// </summary>
    public void UpdateInventoryReference(Inventory newInventory)
    {
        // Unsubscribe from old inventory
        if (inventory != null)
        {
            inventory.OnInventoryChanged -= UpdateInventoryUI;
        }
        
        // Set new inventory reference
        inventory = newInventory;
        
        // Subscribe to new inventory events
        if (inventory != null)
        {
            inventory.OnInventoryChanged += UpdateInventoryUI;
            UpdateInventoryUI();
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