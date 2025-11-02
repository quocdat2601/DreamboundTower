using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Central drag and drop system for inventory management
/// 
/// USAGE:
/// - Handles dragging items between inventory and equipment slots
/// - Provides visual feedback during drag operations
/// - Manages item validation and placement logic
/// - Automatically finds Inventory and Equipment components
/// 
/// SETUP:
/// 1. Attach to a GameObject in the scene
/// 2. Assign dragPreviewPrefab and dragCanvas references
/// 3. System automatically finds required components
/// </summary>
public class DragDropSystem : MonoBehaviour
{
    [Header("Drag and Drop Settings")]
    [Tooltip("The visual representation of the item being dragged")]
    public GameObject dragPreviewPrefab;
    
    [Tooltip("Canvas to render the drag preview on")]
    public Canvas dragCanvas;
    
    [Tooltip("Z-offset for the drag preview to appear above other UI")]
    public float dragPreviewZOffset = 100f;
    
    [Header("Visual Feedback")]
    [Tooltip("Color to highlight valid drop zones")]
    public Color validDropColor = Color.green;
    
    [Tooltip("Color to highlight invalid drop zones")]
    public Color invalidDropColor = Color.red;
    
    
    // Current drag state
    private bool isDragging = false;
    private DraggableItem currentDraggedItem;
    private GameObject dragPreview;
    private Vector3 dragOffset;
    
    // Drop zone tracking
    private DropZone currentHoveredDropZone;
    
    // References
    public Inventory inventory;
    public Equipment equipment;
    
    // Events
    public System.Action<GearItem, int, int> OnItemMoved; // item, fromSlot, toSlot
    public System.Action<GearItem, int> OnItemEquipped; // item, equipmentSlot
    public System.Action<GearItem, int> OnItemUnequipped; // item, inventorySlot
    
    void Awake()
    {
        // Canvas should be assigned in inspector
        if (dragCanvas == null) return;
    }
    
    void Start()
    {
        // Try to find references immediately
        TryFindReferences();
        
    }
    
    /// <summary>
    /// Try to find the required references (Inventory and Equipment)
    /// This handles the case where PlayerPrefab is instantiated after DragDropSystem
    /// </summary>
    void TryFindReferences()
    {
        // Find the correct Equipment component (the instantiated one, not the prefab)
        Invoke(nameof(RetryFindEquipment), 0.5f);
        Invoke(nameof(RetryFindEquipment), 1.0f);
        
        // Find the correct Inventory component (the instantiated one, not the prefab)
        Invoke(nameof(RetryFindInventory), 0.5f);
        Invoke(nameof(RetryFindInventory), 1.0f);
    }
    
    /// <summary>
    /// Retry finding the Equipment component
    /// </summary>
    void RetryFindEquipment()
    {
        // Priority 1: Find equipment from BattleManager's active player
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            Character playerCharacter = battleManager.GetPlayerCharacter();
            if (playerCharacter != null)
            {
                Equipment playerEquipment = playerCharacter.GetComponent<Equipment>();
                if (playerEquipment != null && playerEquipment.gameObject.activeInHierarchy)
                {
                    equipment = playerEquipment;
                    return;
                }
            }
        }
        
