using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// A special drop zone that deletes items when they are dropped on it.
/// Attach this to the trash bin button to enable drag-to-delete functionality.
/// </summary>
public class TrashBinZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("References")]
    [Tooltip("Drag and drop system reference (auto-found if not set)")]
    public DragDropSystem dragDropSystem;
    
    [Header("Visual Feedback")]
    [Tooltip("Image component to highlight when hovering with an item")]
    public Image highlightImage;
    
    [Tooltip("Color to highlight when item can be deleted")]
    public Color highlightColor = new Color(1f, 0.2f, 0.2f, 0.5f); // Red tint
    
    [Tooltip("Original color to restore when not hovering")]
    private Color originalColor;
    
    private Image trashImage;
    
    void Awake()
    {
        // Get image component for visual feedback
        trashImage = GetComponent<Image>();
        if (trashImage != null)
        {
            originalColor = trashImage.color;
        }
        
        // If no highlight image specified, use the main image
        if (highlightImage == null)
        {
            highlightImage = trashImage;
        }
        
        // Ensure this GameObject can receive raycasts
        if (trashImage != null)
        {
            trashImage.raycastTarget = true;
        }
    }
    
    void Start()
    {
        // Find drag drop system if not assigned
        if (dragDropSystem == null)
        {
            dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
    }
    
    /// <summary>
    /// Called when a draggable item is dropped on this zone
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (dragDropSystem == null) return;
        
        // Get the currently dragged item from DragDropSystem
        DraggableItem draggedItem = dragDropSystem.GetCurrentDraggedItem();
        
        if (draggedItem == null || draggedItem.item == null)
        {
            Debug.LogWarning("[TRASH BIN] No item to delete!");
            return;
        }
        
        GearItem itemToDelete = draggedItem.item;
        int slotIndex = draggedItem.slotIndex;
        bool fromInventory = draggedItem.isInventorySlot;
        
        Debug.Log($"[TRASH BIN] Deleting item: {itemToDelete.itemName} from {(fromInventory ? "inventory" : "equipment")} slot {slotIndex}");
        
        // Remove item from inventory or equipment
        if (fromInventory)
        {
            // Remove from inventory
            if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
            {
                var inventory = GameManager.Instance.playerInstance.GetComponent<Inventory>();
                if (inventory != null)
                {
                    inventory.RemoveItem(itemToDelete);
                    Debug.Log($"[TRASH BIN] Deleted {itemToDelete.itemName} from inventory");
                }
            }
        }
        else
        {
            // Unequip item (this removes it from equipment and applies stat changes)
            if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
            {
                var equipment = GameManager.Instance.playerInstance.GetComponent<Equipment>();
                if (equipment != null)
                {
                    equipment.UnequipItemFromSlot(slotIndex);
                    Debug.Log($"[TRASH BIN] Deleted {itemToDelete.itemName} from equipment slot {slotIndex}");
                }
            }
        }
        
        // Force UI refresh
        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            ui.ForceRefreshUI();
        }
        
        // Clean up drag state via DragDropSystem (handles SetDragging(false) and other cleanup)
        if (dragDropSystem != null)
        {
            dragDropSystem.CleanupDrag();
        }
    }
    
    /// <summary>
    /// Called when pointer enters this zone (hover effect)
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Only highlight if we're dragging an item
        if (dragDropSystem != null && dragDropSystem.IsDragging())
        {
            if (highlightImage != null)
            {
                highlightImage.color = highlightColor;
            }
        }
    }
    
    /// <summary>
    /// Called when pointer exits this zone
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        // Restore original color
        if (highlightImage != null)
        {
            highlightImage.color = originalColor;
        }
    }
}

