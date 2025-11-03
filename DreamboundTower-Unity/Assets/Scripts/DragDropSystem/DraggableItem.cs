using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Component that makes an inventory item draggable
/// 
/// USAGE:
/// - Attach to inventory/equipment slot UI elements
/// - Handles drag events and right-click equip/unequip
/// - Communicates with DragDropSystem for item operations
/// 
/// SETUP:
/// 1. Attach to slot UI GameObjects
/// 2. Assign DragDropSystem reference (auto-found if not set)
/// 3. Set item data via SetItemData() method
/// </summary>
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Item Data")]
    [Tooltip("The item this draggable represents")]
    public GearItem item;
    
    [Tooltip("The slot index this item is in")]
    public int slotIndex;
    
    [Tooltip("Whether this is an inventory slot (true) or equipment slot (false)")]
    public bool isInventorySlot = true;
    
    [Header("References")]
    [Tooltip("Drag and drop system reference")]
    public DragDropSystem dragDropSystem;
    
    
    // Components
    private Image itemImage;
    private Button itemButton;
    private RectTransform rectTransform;
    
    // Drag state
    private bool isDragging = false;
    private Vector3 originalScale;
    private Color originalColor;
    private float originalAlpha;
    
    void Awake()
    {
        // Get components - try to find Image component on this object or its children
        itemImage = GetComponent<Image>();
        if (itemImage == null)
        {
            itemImage = GetComponentInChildren<Image>();
        }
        
        itemButton = GetComponent<Button>();
        if (itemButton == null)
        {
            itemButton = GetComponentInChildren<Button>();
        }
        
        rectTransform = GetComponent<RectTransform>();
        
        // Store original values
        if (itemImage != null)
        {
            originalColor = itemImage.color;
            originalAlpha = originalColor.a;
        }
        originalScale = transform.localScale;
        
        // Try to find DragDropSystem if not assigned in inspector
        if (dragDropSystem == null)
        {
            dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
    }
    
    void Start()
    {
        // If no drag drop system found, try again later
        if (dragDropSystem == null)
        {
            Invoke(nameof(FindDragDropSystem), 0.1f);
        }
    }
    
    void FindDragDropSystem()
    {
        // Try to find DragDropSystem if not assigned in inspector
        if (dragDropSystem == null)
        {
            dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        }
    }
    
    /// <summary>
    /// Set the item data for this draggable
    /// </summary>
    /// <param name="gearItem">The gear item</param>
    /// <param name="slot">The slot index</param>
    /// <param name="isInventory">Whether this is an inventory slot</param>
    public void SetItemData(GearItem gearItem, int slot, bool isInventory)
    {
        item = gearItem;
        slotIndex = slot;
        isInventorySlot = isInventory;
        
        // Update visual representation
        UpdateVisuals();
    }
    
    /// <summary>
    /// Update the visual representation of the item
    /// </summary>
    void UpdateVisuals()
    {
        if (itemImage == null) return;
        
        if (item != null && item.icon != null)
        {
            itemImage.sprite = item.icon;
            itemImage.color = Color.white;
            
            // Ensure the item Image is rendered on top (same as in UpdateInventorySlot)
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
    
    /// <summary>
    /// Set the dragging state
    /// </summary>
    /// <param name="dragging">Whether this item is being dragged</param>
    public void SetDragging(bool dragging)
    {
        isDragging = dragging;
        
        if (itemImage == null) return;
        
        if (dragging)
        {
            // Hide the item from the slot during drag
            itemImage.color = Color.clear;
            
            // Disable button during drag
            if (itemButton != null)
            {
                itemButton.interactable = false;
            }
        }
        else
        {
            // Restore original visual state
            transform.localScale = originalScale;
            
            // Restore color - if item exists, use white, otherwise clear
            if (item != null)
            {
                itemImage.color = Color.white;
            }
            else
            {
                itemImage.color = Color.clear;
            }
            
            // Re-enable button
            if (itemButton != null)
            {
                itemButton.interactable = true;
            }
        }
    }
    
    /// <summary>
    /// Handle begin drag event
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (item == null || dragDropSystem == null) return;
        
        // Start drag operation
        dragDropSystem.StartDrag(this, eventData.position);
        
    }
    
    /// <summary>
    /// Handle drag event (continuous)
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        // Drag handling is done by the DragDropSystem
        // This method is required by the interface but we don't need to do anything here
    }
    
    /// <summary>
    /// Handle end drag event
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (dragDropSystem == null) return;
        
        // End drag operation
        dragDropSystem.EndDrag(eventData.position);
        
    }
    
    /// <summary>
    /// Handle click event (for non-drag interactions)
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Only handle clicks if we're not dragging
        if (isDragging || item == null) return;
        
        // Handle different click types
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Right click - context menu or quick action
            HandleRightClick();
        }
    }
    
    
    /// <summary>
    /// Handle right click (equip/unequip or sell in shop)
    /// </summary>
    void HandleRightClick()
    {
        // Check if we're in the shop scene
        if (IsInShopScene())
        {
            // In shop scene: sell the item
            HandleSellItem();
            return;
        }
        
        // Not in shop scene: use normal equip/unequip behavior
        // Find the correct equipment and inventory references
        Equipment correctEquipment = FindCorrectEquipment();
        Inventory correctInventory = FindCorrectInventory();
        
        if (correctEquipment == null || correctInventory == null) return;
        
        if (isInventorySlot)
        {
            // For inventory slots, get the item directly from the inventory slot
            // This ensures we always have the correct item even if the DraggableItem's item field is outdated
            GearItem inventoryItem = correctInventory.GetItemAt(slotIndex);
            if (inventoryItem == null) return;
            
            // Equip the item (this automatically handles swapping and removes from inventory)
            bool success = correctEquipment.EquipItem(inventoryItem);
            if (success)
            {
                // Force UI update
                ForceUIUpdate();
            }
        }
        else
        {
            // For equipment slots, get the item directly from the equipment slot
            // This ensures we always have the correct item even if the DraggableItem's item field is outdated
            GearItem slotItem = correctEquipment.GetEquippedItemFromSlot(slotIndex);
            if (slotItem == null) return;
            
            // Unequip the item
            GearItem unequippedItem = correctEquipment.UnequipItemFromSlot(slotIndex);
            if (unequippedItem != null)
            {
                // Add the unequipped item back to inventory
                correctInventory.AddItem(unequippedItem);
                // Force UI update
                ForceUIUpdate();
            }
        }
    }

    /// <summary>
    /// Check if we're currently in the shop scene
    /// </summary>
    bool IsInShopScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        return currentScene.name == "ShopScene";
    }

    /// <summary>
    /// Handle selling an item in the shop scene
    /// </summary>
    void HandleSellItem()
    {
        // Find ShopManager
        ShopManager shopManager = FindFirstObjectByType<ShopManager>();
        if (shopManager == null) return;

        // Find the correct equipment and inventory references
        Equipment correctEquipment = FindCorrectEquipment();
        Inventory correctInventory = FindCorrectInventory();
        
        if (correctEquipment == null || correctInventory == null) return;

        GearItem itemToSell = null;

        if (isInventorySlot)
        {
            // For inventory slots, get the item directly from the inventory slot
            itemToSell = correctInventory.GetItemAt(slotIndex);
        }
        else
        {
            // For equipment slots, get the item directly from the equipment slot
            itemToSell = correctEquipment.GetEquippedItemFromSlot(slotIndex);
        }

        if (itemToSell == null) return;

        // Sell the item
        bool success = shopManager.SellItem(itemToSell);
        if (success)
        {
            // Force UI update
            ForceUIUpdate();
        }
    }
    
    /// <summary>
    /// Find the correct Equipment component (instantiated player, not prefab)
    /// </summary>
    Equipment FindCorrectEquipment()
    {
        Equipment[] allEquipment = FindObjectsByType<Equipment>(FindObjectsSortMode.None);
        foreach (var equip in allEquipment)
        {
            // Look for instantiated player equipment (not prefab)
            if (equip.gameObject.name.Contains("(Clone)") || equip.gameObject.name.Contains("DontDestroy"))
            {
                return equip;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Find the correct Inventory component (instantiated player, not prefab)
    /// </summary>
    Inventory FindCorrectInventory()
    {
        Inventory[] allInventories = FindObjectsByType<Inventory>(FindObjectsSortMode.None);
        foreach (var inv in allInventories)
        {
            // Look for instantiated player inventory (not prefab)
            if (inv.gameObject.name.Contains("(Clone)") || inv.gameObject.name.Contains("DontDestroy"))
            {
                return inv;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Force UI update after equip/unequip operations
    /// </summary>
    void ForceUIUpdate()
    {
        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            ui.ForceRefreshUI();
            ui.ForceRefreshEquipmentUI();
            ui.Invoke(nameof(ui.FindCorrectEquipmentReference), 0.1f);
        }
    }
    
    /// <summary>
    /// Check if this item can be dragged
    /// </summary>
    public bool CanDrag()
    {
        return item != null && dragDropSystem != null;
    }
}
