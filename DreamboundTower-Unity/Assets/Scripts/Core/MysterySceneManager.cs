using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Presets;
using TMPro;

public class MysterySceneManager : MonoBehaviour
{
    [Header("Scene References")]
    [Tooltip("Animator của đối tượng Rương (có trigger 'Open')")]
    public Animator chestAnimator;
    [Tooltip("Nút 'Mở Rương'")]
    public Button openChestButton;

    [Header("Reward Config")]
    [Tooltip("Danh sách tất cả các 'luật chơi' cho rương (kéo các file TreasureTier SO vào đây)")]
    public List<TreasureTierConfigSO> allTreasureTiers;
    [Tooltip("Tỷ lệ gặp Mimic, 0.1 = 10%")]
    [Range(0f, 1f)]
    public float mimicChance = 0.1f;

    [Header("Mimic Config")]
    [Tooltip("Kéo file EnemyTemplateSO của Mimic (Mimic.asset) vào đây")]
    public EnemyTemplateSO mimicTemplate;

    [Header("Reward Panel UI")]
    public GameObject rewardPanel;

    [Tooltip("Kéo Panel có 'Grid Layout Group' vào đây")]
    public Transform rewardContainer; // Nơi chứa các slot item VÀ CoinObject

    // ✅ THÊM BIẾN NÀY
    [Tooltip("Kéo đối tượng 'CoinObject' (con của RewardContainer) vào đây")]
    public GameObject coinObject;

    [Tooltip("Kéo Prefab 'ItemSlot' (giống của Shop) vào đây")]
    public GameObject itemSlotPrefab;

    public Button leaveButton;

    private RunData currentRunData;
    private int currentAbsoluteFloor;

    void Start()
    {
        // 1. Ẩn/Hiện các UI cần thiết khi bắt đầu
        rewardPanel.SetActive(false);
        openChestButton.gameObject.SetActive(false);
        if (coinObject != null)
        {
            coinObject.SetActive(false); // ✅ ẨN CoinObject KHI BẮT ĐẦU
        }

        // 2. Lấy dữ liệu game
        if (GameManager.Instance == null)
        {
            Debug.LogError("LỖI: Không tìm thấy GameManager! MysteryScene không thể hoạt động.");
            return;
        }
        currentRunData = GameManager.Instance.currentRunData;
        currentAbsoluteFloor = (currentRunData.mapData.currentZone - 1) * 10 + currentRunData.mapData.currentFloorInZone;

        // 3. Roll xúc xắc xem có phải Mimic không
        if (Random.value < mimicChance)
        {
            // Gặp Mimic!
            SetupMimicEncounter();
        }
        else
        {
            // Rương thường
            SetupRewardChest();
        }
    }

    // Thiết lập cho trường hợp gặp Mimic
    private void SetupMimicEncounter()
    {
        Debug.LogWarning("Một cảm giác nguy hiểm... Đây là Mimic!");
        openChestButton.gameObject.SetActive(true);
        // Khi bấm nút "Mở Rương", nó sẽ kích hoạt trận chiến
        openChestButton.onClick.AddListener(StartMimicBattle);
    }

    // Thiết lập cho trường hợp rương thường
    private void SetupRewardChest()
    {
        openChestButton.gameObject.SetActive(true);
        openChestButton.onClick.AddListener(OnOpenChestClicked);
    }

    private void StartMimicBattle()
    {
        openChestButton.interactable = false;
        if (mimicTemplate == null)
        {
            Debug.LogError("LỖI: Chưa gán 'Mimic Template' vào MysterySceneManager!");
            return;
        }

        // Kích hoạt animation "Open" (hoặc "Transform")
        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("Open");
        }

