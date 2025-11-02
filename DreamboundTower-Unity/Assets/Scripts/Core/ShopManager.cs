using Presets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ShopManager : MonoBehaviour
{
    [Header("Debug & Override")]
    public bool useOverrideFloor = false;
    [Range(1, 100)] public int debugFloor = 1;

    [Header("Shop Configuration")]
    public List<ShopTierConfigSO> allShopTiers;
    public int numberOfItemsToSell = 6;

    [Header("UI References")]
    public Transform stallPanelContainer;
    public GameObject itemSlotPrefab;
    public Button leaveButton;

    [Header("Fallback Player Data")]
    public RacePresetSO fallbackRace;
    public ClassPresetSO fallbackClass;

    private RunData currentRunData;
    void Start()
    {
        StartCoroutine(SetupShop());
    }

    private IEnumerator SetupShop()
    {
        if (GameManager.Instance == null)
        {
            GameObject gameManagerPrefab = Resources.Load<GameObject>("GameManager");
            if (gameManagerPrefab == null) { yield break; }
            Instantiate(gameManagerPrefab);
            yield return null; // wait for Awake
        }

        // Try to use in-memory run data first
        currentRunData = GameManager.Instance.currentRunData;
        if (currentRunData == null)
        {
            // Attempt to load saved run when coming from map
            currentRunData = RunSaveService.LoadRun();
            if (currentRunData != null)
            {
                GameManager.Instance.currentRunData = currentRunData;
                // Ensure player exists for inventory refs
                GameManager.Instance.InitializePlayerCharacter();
            }
            else
            {
                // Final fallback: debug bootstrap for standalone ShopScene testing
                currentRunData = new RunData();
                currentRunData.playerData.gold = 9999;
                currentRunData.playerData.steadfastDurability = 3;
                if (fallbackRace != null && fallbackClass != null)
                {
                    currentRunData.playerData.selectedRaceId = fallbackRace.id;
                    currentRunData.playerData.selectedClassId = fallbackClass.id;
                }
                GameManager.Instance.currentRunData = currentRunData;
                GameManager.Instance.isDebugRun = true;
                GameManager.Instance.InitializePlayerCharacter();
            }
        }
        leaveButton.onClick.AddListener(OnLeave);
        UpdatePlayerGoldUI();
        if (GameManager.Instance.playerStatusUI != null)
        {
            GameManager.Instance.playerStatusUI.UpdateSteadfastHeart(currentRunData.playerData.steadfastDurability);
        }

        // Bind Inventory/Equipment UI to the actual player's components
        SyncInventoryAndEquipmentUI();

        GenerateAndDisplayItems();
    }

    private void SyncInventoryAndEquipmentUI()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerInstance == null) return;
        var playerInv = GameManager.Instance.playerInstance.GetComponent<Inventory>();
        var playerEquip = GameManager.Instance.playerInstance.GetComponent<Equipment>();

        var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
        foreach (var ui in inventoryUIs)
        {
            if (playerInv != null && ui.inventory != playerInv)
            {
                ui.UpdateInventoryReference(playerInv);
            }
            if (playerEquip != null && ui.equipment != playerEquip)
            {
                ui.equipment = playerEquip;
                ui.ForceRefreshEquipmentUI();
            }
            ui.ForceRefreshUI();
        }

        var dragDropSystem = FindFirstObjectByType<DragDropSystem>();
        if (dragDropSystem != null && playerInv != null)
        {
            dragDropSystem.UpdateInventoryReference(playerInv);
        }
    }

    private void GenerateAndDisplayItems()
    {
        foreach (Transform child in stallPanelContainer) { Destroy(child.gameObject); }

        int floorToUse = useOverrideFloor ? debugFloor : GetCurrentAbsoluteFloor();

        ShopTierConfigSO currentTier = GetShopTierForFloor(floorToUse);
        if (currentTier == null) { return; }

        for (int i = 0; i < numberOfItemsToSell; i++)
        {
            ItemRarity selectedRarity = GetRandomRarityBasedOnWeight(currentTier.rarityWeights);

            List<GearItem> availableItems = GameManager.Instance.allItems
                .Where(item => item.rarity == selectedRarity)
                .ToList();

            if (availableItems.Count == 0 && selectedRarity != ItemRarity.Common)
            {
                availableItems = GameManager.Instance.allItems
                    .Where(item => item.rarity == ItemRarity.Common)
                    .ToList();
            }

            if (availableItems.Count > 0)
            {
                GearItem itemToDisplay = availableItems[Random.Range(0, availableItems.Count)];
                Debug.Log($"[SHOP] Generating shop item {i+1}: {itemToDisplay.itemName} (rarity: {itemToDisplay.rarity}, price: {itemToDisplay.basePrice})");
                CreateItemSlot(itemToDisplay);
            }
            else { }
        }
    }

    private void CreateItemSlot(GearItem item)
    {
        GameObject slotGO = Instantiate(itemSlotPrefab, stallPanelContainer);
        // Đảm bảo slot có Button + Graphic để nhận raycast
        Button slotButton = slotGO.GetComponent<Button>();
        if (slotButton == null)
        {
            slotButton = slotGO.AddComponent<Button>();
        }
        Image rootImage = slotGO.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = slotGO.AddComponent<Image>();
            // Set rarity background color
            RarityColorUtility.ApplyRarityBackground(rootImage, item.rarity);
        }
        else
        {
            // Apply rarity background color to existing image
            RarityColorUtility.ApplyRarityBackground(rootImage, item.rarity);
        }
        rootImage.raycastTarget = true;
        
        // Also look for a background child object (common in UI prefabs)
        Transform bgTransform = slotGO.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                RarityColorUtility.ApplyRarityBackground(bgImage, item.rarity);
            }
        }
        
        Transform itemIconTransform = slotGO.transform.Find("ItemIcon");
        if (itemIconTransform == null)
        {
            Debug.LogError($"[SHOP] ItemIcon not found in slot prefab for item: {item.itemName}! Cannot create shop slot.");
            Destroy(slotGO);
            return;
        }
        
        Image itemIcon = itemIconTransform.GetComponent<Image>();
        if (itemIcon == null)
        {
            Debug.LogError($"[SHOP] Image component not found on ItemIcon for item: {item.itemName}! Cannot create shop slot.");
            Destroy(slotGO);
            return;
        }
        
        // Check if item has an icon sprite
        if (item.icon == null)
        {
            Debug.LogWarning($"[SHOP] Item '{item.itemName}' has no icon sprite assigned! It will appear as white/empty.");
        }
        
        itemIcon.sprite = item.icon;
        // Đảm bảo icon nhận raycast (đây thường là topmost hit)
        itemIcon.raycastTarget = true;
        
        // If icon is null, the Image will show as white/empty, but we still want to show the item exists
        if (item.icon == null)
        {
            // Could set a default placeholder sprite here if you have one
            // For now, just log the warning above
        }
        
        // Check if player can afford this item and darken if not enough gold
        bool canAfford = currentRunData != null && currentRunData.playerData.gold >= item.basePrice;
        if (!canAfford)
        {
            // Darken the icon (similar to disabled event choices)
            itemIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.7f); // Gray with reduced opacity
            
            // DO NOT darken the rootImage - keep the rarity background color visible
            // The rarity background should remain visible even if unaffordable
            
            // Disable button interaction but keep it visible
            if (slotButton != null)
            {
                var colors = slotButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
                slotButton.colors = colors;
            }
        }

        // Kết nối Tooltip (nếu có)
        TooltipTrigger trigger = slotGO.GetComponent<TooltipTrigger>();
        if (trigger != null)
        {
            trigger.dataToShow = item;
            trigger.OnItemHoverEnter.AddListener(TooltipManager.Instance.ShowItemTooltip);
            trigger.OnHoverExit.AddListener(TooltipManager.Instance.HideAllTooltips);
        }

        // Vô hiệu hóa mọi DraggableItem/DropZone trong slot vì shop chỉ cần click mua
        var draggables = slotGO.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in draggables)
        {
            if (mb == null) continue;
            var type = mb.GetType().Name;
            if (type == "DraggableItem" || type == "DropZone")
            {
                mb.enabled = false;
            }
        }

        // Only add click handlers if player can afford the item
        if (canAfford)
        {
            // Gắn handler click độc lập với Button để đảm bảo nhận sự kiện (trên root)
            var clickable = slotGO.GetComponent<ShopItemClickHandler>();
            if (clickable == null) clickable = slotGO.AddComponent<ShopItemClickHandler>();
            clickable.manager = this;
            clickable.item = item;
            clickable.button = slotButton;
            clickable.slotRoot = slotGO;

            // Đồng thời gắn handler trực tiếp lên ItemIcon (vì nó là đối tượng được raycast)
            var iconClickable = itemIcon.gameObject.GetComponent<ShopItemClickHandler>();
            if (iconClickable == null) iconClickable = itemIcon.gameObject.AddComponent<ShopItemClickHandler>();
            iconClickable.manager = this;
            iconClickable.item = item;
            iconClickable.button = slotButton;
            iconClickable.slotRoot = slotGO;

            // Cho phép button con (nếu prefab có) cũng gọi mua
            var childButton = slotGO.GetComponentInChildren<Button>(true);
            if (childButton != null)
            {
                childButton.onClick.AddListener(() => PurchaseItem(item, childButton, slotGO));
            }
        }
        else
        {
            // Disable button completely if can't afford
            if (slotButton != null)
            {
                slotButton.interactable = false;
            }
        }
    }

    // Helper đảm bảo nhận click kể cả khi Button bị chặn bởi component khác
    private class ShopItemClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public ShopManager manager;
        public GearItem item;
        public Button button;
        public GameObject slotRoot;

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[SHOP] ShopItemClickHandler.OnPointerClick called for item: {(item != null ? item.itemName : "NULL")}");
            if (manager == null)
            {
                Debug.LogWarning("[SHOP] Manager is null in ShopItemClickHandler!");
                return;
            }
            if (item == null)
            {
                Debug.LogWarning("[SHOP] Item is null in ShopItemClickHandler!");
                return;
            }
            manager.PurchaseItem(item, button != null ? button : GetComponent<Button>(), slotRoot != null ? slotRoot : gameObject);
        }
    }

    private void PurchaseItem(GearItem item, Button slotButton, GameObject slotRoot)
    {
        if (item == null)
        {
            Debug.LogWarning("[SHOP] PurchaseItem called with null item!");
            return;
        }
        
        Debug.Log($"[SHOP] Attempting to purchase: {item.itemName} (price: {item.basePrice}, gold: {currentRunData.playerData.gold})");
        
        if (currentRunData.playerData.gold < item.basePrice)
        {
            Debug.LogWarning($"[SHOP] Not enough gold to purchase {item.itemName}! Need {item.basePrice}, have {currentRunData.playerData.gold}");
            return;
        }

        // Hide tooltip IMMEDIATELY when purchase starts (before any other operations)
        if (TooltipManager.Instance != null)
        {
            TooltipManager.Instance.HideAllTooltips();
            if (TooltipManager.Instance.itemTooltipPanel != null)
            {
                TooltipManager.Instance.itemTooltipPanel.SetActive(false);
            }
        }
        
        currentRunData.playerData.gold -= item.basePrice;
        if (currentRunData.playerData.gold < 0) currentRunData.playerData.gold = 0;
        UpdatePlayerGoldUI();

        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var inventory = GameManager.Instance.playerInstance.GetComponent<Inventory>();
            if (inventory != null)
            {
                bool success = inventory.AddItem(item);
                if (success)
                {
                    Debug.Log($"[SHOP] Successfully added {item.itemName} to inventory");
                }
                else
                {
                    Debug.LogWarning($"[SHOP] Failed to add {item.itemName} to inventory (inventory full?)");
                }
            }
            else
            {
                Debug.LogError("[SHOP] Inventory component not found on player!");
            }
        }
        else
        {
            Debug.LogError("[SHOP] GameManager or playerInstance is null! Cannot add item to inventory.");
        }

        // Clear slot visuals and disable interactions so it cannot be bought again
        if (slotRoot != null)
        {
            // Clear ItemIcon
            var iconTf = slotRoot.transform.Find("ItemIcon");
            if (iconTf != null)
            {
                var img = iconTf.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = null;
                    var c = img.color; c.a = 0f; img.color = c;
                }
                iconTf.gameObject.SetActive(false);
            }
            
            // Immediately hide tooltip and disable tooltip trigger when item is purchased
            var tooltipTrigger = slotRoot.GetComponent<TooltipTrigger>();
            if (tooltipTrigger != null)
            {
                // Disable the trigger FIRST to prevent any hover events
                tooltipTrigger.enabled = false;
                
                // Force exit event to ensure tooltip closes immediately
                if (tooltipTrigger.OnHoverExit != null)
                {
                    tooltipTrigger.OnHoverExit.Invoke();
                }
                
                // Clear all data and listeners
                tooltipTrigger.dataToShow = null;
                tooltipTrigger.OnItemHoverEnter.RemoveAllListeners();
                tooltipTrigger.OnHoverExit.RemoveAllListeners();
            }
            
            // Hide tooltip from TooltipManager (ensure it's hidden)
            if (TooltipManager.Instance != null)
            {
                TooltipManager.Instance.HideAllTooltips();
                
                // Also directly deactivate tooltip panels as backup
                if (TooltipManager.Instance.itemTooltipPanel != null)
                {
                    TooltipManager.Instance.itemTooltipPanel.SetActive(false);
                }
            }
            
            // Reset background color to default (clear rarity color)
            var rootImage = slotRoot.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.color = Color.white;
            }
            
            // Clear background child color if it exists
            var bgTransform = slotRoot.transform.Find("Background");
            if (bgTransform != null)
            {
                var bgImage = bgTransform.GetComponent<Image>();
                if (bgImage != null)
                {
                    bgImage.color = Color.white;
                }
            }
            
            // Clear any text components that might show item name or price
            var textComponents = slotRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var text in textComponents)
            {
                text.text = "";
            }
            var textComponentsLegacy = slotRoot.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var text in textComponentsLegacy)
            {
                text.text = "";
            }
            
            // Disable all interactions
            foreach (var btn in slotRoot.GetComponentsInChildren<Button>(true))
            {
                btn.interactable = false;
            }
            foreach (var h in slotRoot.GetComponentsInChildren<ShopItemClickHandler>(true))
            {
                h.enabled = false;
            }
            foreach (var trig in slotRoot.GetComponentsInChildren<EventTrigger>(true))
            {
                trig.enabled = false;
            }
        }
    }

    /// <summary>
    /// Calculate the sell price for an item (typically 50% of base price)
    /// </summary>
    public int CalculateSellPrice(GearItem item)
    {
        if (item == null) return 0;
        // Standard practice: sell for 50% of base price
        return Mathf.Max(1, item.basePrice / 2);
    }

    /// <summary>
    /// Sell an item from inventory or equipment
    /// </summary>
    public bool SellItem(GearItem item)
    {
        if (item == null || currentRunData == null) return false;

        int sellPrice = CalculateSellPrice(item);

        // Try to remove from inventory first
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var inventory = GameManager.Instance.playerInstance.GetComponent<Inventory>();
            var equipment = GameManager.Instance.playerInstance.GetComponent<Equipment>();

            if (inventory != null)
            {
                // Try to remove from inventory (RemoveItem already checks if item exists)
                if (inventory.RemoveItem(item))
                {
                    // Add gold and update UI
                    currentRunData.playerData.gold += sellPrice;
                    UpdatePlayerGoldUI();

                    // Force UI refresh
                    SyncInventoryAndEquipmentUI();
                    
                    // Update shop item affordability after getting gold
                    UpdateShopItemAffordability();
                    return true;
                }
            }

            // If not in inventory, check equipment
            if (equipment != null)
            {
                // Check all equipment slots
                for (int i = 0; i < equipment.equipmentSlots.Length; i++)
                {
                    if (equipment.equipmentSlots[i] == item)
                    {
                        // Unequip the item
                        GearItem unequippedItem = equipment.UnequipItemFromSlot(i);
                        if (unequippedItem != null)
                        {
                            // Add gold and update UI
                            currentRunData.playerData.gold += sellPrice;
                            UpdatePlayerGoldUI();

                            // Force UI refresh
                            SyncInventoryAndEquipmentUI();
                            
                            // Update shop item affordability after getting gold
                            UpdateShopItemAffordability();
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }

    // --- CÁC HÀM HỖ TRỢ ---
    /// <summary>
    /// Cập nhật UI hiển thị số vàng của người chơi.
    /// </summary>
    private void UpdatePlayerGoldUI()
    {
        if (GameManager.Instance != null && currentRunData != null)
        {
            GameManager.Instance.UpdatePlayerGoldUI(currentRunData.playerData.gold);
        }
    }
    
    /// <summary>
    /// Update affordability state of all shop items based on current gold
    /// </summary>
    private void UpdateShopItemAffordability()
    {
        if (stallPanelContainer == null || currentRunData == null) return;
        
        foreach (Transform child in stallPanelContainer)
        {
            if (child == null) continue;
            
            // Get the item from TooltipTrigger or ShopItemClickHandler
            TooltipTrigger trigger = child.GetComponent<TooltipTrigger>();
            ShopItemClickHandler clickHandler = child.GetComponent<ShopItemClickHandler>();
            
            GearItem item = null;
            if (trigger != null && trigger.dataToShow is GearItem triggerItem)
            {
                item = triggerItem;
            }
            else if (clickHandler != null && clickHandler.item != null)
            {
                item = clickHandler.item;
            }
            
            if (item == null) continue;
            
            // Check if player can afford this item now
            bool canAfford = currentRunData.playerData.gold >= item.basePrice;
            
            // Find ItemIcon and update its appearance
            Transform itemIconTransform = child.Find("ItemIcon");
            if (itemIconTransform != null)
            {
                Image itemIcon = itemIconTransform.GetComponent<Image>();
                if (itemIcon != null)
                {
                    if (canAfford)
                    {
                        // Restore normal color
                        itemIcon.color = Color.white;
                    }
                    else
                    {
                        // Darken the icon
                        itemIcon.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                    }
                }
            }
            
            // Update button interactability
            Button slotButton = child.GetComponent<Button>();
            if (slotButton != null)
            {
                slotButton.interactable = canAfford;
            }
            
            // Enable/disable ShopItemClickHandler based on affordability
            if (canAfford)
            {
                // If item becomes affordable, ensure click handler exists
                if (clickHandler == null)
                {
                    clickHandler = child.GetComponent<ShopItemClickHandler>();
                    if (clickHandler == null)
                    {
                        clickHandler = child.gameObject.AddComponent<ShopItemClickHandler>();
                    }
                    clickHandler.manager = this;
                    clickHandler.item = item;
                    clickHandler.button = slotButton;
                    clickHandler.slotRoot = child.gameObject;
                }
                clickHandler.enabled = true;
            }
            else
            {
                // If item becomes unaffordable, disable handler
                if (clickHandler != null)
                {
                    clickHandler.enabled = false;
                }
            }
            
            // Enable/disable all buttons in children
            foreach (var btn in child.GetComponentsInChildren<Button>(true))
            {
                if (btn != null)
                {
                    btn.interactable = canAfford;
                }
            }
        }
    }

    /// <summary>
    /// Tính toán tầng tuyệt đối của người chơi trong tòa tháp.
    /// </summary>
    private int GetCurrentAbsoluteFloor()
    {
        if (currentRunData == null) return 1;

        int zone = currentRunData.mapData.currentZone;
        int floorInZone = currentRunData.mapData.currentFloorInZone;

        // Giả sử mỗi zone có 10 tầng theo GDD của bạn.
        // Bạn có thể thay đổi số này nếu cần.
        int floorsPerZone = 10;

        // Công thức: (Zone hiện tại - 1) * số tầng mỗi zone + tầng hiện tại trong zone đó
        return (zone - 1) * floorsPerZone + floorInZone;
    }

    /// <summary>
    /// Tìm ShopTierConfigSO phù hợp cho một tầng cụ thể.
    /// </summary>
    private ShopTierConfigSO GetShopTierForFloor(int floor)
    {
        // Duyệt qua tất cả các "bộ luật" của shop
        foreach (var tier in allShopTiers)
        {
            // Nếu tầng hiện tại nằm trong khoảng min-max của tier này, trả về nó
            if (floor >= tier.minAbsoluteFloor && floor <= tier.maxAbsoluteFloor)
            {
                return tier;
            }
        }
        // Nếu không tìm thấy, trả về null (sẽ gây ra lỗi để chúng ta biết)
        return null;
    }

    /// <summary>
    /// Chọn ngẫu nhiên một ItemRarity dựa trên "trọng số" (tỷ lệ).
    /// </summary>
    private ItemRarity GetRandomRarityBasedOnWeight(List<RarityWeight> weights)
    {
        // 1. Tính tổng tất cả các trọng số
        float totalWeight = weights.Sum(w => w.weight);

        // 2. Lấy một số ngẫu nhiên trong khoảng từ 0 đến tổng trọng số
        float randomValue = Random.Range(0, totalWeight);

        float currentWeightSum = 0;

        // 3. Duyệt qua từng độ hiếm
        foreach (var weight in weights)
        {
            // Cộng dồn trọng số vào tổng hiện tại
            currentWeightSum += weight.weight;

            // Nếu số ngẫu nhiên nhỏ hơn hoặc bằng tổng hiện tại,
            // chúng ta đã tìm thấy độ hiếm cần chọn
            if (randomValue <= currentWeightSum)
            {
                return weight.rarity;
            }
        }

        // Fallback: Nếu có lỗi gì đó, luôn trả về Common
        return ItemRarity.Common;
    }

    /// <summary>
    /// Được gọi khi người chơi nhấn nút "Rời đi".
    /// </summary>
    private void OnLeave()
    {
        // Lưu lại trạng thái người chơi (quan trọng nhất là lượng vàng mới)
        GameManager.Instance.SavePlayerStateToRunData();

        // Quay trở lại scene bản đồ của zone hiện tại
        string zoneSceneToLoad = "Zone" + currentRunData.mapData.currentZone;
        SceneManager.LoadScene(zoneSceneToLoad);
    }
}