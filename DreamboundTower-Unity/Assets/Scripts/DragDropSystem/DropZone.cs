using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Component that defines a drop zone for drag and drop operations
/// 
/// USAGE:
/// - Attach to inventory or equipment slot UI elements
/// - Validates if items can be dropped on this zone
/// - Provides visual feedback during drag operations
/// - Handles item type restrictions and slot validation
/// 
/// SETUP:
/// 1. Attach to slot UI GameObjects
/// 2. Set slotIndex and isInventorySlot properties
/// 3. Assign DragDropSystem reference (auto-found if not set)
/// </summary>
public class DropZone : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag and drop system reference")]
    public DragDropSystem dragDropSystem;
    
    [Header("Drop Zone Settings")]
    [Tooltip("The slot index this drop zone represents")]
    public int slotIndex;
    
    [Tooltip("Whether this is an inventory slot (true) or equipment slot (false)")]
    public bool isInventorySlot = true;
    
    [Tooltip("Specific gear types this slot can accept (empty = accepts all)")]
    public List<GearType> acceptedGearTypes = new List<GearType>();
    
    [Tooltip("Whether this slot can accept any item type")]
    public bool acceptAllTypes = true;
    
    [Header("Visual Feedback")]
    [Tooltip("Image component to use for highlighting")]
    public Image highlightImage;
    
    [Tooltip("Default color for the highlight")]
    public Color defaultHighlightColor = Color.white;
    
    [Tooltip("Alpha for highlighting")]
    [Range(0f, 1f)]
    public float highlightAlpha = 0.3f;
    
    // Components
    private Image slotImage;
    private RectTransform rectTransform;
    
    // Highlight state
    private bool isHighlighted = false;
    private Color originalColor;
    private Color currentHighlightColor;
    
    void Awake()
    {
        // Get components
        slotImage = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();
        
        // Store original color
        if (slotImage != null)
        {
            originalColor = slotImage.color;
        }
        
        // If no highlight image specified, use the slot image
        if (highlightImage == null)
        {
            highlightImage = slotImage;
        }
        
        // DragDropSystem should be assigned in inspector
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
    /// Check if this drop zone can accept the specified item
    /// </summary>
    /// <param name="item">The item to check</param>
    /// <returns>True if the item can be dropped here</returns>
    public bool CanAcceptItem(GearItem item)
    {
        if (item == null) return false;
        
        // If accept all types is enabled, accept any item
        if (acceptAllTypes) return true;
        
        // Check if the item's gear type is in the accepted list
        return acceptedGearTypes.Contains(item.gearType);
    }
    
    /// <summary>
    /// Set the highlight state of this drop zone
    /// </summary>
    /// <param name="highlighted">Whether to highlight</param>
    /// <param name="color">Color to use for highlighting (optional)</param>
    public void SetHighlight(bool highlighted, Color? color = null)
    {
        isHighlighted = highlighted;
        
        if (highlightImage == null) return;
        
        if (highlighted)
        {
            // Apply highlight
            currentHighlightColor = color ?? defaultHighlightColor;
            Color highlightColorWithAlpha = currentHighlightColor;
            highlightColorWithAlpha.a = highlightAlpha;
            highlightImage.color = highlightColorWithAlpha;
        }
        else
        {
            // Remove highlight
            highlightImage.color = originalColor;
        }
    }
    
    /// <summary>
    /// Set the accepted gear types for this drop zone
    /// </summary>
    /// <param name="gearTypes">List of gear types to accept</param>
    public void SetAcceptedGearTypes(List<GearType> gearTypes)
    {
        acceptedGearTypes.Clear();
        if (gearTypes != null)
        {
            acceptedGearTypes.AddRange(gearTypes);
        }
        
        // If no specific types set, accept all
        acceptAllTypes = acceptedGearTypes.Count == 0;
    }
    
    /// <summary>
    /// Add a gear type to the accepted list
    /// </summary>
    /// <param name="gearType">The gear type to add</param>
    public void AddAcceptedGearType(GearType gearType)
    {
        if (!acceptedGearTypes.Contains(gearType))
        {
            acceptedGearTypes.Add(gearType);
            acceptAllTypes = false;
        }
    }
    
    /// <summary>
    /// Remove a gear type from the accepted list
    /// </summary>
    /// <param name="gearType">The gear type to remove</param>
    public void RemoveAcceptedGearType(GearType gearType)
    {
        acceptedGearTypes.Remove(gearType);
        acceptAllTypes = acceptedGearTypes.Count == 0;
    }
    
    /// <summary>
    /// Get the slot index
    /// </summary>
    public int GetSlotIndex()
    {
        return slotIndex;
    }
    
    /// <summary>
    /// Check if this is an inventory slot
    /// </summary>
    public bool IsInventorySlot()
    {
        return isInventorySlot;
    }
    
    /// <summary>
    /// Update the slot index
    /// </summary>
    /// <param name="newSlotIndex">The new slot index</param>
    public void UpdateSlotIndex(int newSlotIndex)
    {
        slotIndex = newSlotIndex;
    }
    
    /// <summary>
    /// Get the current item in this slot
    /// </summary>
    public GearItem GetCurrentItem()
    {
        if (isInventorySlot)
        {
            Inventory inventory = FindFirstObjectByType<Inventory>();
            if (inventory != null)
            {
                return inventory.GetItemAt(slotIndex);
            }
        }
        else
        {
            Equipment equipment = FindFirstObjectByType<Equipment>();
            if (equipment != null)
            {
                return equipment.GetEquippedItemFromSlot(slotIndex);
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Check if this slot is empty
    /// </summary>
    public bool IsEmpty()
    {
        return GetCurrentItem() == null;
    }
    
    /// <summary>
    /// Get the gear type this slot is designed for
    /// </summary>
    public GearType GetSlotGearType()
    {
        if (isInventorySlot)
        {
            // Inventory slots can hold any type
            return GearType.Weapon; // Default, but not restrictive
        }
        else
        {
            // Equipment slots have specific types
            Equipment equipment = FindFirstObjectByType<Equipment>();
            if (equipment != null)
            {
                return equipment.GetGearTypeFromSlot(slotIndex);
            }
            return GearType.Weapon; // Fallback
        }
    }
    
    void OnDestroy()
    {
        // Cleanup is handled automatically by the drag drop system's raycasting
    }
}