        // Bắt đầu Coroutine để trì hoãn việc chuyển scene
        StartCoroutine(LoadMimicBattleRoutine(1.0f)); // Trì hoãn 1 giây (bằng với rương thường)
    }

    private IEnumerator LoadMimicBattleRoutine(float delay)
    {
        // Đợi cho animation chạy
        yield return new WaitForSeconds(delay);

        // Các logic cũ của StartMimicBattle được chuyển vào đây
        Debug.LogWarning("Nó là một con Mimic! Chuẩn bị chiến đấu!");

        currentRunData.mapData.pendingEnemyArchetypeId = mimicTemplate.name;
        currentRunData.mapData.pendingEnemyKind = (int)mimicTemplate.kind;
        currentRunData.mapData.pendingEnemyFloor = currentAbsoluteFloor;

        RunSaveService.SaveRun(currentRunData);
        SceneManager.LoadScene("MainGame");
    }

    // Được gọi khi bấm nút và là rương thường
    private void OnOpenChestClicked()
    {
        openChestButton.interactable = false;

        if (chestAnimator != null)
        {
            chestAnimator.SetTrigger("Open");
        }

        StartCoroutine(ShowRewardsRoutine(1.0f));
    }

    // Coroutine chờ animation
    private IEnumerator ShowRewardsRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Tính toán và trao thưởng
        CalculateAndGiveRewards();

        // Hiển thị panel phần thưởng
        rewardPanel.SetActive(true);
        leaveButton.onClick.AddListener(OnLeave);
    }

    // Tính toán và trao phần thưởng cho người chơi
    private void CalculateAndGiveRewards()
    {
        TreasureTierConfigSO tier = GetTreasureTierForFloor(currentAbsoluteFloor);
        if (tier == null)
        {
            Debug.LogError("Không tìm thấy Treasure Tier cho tầng " + currentAbsoluteFloor);
            return;
        }

        // Dọn dẹp reward container (chỉ xóa item, không xóa goldText)
        foreach (Transform child in rewardContainer)
        {
            if (child.gameObject != coinObject) // Chỉ xóa nếu KHÔNG PHẢI là CoinObject
            {
                Destroy(child.gameObject);
            }
        }

        // 1. Roll và Hiển thị Vàng
        int goldWon = Random.Range(tier.minGold, tier.maxGold + 1);
        currentRunData.playerData.gold += goldWon;
        if (coinObject != null)
        {
            // Tìm Text "CoinValue" bên trong CoinObject
            TextMeshProUGUI coinValueText = coinObject.GetComponentInChildren<TextMeshProUGUI>();
            if (coinValueText != null)
            {
                coinValueText.text = $"+{goldWon}";
            }
            coinObject.SetActive(true); // ✅ KÍCH HOẠT CoinObject
        }

        // 2. Roll và Tạo Prefab Item
        for (int i = 0; i < tier.numberOfItems; i++)
        {
            ItemRarity rarity = GetRandomRarityBasedOnWeight(tier.rarityWeights);
            List<GearItem> availableItems = GameManager.Instance.allItems
                .Where(item => item.rarity == rarity).ToList();

            if (availableItems.Count > 0)
            {
                GearItem itemWon = availableItems[Random.Range(0, availableItems.Count)];
                
                // Thêm item vào inventory
                if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
                {
                    var inventory = GameManager.Instance.playerInstance.GetComponent<Inventory>();
                    if (inventory != null)
                    {
                        bool added = inventory.AddItem(itemWon);
                        if (added)
                        {
                            Debug.Log($"[MYSTERY] Added {itemWon.itemName} to inventory");
                            
                            // Lưu item vào RunData để persistence
                            string itemId = itemWon.name; // Use ScriptableObject name as ID
                            if (currentRunData != null && !currentRunData.playerData.inventoryItemIds.Contains(itemId))
                            {
                                currentRunData.playerData.inventoryItemIds.Add(itemId);
                            }
                            
                            // Force refresh UI
                            var inventoryUIs = FindObjectsByType<DragDropInventoryUI>(FindObjectsSortMode.None);
                            foreach (var ui in inventoryUIs)
                            {
                                if (ui.inventory != inventory)
                                {
                                    ui.UpdateInventoryReference(inventory);
                                }
                                else
                                {
                                    ui.ForceRefreshUI();
                                }
                            }
                            
                            // Update DragDropSystem reference
                            var dragDropSystem = FindFirstObjectByType<DragDropSystem>();
                            if (dragDropSystem != null)
                            {
                                dragDropSystem.UpdateInventoryReference(inventory);
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[MYSTERY] Inventory full! Cannot add {itemWon.itemName}");
                        }
                    }
                    else
                    {
                        Debug.LogError("[MYSTERY] Inventory component not found on playerInstance!");
                    }
                }
                else
                {
                    Debug.LogError("[MYSTERY] GameManager.Instance or playerInstance is null!");
                }
                
                CreateRewardSlot(itemWon.icon, itemWon, itemWon.itemName); // Gọi hàm tạo slot cho item
            }
        }

        // Cập nhật UI vàng tổng của người chơi (trên HUD bất tử)
        GameManager.Instance.UpdatePlayerGoldUI(currentRunData.playerData.gold);
    }

    private void CreateRewardSlot(Sprite icon, GearItem itemData, string overrideName)
    {
        GameObject slotGO = Instantiate(itemSlotPrefab, rewardContainer);

        Image itemIcon = slotGO.transform.Find("ItemIcon").GetComponent<Image>();
        // (Tùy chọn) Tìm Text bên trong prefab, ví dụ: "ItemNameText"
        // TextMeshProUGUI nameText = slotGO.transform.Find("ItemNameText").GetComponent<TextMeshProUGUI>();

        if (itemIcon != null) itemIcon.sprite = icon;
        // if (nameText != null) nameText.text = overrideName; // Hiển thị "+500 Vàng" hoặc "Kiếm Sắt"

        TooltipTrigger trigger = slotGO.GetComponent<TooltipTrigger>();
        Button slotButton = slotGO.GetComponent<Button>();
        slotButton.interactable = false; // Luôn tắt nút

        if (trigger != null && itemData != null) // Chỉ bật tooltip nếu là item
        {
            trigger.dataToShow = itemData;
            trigger.OnItemHoverEnter.AddListener(TooltipManager.Instance.ShowItemTooltip);
            trigger.OnHoverExit.AddListener(TooltipManager.Instance.HideAllTooltips);
        }
        else if (trigger != null)
        {
            trigger.enabled = false; // Tắt tooltip cho Vàng
        }
    }

    // Được gọi khi bấm nút "Rời đi"
    private void OnLeave()
    {
        leaveButton.interactable = false;
        // Lưu lại vàng/item mới vào file save
        GameManager.Instance.SavePlayerStateToRunData();
        // Quay trở lại map
        string zoneSceneToLoad = "Zone" + currentRunData.mapData.currentZone;
        SceneManager.LoadScene(zoneSceneToLoad);
    }

    // --- CÁC HÀM HỖ TRỢ ---

    private TreasureTierConfigSO GetTreasureTierForFloor(int floor)
    {
        foreach (var tier in allTreasureTiers)
        {
            if (floor >= tier.minAbsoluteFloor && floor <= tier.maxAbsoluteFloor)
                return tier;
        }
        return null;
    }

    // Thuật toán chọn ngẫu nhiên theo trọng số
    private ItemRarity GetRandomRarityBasedOnWeight(List<RarityWeight> weights)
    {
        float totalWeight = weights.Sum(w => w.weight);
        float randomValue = Random.Range(0, totalWeight);
        float currentWeightSum = 0;
        foreach (var weight in weights)
        {
            currentWeightSum += weight.weight;
            if (randomValue <= currentWeightSum)
                return weight.rarity;
        }
        return ItemRarity.Common;
    }

    private void CreateRewardSlot(GearItem itemData)
    {
        if (itemData == null) return;

        GameObject slotGO = Instantiate(itemSlotPrefab, rewardContainer);

        // Apply rarity background color to slot
        Image rootImage = slotGO.GetComponent<Image>();
        if (rootImage == null)
        {
            rootImage = slotGO.AddComponent<Image>();
        }
        RarityColorUtility.ApplyRarityBackground(rootImage, itemData.rarity);
        
        // Also look for a background child object
        Transform bgTransform = slotGO.transform.Find("Background");
        if (bgTransform != null)
        {
            Image bgImage = bgTransform.GetComponent<Image>();
            if (bgImage != null)
            {
                RarityColorUtility.ApplyRarityBackground(bgImage, itemData.rarity);
            }
        }

        // 1. Gán Icon
        Image itemIcon = null;
        Transform iconTransform = slotGO.transform.Find("ItemIcon"); // Tìm đối tượng con

        if (iconTransform != null)
        {
            itemIcon = iconTransform.GetComponent<Image>();
        }

        if (itemIcon != null)
        {
            itemIcon.sprite = itemData.icon;
        }
        else
        {
            Debug.LogWarning("Không tìm thấy 'ItemIcon' (Image) bên trong itemSlotPrefab!");
        }

        // 2. Kết nối Tooltip (vì chúng ta tái sử dụng prefab của shop)
        TooltipTrigger trigger = slotGO.GetComponent<TooltipTrigger>();
        if (trigger != null)
        {
            trigger.dataToShow = itemData;
            trigger.OnItemHoverEnter.AddListener(TooltipManager.Instance.ShowItemTooltip);
            trigger.OnHoverExit.AddListener(TooltipManager.Instance.HideAllTooltips);
        }

        // 3. Vô hiệu hóa nút bấm (vì đây là phần thưởng, không phải để mua)
        Button slotButton = slotGO.GetComponent<Button>();
        if (slotButton != null)
        {
            slotButton.interactable = false;
        }
    }
}