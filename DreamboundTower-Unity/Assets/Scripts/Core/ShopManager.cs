using Presets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
            // Transparent but raycastable
            var c = rootImage.color; c.a = 0.001f; rootImage.color = c;
        }
        rootImage.raycastTarget = true;
        Image itemIcon = slotGO.transform.Find("ItemIcon").GetComponent<Image>();
        itemIcon.sprite = item.icon;
        // Đảm bảo icon nhận raycast (đây thường là topmost hit)
        itemIcon.raycastTarget = true;

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

    // Helper đảm bảo nhận click kể cả khi Button bị chặn bởi component khác
    private class ShopItemClickHandler : MonoBehaviour, IPointerClickHandler
    {
        public ShopManager manager;
        public GearItem item;
        public Button button;
        public GameObject slotRoot;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (manager == null || item == null) return;
            manager.PurchaseItem(item, button != null ? button : GetComponent<Button>(), slotRoot != null ? slotRoot : gameObject);
        }
    }

    private void PurchaseItem(GearItem item, Button slotButton, GameObject slotRoot)
    {
        if (currentRunData.playerData.gold < item.basePrice) return;

        currentRunData.playerData.gold -= item.basePrice;
        if (currentRunData.playerData.gold < 0) currentRunData.playerData.gold = 0;
        UpdatePlayerGoldUI();

        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var inventory = GameManager.Instance.playerInstance.GetComponent<Inventory>();
            if (inventory != null)
            {
                inventory.AddItem(item);
            }
        }

        // Disable slot interactions and clear visuals so it cannot be bought again
        if (slotRoot != null)
        {
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
        }
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