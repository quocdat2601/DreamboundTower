using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Slots")]
    public List<Image> inventorySlots = new List<Image>(); // 20 slots for inventory
    public List<Image> equipmentSlots = new List<Image>(); // 8 slots for equipment
    
    [Header("References")]
    public Inventory inventory;
    public Equipment equipment;
    public GearManager gearManager;
    
    [Header("UI Prefabs")]
    public GameObject itemSlotPrefab; // Prefab for item slots with icon
    
    private List<GameObject> inventorySlotObjects = new List<GameObject>();
    private List<GameObject> equipmentSlotObjects = new List<GameObject>();
    
    void Start()
    {
        // Get components if not assigned
        if (inventory == null)
            inventory = FindFirstObjectByType<Inventory>();
        if (equipment == null)
            equipment = FindFirstObjectByType<Equipment>();
        if (gearManager == null)
            gearManager = FindFirstObjectByType<GearManager>();
        
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
    }
    
    void InitializeInventorySlots()
    {
        // Create slot objects for inventory (20 slots)
        for (int i = 0; i < 20; i++)
        {
            if (i < inventorySlots.Count)
            {
                GameObject slotObj = CreateSlotObject(inventorySlots[i].transform, i, true);
                inventorySlotObjects.Add(slotObj);
            }
        }
    }
    
    void InitializeEquipmentSlots()
    {
        // Create slot objects for equipment (8 slots)
        for (int i = 0; i < 8; i++)
        {
            if (i < equipmentSlots.Count)
            {
                GameObject slotObj = CreateSlotObject(equipmentSlots[i].transform, i, false);
                equipmentSlotObjects.Add(slotObj);
            }
        }
    }
    
    GameObject CreateSlotObject(Transform parent, int slotIndex, bool isInventory)
    {
        GameObject slotObj = new GameObject($"Slot_{slotIndex}");
        slotObj.transform.SetParent(parent, false);
        
        // Add Image component for item icon
        Image itemIcon = slotObj.AddComponent<Image>();
        itemIcon.color = Color.clear; // Invisible by default
        
        // Add Button component for clicking
        Button button = slotObj.AddComponent<Button>();
        button.targetGraphic = itemIcon;
        
        // Add click listener
        int index = slotIndex; // Capture for closure
        if (isInventory)
        {
            button.onClick.AddListener(() => OnInventorySlotClicked(index));
        }
        else
        {
            button.onClick.AddListener(() => OnEquipmentSlotClicked(index));
        }
        
        // Make it fill the parent
        RectTransform rect = slotObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        
        return slotObj;
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
        
        // Map equipment slot index to gear type
        GearType gearType = equipment.GetGearTypeFromSlot(slotIndex);
        GearItem item = equipment.GetEquippedItem(gearType);
        
        if (item != null)
        {
            
            // Unequip the item
            equipment.UnequipItem(gearType);
        }
    }
    
    
    void UpdateInventoryUI()
    {
        if (inventory == null) return;
        
        for (int i = 0; i < Mathf.Min(20, inventory.items.Count); i++)
        {
            UpdateInventorySlot(i, inventory.items[i]);
        }
    }
    
    void UpdateEquipmentUI()
    {
        if (equipment == null) return;
        
        // Update equipment slots based on equipped items
        for (int i = 0; i < 8; i++)
        {
            GearType gearType = equipment.GetGearTypeFromSlot(i);
            GearItem item = equipment.GetEquippedItem(gearType);
            UpdateEquipmentSlot(i, item);
        }
    }
    
    void UpdateInventorySlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= inventorySlotObjects.Count) return;
        
        GameObject slotObj = inventorySlotObjects[slotIndex];
        Image itemIcon = slotObj.GetComponent<Image>();
        
        if (item != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.color = Color.white;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
        }
    }
    
    void UpdateEquipmentSlot(int slotIndex, GearItem item)
    {
        if (slotIndex >= equipmentSlotObjects.Count) return;
        
        GameObject slotObj = equipmentSlotObjects[slotIndex];
        Image itemIcon = slotObj.GetComponent<Image>();
        
        if (item != null)
        {
            itemIcon.sprite = item.icon;
            itemIcon.color = Color.white;
        }
        else
        {
            itemIcon.sprite = null;
            itemIcon.color = Color.clear;
        }
    }
    
    void UpdateAllUI()
    {
        UpdateInventoryUI();
        UpdateEquipmentUI();
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