        // Priority 2: Find equipment from GameManager's persistent player
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            Equipment persistentEquipment = GameManager.Instance.playerInstance.GetComponent<Equipment>();
            if (persistentEquipment != null && persistentEquipment.gameObject.activeInHierarchy)
            {
                equipment = persistentEquipment;
                return;
            }
        }
        
        // Priority 3: Find any active equipment in the scene
        Equipment[] allEquipment = FindObjectsByType<Equipment>(FindObjectsSortMode.None);
        foreach (var equip in allEquipment)
        {
            if (equip.gameObject.activeInHierarchy)
            {
                equipment = equip;
                return;
            }
        }
    }
    
    /// <summary>
    /// Retry finding the Inventory component
    /// </summary>
    void RetryFindInventory()
    {
        // Priority 1: Find inventory from BattleManager's active player
        BattleManager battleManager = FindFirstObjectByType<BattleManager>();
        if (battleManager != null)
        {
            Character playerCharacter = battleManager.GetPlayerCharacter();
            if (playerCharacter != null)
            {
                Inventory playerInventory = playerCharacter.GetComponent<Inventory>();
                if (playerInventory != null && playerInventory.gameObject.activeInHierarchy)
                {
                    inventory = playerInventory;
                    return;
                }
            }
        }
        
        // Priority 2: Find inventory from GameManager's persistent player
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            Inventory persistentInventory = GameManager.Instance.playerInstance.GetComponent<Inventory>();
            if (persistentInventory != null && persistentInventory.gameObject.activeInHierarchy)
            {
                inventory = persistentInventory;
                return;
            }
        }
        
        // Priority 3: Find any active inventory in the scene
        Inventory[] allInventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        foreach (var inv in allInventories)
        {
            if (inv.gameObject.activeInHierarchy)
            {
                inventory = inv;
                return;
            }
        }
    }
    
    
    void Update()
    {
        if (isDragging)
        {
            UpdateDragPreview();
            UpdateDropZoneHighlighting();
        }
    }
    
    /// <summary>
    /// Start dragging an item
    /// </summary>
    /// <param name="draggableItem">The item being dragged</param>
    /// <param name="mousePosition">Initial mouse position</param>
    public void StartDrag(DraggableItem draggableItem, Vector3 mousePosition)
    {
        if (draggableItem == null || draggableItem.item == null) return;
        
        isDragging = true;
        currentDraggedItem = draggableItem;
        
        // Create drag preview
        CreateDragPreview(draggableItem.item);
        
        // For Screen Space - Overlay, position directly at mouse cursor
        // No need for complex offset calculation
        dragOffset = Vector3.zero;
        
        Debug.Log($"[DRAG] StartDrag - Mouse: {mousePosition}, Offset set to zero for ScreenSpaceOverlay");
        
        // Hide original item during drag
        draggableItem.SetDragging(true);
    }
    
    /// <summary>
    /// End the drag operation
    /// </summary>
    /// <param name="mousePosition">Final mouse position</param>
    public void EndDrag(Vector3 mousePosition)
    {
        if (!isDragging || currentDraggedItem == null) return;
        
        // Find drop zone under mouse
        DropZone targetDropZone = GetDropZoneUnderMouse(mousePosition);
        
        if (targetDropZone != null && CanDropOnZone(targetDropZone))
        {
            // Perform the drop
            PerformDrop(currentDraggedItem, targetDropZone);
        }
        else
        {
            // Return item to original position
            ReturnItemToOriginalPosition();
        }
        
        // Force UI refresh immediately before cleanup to ensure slots are updated
        // This ensures that when SetHighlight(false) is called, GetCurrentItem() returns correct state
        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            ui.ForceRefreshUI();
        }
        
        // Clean up drag state (this will call SetHighlight(false) which uses GetCurrentItem())
        CleanupDrag();
    }
    
    /// <summary>
    /// Create visual preview of the item being dragged
    /// </summary>
    void CreateDragPreview(GearItem item)
    {
        if (dragPreviewPrefab != null)
        {
            dragPreview = Instantiate(dragPreviewPrefab, dragCanvas.transform);
            
            // Set the item icon on the prefab
            Image previewImage = dragPreview.GetComponent<Image>();
            if (previewImage != null && item != null)
            {
                previewImage.sprite = item.icon;
                previewImage.color = new Color(1f, 1f, 1f, 0.8f); // Semi-transparent
            }
        }
        else
        {
            // Create simple drag preview
            dragPreview = new GameObject("DragPreview");
            dragPreview.transform.SetParent(dragCanvas.transform, false);
            
            Image previewImage = dragPreview.AddComponent<Image>();
            if (item != null)
            {
                previewImage.sprite = item.icon;
            }
            previewImage.color = new Color(1f, 1f, 1f, 0.8f); // Semi-transparent
            
            RectTransform rect = dragPreview.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(64, 64); // Standard item size
        }
        
        // Set high sorting order to appear above everything
        Canvas previewCanvas = dragPreview.GetComponent<Canvas>();
        if (previewCanvas == null)
        {
            previewCanvas = dragPreview.AddComponent<Canvas>();
        }
        previewCanvas.overrideSorting = true;
        previewCanvas.sortingOrder = 1000;
        
        // Add graphic raycaster to prevent blocking
        if (dragPreview.GetComponent<GraphicRaycaster>() == null)
        {
            dragPreview.AddComponent<GraphicRaycaster>();
        }
    }
    
    /// <summary>
    /// Update the position of the drag preview
    /// </summary>
    void UpdateDragPreview()
    {
        if (dragPreview == null) return;
        
        Vector3 mousePos = Mouse.current.position.ReadValue();
        
        
        // For UI elements, use RectTransform.anchoredPosition instead of transform.position
        RectTransform rectTransform = dragPreview.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // Check canvas render mode
            if (dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                // For Screen Space - Overlay, use mouse position directly
                rectTransform.position = mousePos;
            }
            else
            {
                // For Screen Space - Camera or World Space, convert to local position
                Vector3 screenPos = mousePos + dragOffset;
                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    dragCanvas.transform as RectTransform, 
                    screenPos, 
                    dragCanvas.worldCamera, 
                    out localPos
                );
                rectTransform.anchoredPosition = localPos;
            }
        }
        else
        {
            // Fallback for non-UI elements
            Vector3 screenPos = mousePos + dragOffset;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
            worldPos.z = dragPreviewZOffset;
            dragPreview.transform.position = worldPos;
        }
    }
    
    /// <summary>
    /// Update highlighting of drop zones
    /// </summary>
    void UpdateDropZoneHighlighting()
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        DropZone hoveredZone = GetDropZoneUnderMouse(mousePos);
        
        // Update highlighting
        if (hoveredZone != currentHoveredDropZone)
        {
            // Remove highlight from previous zone
            if (currentHoveredDropZone != null)
            {
                currentHoveredDropZone.SetHighlight(false);
            }
            
            // Add highlight to new zone
            currentHoveredDropZone = hoveredZone;
            if (currentHoveredDropZone != null)
            {
                bool canDrop = CanDropOnZone(currentHoveredDropZone);
                currentHoveredDropZone.SetHighlight(true, canDrop ? validDropColor : invalidDropColor);
            }
        }
    }
    
    
    /// <summary>
    /// Get the drop zone under the mouse position
    /// </summary>
    DropZone GetDropZoneUnderMouse(Vector3 mousePosition)
    {
        // Use raycast to find UI elements under mouse
        GraphicRaycaster raycaster = dragCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null) 
        {
            return null;
        }
        
        if (EventSystem.current == null)
        {
            return null;
        }
        
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = mousePosition;
        
        List<RaycastResult> results = new List<RaycastResult>();
        raycaster.Raycast(eventData, results);
        
        foreach (RaycastResult result in results)
        {
            DropZone dropZone = result.gameObject.GetComponent<DropZone>();
            if (dropZone != null)
            {
                return dropZone;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if the current item can be dropped on the specified zone
    /// </summary>
    bool CanDropOnZone(DropZone dropZone)
    {
        if (currentDraggedItem == null || dropZone == null) return false;
        
        // Check if the drop zone accepts this item type
        return dropZone.CanAcceptItem(currentDraggedItem.item);
    }
    
    /// <summary>
    /// Perform the actual drop operation
    /// </summary>
    void PerformDrop(DraggableItem draggedItem, DropZone targetZone)
    {
        if (draggedItem == null || targetZone == null) return;
        
        
        GearItem item = draggedItem.item;
        int fromSlot = draggedItem.slotIndex;
        bool fromInventory = draggedItem.isInventorySlot;
        
        // Handle different drop scenarios
        if (targetZone.isInventorySlot)
        {
            // Dropping on inventory slot
            if (fromInventory)
            {
                // Moving within inventory
                MoveItemInInventory(fromSlot, targetZone.slotIndex);
            }
            else
            {
                // Unequipping to inventory
                UnequipToInventory(item, fromSlot, targetZone.slotIndex);
            }
        }
        else
        {
            // Dropping on equipment slot
            if (fromInventory)
            {
                // Equipping from inventory
                EquipFromInventory(item, fromSlot, targetZone.slotIndex);
            }
            else
            {
                // Moving between equipment slots
                MoveItemInEquipment(fromSlot, targetZone.slotIndex);
            }
        }
    }
    
    /// <summary>
    /// Move item within inventory
    /// </summary>
    void MoveItemInInventory(int fromSlot, int toSlot)
    {
        if (inventory == null) return;
        
        GearItem item = inventory.GetItemAt(fromSlot);
        GearItem targetItem = inventory.GetItemAt(toSlot);
        
        // Swap items
        inventory.items[fromSlot] = targetItem;
        inventory.items[toSlot] = item;
        
        // Trigger events
        OnItemMoved?.Invoke(item, fromSlot, toSlot);
        inventory.OnInventoryChanged?.Invoke();
        
        // Force UI refresh to ensure visual updates
        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            ui.ForceRefreshUI();
        }
    }
    
    
    /// <summary>
    /// Unequip item to inventory slot
    /// </summary>
    void UnequipToInventory(GearItem item, int equipmentSlot, int inventorySlot)
    {
        if (equipment == null || inventory == null) return;
        
        // Get the existing item in the target inventory slot
        GearItem existingItem = inventory.GetItemAt(inventorySlot);
        
        // Remove from equipment
        equipment.UnequipItemFromSlot(equipmentSlot);
        
        // Put the equipped item in the inventory slot
        inventory.items[inventorySlot] = item;
        
        // If there was an item in the target slot, try to equip it to the SAME equipment slot
        if (existingItem != null)
        {
            // Check if the existing item can be equipped to this slot type
            GearType slotType = equipment.GetGearTypeFromSlot(equipmentSlot);
            if (existingItem.gearType == slotType)
            {
                // Directly equip to the specific slot without removing from inventory first
                // (since we're doing a direct swap)
                GearItem oldItem = equipment.equipmentSlots[equipmentSlot];
                equipment.equipmentSlots[equipmentSlot] = existingItem;
                
                // Apply stat bonuses
                equipment.ApplyGearStats();
                
                // Trigger events
                equipment.OnItemEquipped?.Invoke(existingItem, existingItem.gearType);
                equipment.OnEquipmentChanged?.Invoke();
                
                Debug.Log($"[DRAG DROP] Swapped {item.itemName} with {existingItem.itemName} in equipment slot {equipmentSlot}");
            }
            else
            {
                // If gear type doesn't match, add it back to inventory
                inventory.AddItem(existingItem);
                Debug.Log($"[DRAG DROP] Could not equip {existingItem.itemName} (type: {existingItem.gearType}) to equipment slot {equipmentSlot} (type: {slotType}), added back to inventory");
            }
        }
        
        // Trigger events
        OnItemUnequipped?.Invoke(item, inventorySlot);
        equipment.OnEquipmentChanged?.Invoke();
        inventory.OnInventoryChanged?.Invoke();
        
        // Force immediate UI refresh
        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            ui.ForceRefreshUI();
        }
    }
    
    /// <summary>
    /// Equip item from inventory to equipment slot
    /// </summary>
    void EquipFromInventory(GearItem item, int inventorySlot, int equipmentSlot)
    {
        if (inventory == null || equipment == null) return;
        
        // Use the new EquipItemToSlot method for specific slot targeting
        bool success = equipment.EquipItemToSlot(item, equipmentSlot);
        
        if (success)
        {
            // Trigger events
            OnItemEquipped?.Invoke(item, equipmentSlot);
            equipment.OnEquipmentChanged?.Invoke();
            inventory.OnInventoryChanged?.Invoke();
            
            // Force immediate UI refresh and ensure correct equipment reference
            var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
            foreach (var ui in inventoryUIs)
            {
                ui.ForceRefreshEquipmentUI();
                // Also ensure the UI is referencing the correct Equipment component
                ui.Invoke(nameof(ui.FindCorrectEquipmentReference), 0.1f);
            }
        }
        else
        {
            // If equipping failed, put the item back in the inventory
            inventory.items[inventorySlot] = item;
        }
    }
    
    /// <summary>
    /// Move item between equipment slots
    /// </summary>
    void MoveItemInEquipment(int fromSlot, int toSlot)
    {
        if (equipment == null) return;
        
        GearItem item = equipment.GetEquippedItemFromSlot(fromSlot);
        GearItem targetItem = equipment.GetEquippedItemFromSlot(toSlot);
        
        // Remove stat bonuses from both items first
        if (item != null)
        {
            equipment.RemoveGearStats(item);
        }
        if (targetItem != null)
        {
            equipment.RemoveGearStats(targetItem);
        }
        
        // Swap items
        equipment.equipmentSlots[fromSlot] = targetItem;
        equipment.equipmentSlots[toSlot] = item;
        
        // Reapply stat bonuses for all equipped items
        equipment.ApplyGearStats();
        
        // Trigger events
        OnItemMoved?.Invoke(item, fromSlot, toSlot);
        equipment.OnEquipmentChanged?.Invoke();
        
        // Force UI update
        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            ui.ForceRefreshEquipmentUI();
            ui.Invoke(nameof(ui.FindCorrectEquipmentReference), 0.1f);
        }
    }
    
    /// <summary>
    /// Return item to its original position
    /// </summary>
    void ReturnItemToOriginalPosition()
    {
        if (currentDraggedItem != null)
        {
            currentDraggedItem.SetDragging(false);
        }
    }
    
    /// <summary>
    /// Clean up drag state
    /// </summary>
    void CleanupDrag()
    {
        isDragging = false;
        currentDraggedItem = null;
        
        // Remove highlight from current drop zone
        if (currentHoveredDropZone != null)
        {
            currentHoveredDropZone.SetHighlight(false);
            currentHoveredDropZone = null;
        }
        
        // Destroy drag preview
        if (dragPreview != null)
        {
            Destroy(dragPreview);
            dragPreview = null;
        }
    }
    
    
    /// <summary>
    /// Update the inventory reference to match the LootManager's inventory
    /// </summary>
    public void UpdateInventoryReference(Inventory newInventory)
    {
        if (inventory != newInventory)
        {
            inventory = newInventory;
        }
    }
    
    
    
}
