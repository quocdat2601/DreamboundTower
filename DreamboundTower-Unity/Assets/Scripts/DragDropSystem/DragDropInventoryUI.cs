using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Presets;

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
    
    // Store original slot colors to restore when empty
    private Dictionary<Image, Color> originalSlotColors = new Dictionary<Image, Color>();
    
    void Awake()
    {
        // Store original slot colors IMMEDIATELY in Awake, before anything else
        // This ensures we capture the true original colors from the prefab/scene
        StoreOriginalSlotColors();
    }
    
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

        // Setup tooltip connections (like BattleUIManager does for skills)
        StartCoroutine(ConnectTooltipEvents());
    
    }
    
    IEnumerator ConnectTooltipEvents()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (TooltipManager.Instance == null)
        {
            yield break;
        }
        
        // Connect all BagSlot TooltipTriggers
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i] == null) continue;
            
            GameObject slotGameObject = inventorySlots[i].transform.parent.gameObject;
            GearItem item = (i < inventory?.items.Count) ? inventory.items[i] : null;
            
            ConnectTooltipForSlot(slotGameObject, inventorySlots[i].transform.root, item);
        }
        
        // Connect all Equipment Slot TooltipTriggers
        for (int i = 0; i < equipmentSlots.Count; i++)
        {
            if (equipmentSlots[i] == null) continue;
            
            GameObject slotGameObject = equipmentSlots[i].transform.parent.gameObject;
            GearItem item = (i < equipment?.equipmentSlots.Length) ? equipment.equipmentSlots[i] : null;
            
            ConnectTooltipForSlot(slotGameObject, equipmentSlots[i].transform.root, item);
        }
    }
    
    void ConnectTooltipForSlot(GameObject slotGameObject, Transform rootTransform, GearItem item)
    {
        TooltipTrigger trigger = slotGameObject.GetComponent<TooltipTrigger>();
        if (trigger == null)
        {
            trigger = rootTransform.GetComponentInChildren<TooltipTrigger>();
            if (trigger == null)
            {
                return;
            }
        }
        
        // Connect events
        trigger.OnItemHoverEnter.RemoveAllListeners();
        trigger.OnHoverExit.RemoveAllListeners();
        
        trigger.OnItemHoverEnter.AddListener(ShowItemTooltip);
        trigger.OnHoverExit.AddListener(HideItemTooltip);
        
        // Set dataToShow
        trigger.dataToShow = item;
    }
    
    // Tooltip methods (like BattleUIManager has for skills)
    void ShowItemTooltip(GearItem item)
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.ShowItemTooltip(item);
        }
    }
    
    void HideItemTooltip()
    {
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideAllTooltips();
        }
    }
    
    /// <summary>
    /// Sets up tooltip trigger on the item icon image
    /// </summary>
    void SetupTooltipOnItemIcon(Image itemImage, GearItem item)
    {
        if (itemImage == null) return;
        
        // Get or add TooltipTrigger component to the item icon
        TooltipTrigger trigger = itemImage.GetComponent<TooltipTrigger>();
        if (trigger == null)
        {
            trigger = itemImage.gameObject.AddComponent<TooltipTrigger>();
        }
        
        // Initialize events if null
        if (trigger.OnItemHoverEnter == null)
        {
            trigger.OnItemHoverEnter = new ItemTooltipEvent();
        }
        if (trigger.OnHoverExit == null)
        {
            trigger.OnHoverExit = new UnityEvent();
        }
        
        // Set the data
        trigger.dataToShow = item;
        
        // Connect event listeners
        trigger.OnItemHoverEnter.RemoveAllListeners();
        trigger.OnHoverExit.RemoveAllListeners();
        
        if (item != null)
        {
            trigger.OnItemHoverEnter.AddListener(ShowItemTooltip);
            trigger.OnHoverExit.AddListener(HideItemTooltip);
        }
        
        // Ensure the item icon can receive raycasts for tooltips
        if (itemImage != null)
        {
            itemImage.raycastTarget = (item != null);
        }
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
    /// Store original colors of all slots to restore later
    /// Only stores colors that are NOT rarity colors (to avoid storing purple/etc as original)
    /// </summary>
    void StoreOriginalSlotColors()
    {
        originalSlotColors.Clear();
        
        // Check if a color looks like a rarity color (epic purple, legendary gold, etc)
        // Rarity colors are typically bright/saturated, original slot colors are usually gray/dark
        bool IsLikelyRarityColor(Color color)
        {
            // Rarity colors are usually bright (high saturation) and not gray
            float saturation = Mathf.Max(color.r, color.g, color.b) - Mathf.Min(color.r, color.g, color.b);
            
            // Epic purple: high R and B, low G
            bool looksEpic = color.r > 0.5f && color.b > 0.5f && color.g < 0.5f;
            // Legendary gold: high R and G, low B
            bool looksLegendary = color.r > 0.8f && color.g > 0.7f && color.b < 0.3f;
            // Rare blue: high B, low R and G
            bool looksRare = color.b > 0.5f && color.r < 0.5f && color.g < 0.5f;
            
            return looksEpic || looksLegendary || looksRare || saturation > 0.5f;
        }
        
        // Default empty slot color (dark gray) - use if slot is transparent or has rarity color
        Color defaultEmpty = new Color(0.2f, 0.2f, 0.2f, 0.3f);
        
        // Store inventory slot colors (only if not a rarity color and not transparent)
        foreach (Image slotImage in inventorySlots)
        {
            if (slotImage != null && !originalSlotColors.ContainsKey(slotImage))
            {
                Color currentColor = slotImage.color;
                
                // Check if color is transparent (alpha < 0.1) - if so, use default
                bool isTransparent = currentColor.a < 0.1f;
                
                // Only store if it doesn't look like a rarity color AND is not transparent
                if (!isTransparent && !IsLikelyRarityColor(currentColor))
                {
                    originalSlotColors[slotImage] = currentColor;
                }
                else
                {
                    // Use default empty slot color for transparent or rarity colors
                    originalSlotColors[slotImage] = defaultEmpty;
                }
            }
            
            // Also store parent Image color if exists (we apply rarity to it too)
            if (slotImage != null && slotImage.transform.parent != null)
            {
                Image parentBgImage = slotImage.transform.parent.GetComponent<Image>();
                if (parentBgImage != null && parentBgImage != slotImage && !originalSlotColors.ContainsKey(parentBgImage))
                {
                    Color parentColor = parentBgImage.color;
                    bool isTransparent = parentColor.a < 0.1f;
                    if (!isTransparent && !IsLikelyRarityColor(parentColor))
                    {
                        originalSlotColors[parentBgImage] = parentColor;
                    }
                    else
                    {
                        originalSlotColors[parentBgImage] = defaultEmpty;
                    }
                }
            }
        }
        
        // Store equipment slot colors (only if not a rarity color and not transparent)
        foreach (Image slotImage in equipmentSlots)
        {
            if (slotImage != null && !originalSlotColors.ContainsKey(slotImage))
            {
                Color currentColor = slotImage.color;
                
                // Check if color is transparent (alpha < 0.1) - if so, use default
                bool isTransparent = currentColor.a < 0.1f;
                
                // Only store if it doesn't look like a rarity color AND is not transparent
                if (!isTransparent && !IsLikelyRarityColor(currentColor))
                {
                    originalSlotColors[slotImage] = currentColor;
                }
                else
                {
                    // Use default empty slot color for transparent or rarity colors
                    originalSlotColors[slotImage] = defaultEmpty;
                }
            }
            
            // Also store parent Image color if exists (we apply rarity to it too)
            if (slotImage != null && slotImage.transform.parent != null)
            {
                Image parentBgImage = slotImage.transform.parent.GetComponent<Image>();
                if (parentBgImage != null && parentBgImage != slotImage && !originalSlotColors.ContainsKey(parentBgImage))
                {
                    Color parentColor = parentBgImage.color;
                    bool isTransparent = parentColor.a < 0.1f;
                    if (!isTransparent && !IsLikelyRarityColor(parentColor))
                    {
                        originalSlotColors[parentBgImage] = parentColor;
                    }
                    else
                    {
                        originalSlotColors[parentBgImage] = defaultEmpty;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get the original color for a slot (restore when empty)
    /// </summary>
    Color GetOriginalSlotColor(Image slotImage)
    {
        if (slotImage != null && originalSlotColors.ContainsKey(slotImage))
        {
            return originalSlotColors[slotImage];
        }
        // Fallback to default empty color if not stored
        return new Color(0.2f, 0.2f, 0.2f, 0.3f);
    }
    
    /// <summary>
    /// Apply rarity background color to slot image and related images (background child, parent)
    /// </summary>
    void ApplyRarityToSlotImage(Image slotImage, ItemRarity rarity)
    {
        if (slotImage == null) return;
        
        // Apply to main slot image
        RarityColorUtility.ApplyRarityBackground(slotImage, rarity);
        
        // Update DropZone's originalColor to preserve rarity color after drag operations
        DropZone dropZone = slotImage.GetComponent<DropZone>();
        if (dropZone != null)
        {
            dropZone.UpdateOriginalColor();
        }
        
        // Also check for a "Background" child object
        Transform bgTransform = slotImage.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                RarityColorUtility.ApplyRarityBackground(bgImage, rarity);
            }
        }
        
        // Also check parent GameObject for background Image
        if (slotImage.transform.parent != null)
        {
            Image parentBgImage = slotImage.transform.parent.GetComponent<Image>();
            if (parentBgImage != null && parentBgImage != slotImage)
            {
                RarityColorUtility.ApplyRarityBackground(parentBgImage, rarity);
            }
        }
    }
    
    /// <summary>
    /// Restore original slot color to slot image and related images (background child, parent)
    /// </summary>
    void RestoreSlotColor(Image slotImage)
    {
        if (slotImage == null) return;
        
        Color originalColor = GetOriginalSlotColor(slotImage);
        
        // Restore the slot image color
        slotImage.color = originalColor;
        
        // Also reset background child if exists
        Transform bgTransform = slotImage.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = originalColor;
            }
        }
        
        // Also reset parent GameObject background Image if exists
        if (slotImage.transform.parent != null)
        {
            Image parentBgImage = slotImage.transform.parent.GetComponent<Image>();
            if (parentBgImage != null && parentBgImage != slotImage)
            {
                // Use stored parent color if available, otherwise use slot's original color
                if (originalSlotColors.ContainsKey(parentBgImage))
                {
                    parentBgImage.color = originalSlotColors[parentBgImage];
                }
                else
                {
                    parentBgImage.color = originalColor;
                }
            }
        }
        
        // Update DropZone's originalColor to match restored original
        DropZone dropZone = slotImage.GetComponent<DropZone>();
        if (dropZone != null)
        {
            dropZone.originalColor = originalColor;
        }
    }
    
    /// <summary>
    /// Find ItemIcon Image component in a slot (not the slot background)
    /// </summary>
    Image FindItemIconImage(Image slotImage)
    {
        if (slotImage == null) return null;
        
        // Look for an Image component that's not the slot background
        Image[] allImages = slotImage.GetComponentsInChildren<Image>();
        foreach (Image img in allImages)
        {
            if (img != slotImage) // Not the slot background, this is ItemIcon
            {
                return img;
            }
        }
        
        // If no child Image found, create one
        return CreateItemIconImage(slotImage);
    }
    
    /// <summary>
    /// Update ItemIcon image based on item (show icon if item exists, hide if empty)
    /// </summary>
    Image UpdateItemIconImage(Image slotImage, GearItem item)
    {
        if (slotImage == null) return null;
        
        Image itemImage = FindItemIconImage(slotImage);
        
        // Update the ItemIcon
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
                itemImage.gameObject.SetActive(true);
            }
            else
            {
                // Clear ItemIcon when slot is empty
                itemImage.sprite = null;
                itemImage.color = Color.clear;
                itemImage.enabled = false;
                itemImage.gameObject.SetActive(false);
            }
        }
        
        return itemImage;
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
        
        // Apply rarity background or restore original color
        if (item != null)
        {
            ApplyRarityToSlotImage(slotImage, item.rarity);
        }
        else
        {
            RestoreSlotColor(slotImage);
        }
        
        // Find DraggableItem component
        DraggableItem draggableItem = slotImage.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = slotImage.GetComponentInChildren<DraggableItem>();
        }
        
        // Update ItemIcon and get reference for tooltip
        Image itemImage = UpdateItemIconImage(slotImage, item);
        
        // Update DraggableItem for drag functionality
        if (draggableItem != null)
        {
            draggableItem.SetItemData(item, slotIndex, true);
            draggableItem.dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
        
        // Setup tooltip on the item icon
        SetupTooltipOnItemIcon(itemImage, item);
    }
    
    /// <summary>
    /// Update a specific equipment slot
    /// </summary>
    void UpdateEquipmentSlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= equipmentSlots.Count) return;
        
        // Update the actual slot Image
        Image slotImage = equipmentSlots[slotIndex];
        
        // Apply rarity background or restore original color
        if (item != null)
        {
            ApplyRarityToSlotImage(slotImage, item.rarity);
        }
        else
        {
            RestoreSlotColor(slotImage);
        }
        
        // Find DraggableItem component
        DraggableItem draggableItem = slotImage.GetComponent<DraggableItem>();
        if (draggableItem == null)
        {
            draggableItem = slotImage.GetComponentInChildren<DraggableItem>();
        }
        
        // Update ItemIcon and get reference for tooltip
        Image itemImage = UpdateItemIconImage(slotImage, item);
        
        // Update DraggableItem for drag functionality
        if (draggableItem != null)
        {
            draggableItem.SetItemData(item, slotIndex, false);
            draggableItem.dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
        
        // Setup tooltip on the item icon
        SetupTooltipOnItemIcon(itemImage, item);
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