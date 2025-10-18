using Presets;
using System.Collections;
using System.Collections.Generic;
using System.Linq; // Cần thiết để lọc danh sách item
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [Header("Debug & Override")]
    [Tooltip("Nếu được chọn, sẽ sử dụng 'Debug Floor' thay vì tầng hiện tại của người chơi.")]
    public bool useOverrideFloor = false;
    [Tooltip("Tầng giả lập để test tỷ lệ rớt đồ của shop.")]
    [Range(1, 100)]
    public int debugFloor = 1;

    [Header("Shop Configuration")]
    public List<ShopTierConfigSO> allShopTiers;
    public int numberOfItemsToSell = 6;

    [Header("UI References")]
    [Tooltip("Kéo Panel chứa các item (có Horizontal/Grid Layout Group) vào đây.")]
    public Transform stallPanelContainer;
    [Tooltip("Kéo Prefab của một slot item vào đây.")]
    public GameObject itemSlotPrefab;
    public Button leaveButton;

    [Header("Fallback Player Data")]
    public RacePresetSO fallbackRace;
    public ClassPresetSO fallbackClass;

    private RunData currentRunData;
    void Start()
    {
        // Bắt đầu coroutine để thiết lập shop
        StartCoroutine(SetupShop());
    }

    private IEnumerator SetupShop()
    {
        // --- CƠ CHẾ FALLBACK ĐƯỢC CHUYỂN VÀO COROUTINE ---
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("Không tìm thấy GameManager! Đang tạo một GameManager tạm thời để test...");
            GameObject gameManagerPrefab = Resources.Load<GameObject>("GameManager");
            if (gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);

                // ĐỢI 1 FRAME ĐỂ AWAKE() CỦA GAMEMANAGER CHẠY
                yield return null;

                // --- LOGIC TẠO DỮ LIỆU GIẢ LẬP ĐƯỢC CHUYỂN VÀO ĐÂY ---
                Debug.Log("GameManager tạm thời đã được tạo. Bắt đầu tạo dữ liệu giả lập...");
                currentRunData = new RunData();
                currentRunData.playerData.gold = 9999;
                currentRunData.playerData.steadfastDurability = 3; // Thêm fallback cho Steadfast Heart

                if (fallbackRace != null && fallbackClass != null)
                {
                    currentRunData.playerData.selectedRaceId = fallbackRace.id;
                    currentRunData.playerData.selectedClassId = fallbackClass.id;
                }

                GameManager.Instance.currentRunData = currentRunData;
                // ✅ BÁO HIỆU RẰNG ĐÂY LÀ CHẾ ĐỘ DEBUG
                GameManager.Instance.isDebugRun = true;
                // YÊU CẦU GAMEMANAGER TẠO RA MỘT PLAYERINSTANCE TẠM THỜI
                GameManager.Instance.InitializePlayerCharacter();
            }
            else
            {
                Debug.LogError("Không tìm thấy prefab 'GameManager' trong thư mục 'Resources'!");
                yield break; // Dừng coroutine nếu có lỗi
            }
        }
        else
        {
            // Nếu đã có GameManager, lấy dữ liệu như bình thường
            currentRunData = GameManager.Instance.currentRunData;
        }
        // ------------------------------------

        // Các logic còn lại của hàm Start giờ nằm ở đây
        leaveButton.onClick.AddListener(OnLeave);
        UpdatePlayerGoldUI();

        // Cập nhật cả UI Steadfast Heart
        if (GameManager.Instance.playerStatusUI != null)
        {
            GameManager.Instance.playerStatusUI.UpdateSteadfastHeart(currentRunData.playerData.steadfastDurability);
        }

        GenerateAndDisplayItems();
    }

    private void GenerateAndDisplayItems()
    {
        foreach (Transform child in stallPanelContainer) { Destroy(child.gameObject); }

        int floorToUse = useOverrideFloor ? debugFloor : GetCurrentAbsoluteFloor();
        Debug.Log($"<color=orange>[SHOP] Đang tạo shop cho tầng: {floorToUse}</color>");

        ShopTierConfigSO currentTier = GetShopTierForFloor(floorToUse);
        if (currentTier == null)
        {
            Debug.LogError($"Không tìm thấy Shop Tier nào cho tầng {floorToUse}!");
            return;
        }

        for (int i = 0; i < numberOfItemsToSell; i++)
        {
            ItemRarity selectedRarity = GetRandomRarityBasedOnWeight(currentTier.rarityWeights);
            Debug.Log($"[SHOP] Slot {i + 1}: Tỷ lệ roll ra -> <color=cyan>{selectedRarity}</color>");

            List<GearItem> availableItems = GameManager.Instance.allItems
                .Where(item => item.rarity == selectedRarity)
                .ToList();

            // Nếu không tìm thấy item ở độ hiếm đã roll, hãy thử tìm item Common
            if (availableItems.Count == 0 && selectedRarity != ItemRarity.Common)
            {
                Debug.LogWarning($"Không tìm thấy item {selectedRarity}, thử lại với Common.");
                availableItems = GameManager.Instance.allItems
                    .Where(item => item.rarity == ItemRarity.Common)
                    .ToList();
            }

            if (availableItems.Count > 0)
            {
                GearItem itemToDisplay = availableItems[Random.Range(0, availableItems.Count)];
                CreateItemSlot(itemToDisplay);
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy item nào có độ hiếm {selectedRarity} để bán.");
            }
        }
    }

    private void CreateItemSlot(GearItem item)
    {
        GameObject slotGO = Instantiate(itemSlotPrefab, stallPanelContainer);
        Button slotButton = slotGO.GetComponent<Button>();
        Image itemIcon = slotGO.transform.Find("ItemIcon").GetComponent<Image>();
        itemIcon.sprite = item.icon;

        // --- KẾT NỐI HỆ THỐNG TOOLTIP MỚI ---
        TooltipTrigger trigger = slotGO.GetComponent<TooltipTrigger>();
        if (trigger != null)
        {
            trigger.dataToShow = item;
            // Kết nối các sự kiện của trigger với TooltipManager
            trigger.OnItemHoverEnter.AddListener(TooltipManager.Instance.ShowItemTooltip);
            trigger.OnHoverExit.AddListener(TooltipManager.Instance.HideAllTooltips);
        }
        // ------------------------------------

        slotButton.onClick.AddListener(() => PurchaseItem(item, slotButton));
    }

    private void PurchaseItem(GearItem item, Button slotButton)
    {
        if (currentRunData.playerData.gold >= item.basePrice)
        {
            currentRunData.playerData.gold -= item.basePrice;
            UpdatePlayerGoldUI();

            Debug.Log($"Đã mua {item.itemName}! Item sẽ được thêm vào inventory sau.");
            // Tương lai: Inventory.Instance.AddItem(item);

            slotButton.interactable = false;
            // (Tùy chọn) Hiển thị overlay "Đã bán"
        }
        else
        {
            Debug.Log("Không đủ vàng!");
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