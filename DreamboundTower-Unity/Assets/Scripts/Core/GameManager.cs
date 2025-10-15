using Presets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Namespace này có thể cần hoặc không tùy thuộc vào code của bạn

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")]
    public RunData currentRunData;
    public GameObject playerInstance;

    [Header("Persistent UI")]
    // ✅ CHỈ GIỮ LẠI THAM CHIẾU TRỰC TIẾP NÀY
    public PlayerStatusController playerStatusUI;

    [Header("Prefabs")]
    public GameObject playerPrefab;

    [Header("Asset Databases")]
    public List<RacePresetSO> allRaces;
    public List<ClassPresetSO> allClasses;
    public List<GearItem> allItems;

    private const int HP_UNIT = 10;
    private const int MANA_UNIT = 5;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            if (playerStatusUI != null) Destroy(playerStatusUI.gameObject.transform.root.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ✅ BIẾN CẢ CANVAS CHỨA UI THÀNH BẤT TỬ
        if (playerStatusUI != null)
        {
            DontDestroyOnLoad(playerStatusUI.gameObject.transform.root.gameObject);
        }

        if (SceneManager.GetActiveScene().name == "Initialization")
        {
            SceneManager.LoadScene("MainMenu");
        }

        if (currentRunData == null)
        {
            currentRunData = new RunData();
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (playerInstance != null)
        {
            playerInstance.SetActive(scene.name == "MainGame");
        }

        // ✅ LOGIC HIỂN THỊ UI, GIỜ ĐÂY RẤT ĐƠN GIẢN VÀ ĐÁNG TIN CẬY
        if (playerStatusUI != null)
        {
            bool isGameplayScene = scene.name.StartsWith("Zone") || scene.name == "MainGame" || scene.name == "EventScene";
            playerStatusUI.gameObject.SetActive(isGameplayScene);
        }
    }

    // ✅ CÁC HÀM GIAO TIẾP VỚI UI SỬ DỤNG THAM CHIẾU TRỰC TIẾP
    public void UpdatePlayerHealthUI(int current, int max)
    {
        if (playerStatusUI != null) playerStatusUI.UpdateHealth(current, max);
    }


    // Hàm tìm RacePresetSO dựa trên ID
    public RacePresetSO GetRaceByID(string id)
    {
        return allRaces.Find(race => race.id == id);
    }

    // Hàm tìm ClassPresetSO dựa trên ID
    public ClassPresetSO GetClassByID(string id)
    {
        return allClasses.Find(cls => cls.id == id);
    }

    public GearItem GetItemByID(string id)
    {
        return allItems.Find(item => item.name == id);
    }
    public void InitializePlayerCharacter()
    {
        // Bước 1: Đọc "bản thiết kế" từ currentRunData
        if (playerPrefab == null || currentRunData == null) return;
        string raceId = currentRunData.playerData.selectedRaceId;
        string classId = currentRunData.playerData.selectedClassId;
        if (string.IsNullOrEmpty(raceId) || string.IsNullOrEmpty(classId)) return;
        RacePresetSO raceData = GetRaceByID(raceId);
        ClassPresetSO classData = GetClassByID(classId);
        if (raceData == null || classData == null) return;

        // Bước 2: Bắt đầu "xây nhà" (Instantiate và cấu hình playerInstance)
        if (playerInstance != null) Destroy(playerInstance);
        playerInstance = Instantiate(playerPrefab);
        playerInstance.name = "PlayerCharacter_DontDestroy";
        DontDestroyOnLoad(playerInstance);

        var playerCharacter = playerInstance.GetComponent<Character>();
        var equipment = playerInstance.GetComponent<Equipment>();
        var playerSkills = playerInstance.GetComponent<PlayerSkills>();
        var playerImage = playerInstance.GetComponent<Image>();

        // Bước 3: THIẾT LẬP CHỈ SỐ THEO QUY TRÌNH CHUẨN
        if (playerCharacter != null && playerStatusUI != null)
        {
            // 3.1. GÁN "GIẤY KHAI SINH" (BASE STATS) TỪ RACE SO
            StatBlock baseStats = raceData.baseStats;
            playerCharacter.baseMaxHP = baseStats.HP * HP_UNIT;
            playerCharacter.baseAttackPower = baseStats.STR;
            playerCharacter.baseDefense = baseStats.DEF;
            playerCharacter.baseMana = baseStats.MANA * MANA_UNIT;
            playerCharacter.baseIntelligence = baseStats.INT;
            playerCharacter.baseAgility = baseStats.AGI;

            // 3.2. RESET CHỈ SỐ THỰC CHIẾN VỀ TRẠNG THÁI GỐC
            playerCharacter.ResetToBaseStats();

            // 3.3. CỘNG DỒN CHỈ SỐ TỪ TRANG BỊ (nếu có trong save file)
            equipment.ApplyGearStats(); // Hàm này cần đảm bảo nó đọc trang bị từ RunData

            // 3.4. HỒI ĐẦY MÁU VÀ MANA CHO LẦN ĐẦU TIÊN
            playerCharacter.currentHP = playerCharacter.maxHP;
            playerCharacter.currentMana = playerCharacter.mana;

            // BƯỚC 3.5: ĐĂNG KÝ LẮNG NGHE SỰ KIỆN TỪ NHÂN VẬT
            if (playerCharacter != null && playerStatusUI != null)
            {
                // Dòng này có nghĩa là: "Này playerStatusUI, kể từ bây giờ,
                // mỗi khi sự kiện OnHealthChanged của playerCharacter được phát sóng,
                // hãy tự động chạy hàm UpdateHealth của chính ngươi."
                playerCharacter.OnHealthChanged += playerStatusUI.UpdateHealth;
            }
        }

        // Bước 4: Cấu hình các thành phần khác (Visual, Skills)
        if (playerImage != null)
        {
            Sprite characterSprite = null;
            switch (classData.id)
            {
                case "class_cleric": characterSprite = raceData.clericSprite; break;
                case "class_mage": characterSprite = raceData.mageSprite; break;
                case "class_warrior": characterSprite = raceData.warriorSprite; break;
                case "class_rogue": characterSprite = raceData.rogueSprite; break;
            }
            playerImage.sprite = characterSprite;
        }

        if (playerSkills != null)
        {
            playerSkills.LearnSkills(raceData, classData);
        }

        Debug.Log("Player Character Initialized from RunData!");

        // Bước 5: Lưu trạng thái ban đầu và cập nhật giao diện
        SavePlayerStateToRunData();
        if (playerCharacter != null)
        {
            // THAY ĐỔI NHỎ Ở ĐÂY
            // Thay vì gọi playerCharacter.UpdateHPUI(),
            // ta trực tiếp ra lệnh cho UI cập nhật lần đầu tiên.
            // Điều này rõ ràng và đáng tin cậy hơn.
            playerStatusUI.UpdateHealth(playerCharacter.currentHP, playerCharacter.maxHP);

            Debug.Log($"Player initialized. MaxHP: {playerCharacter.maxHP}. CurrentHP set to full.");
        }   
    }

    public void LoadNextScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // --- PHẦN THÊM MỚI ---
    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // HÀM MỚI CHO NÚT "NEW GAME"
    public void StartNewGame()
    {
        currentRunData = new RunData();
        currentRunData.playerData.steadfastDurability = 3;

        // Quan trọng: Phải gọi SceneManager.LoadScene trước khi InitializePlayerCharacter
        // vì Initialize cần dữ liệu Race/Class được chọn ở scene tiếp theo.
        // Logic khởi tạo HP sẽ được chuyển vào sau khi chọn nhân vật xong.

        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }
        SceneManager.LoadScene("Character Selection");
    }
    public void ContinueGame()
    {
        // 1. Tải "hộp" RunData từ file lưu
        currentRunData = RunSaveService.LoadRun();

        if (currentRunData == null)
        {
            Debug.LogError("Failed to load run data! Starting a new game as a fallback.");
            StartNewGame();
            return;
        }

        // 2. Xây dựng lại "ngôi nhà" (playerInstance) từ "bản thiết kế" (RunData)
        InitializePlayerCharacter();
        LoadStateFromRunData();
        // 3. LOGIC QUAN TRỌNG: KIỂM TRA TRẠNG THÁI "PENDING"
        if (currentRunData.mapData.pendingNodePoint.x != -1 && currentRunData.mapData.pendingNodePoint.y != -1)
        {
            // --- TRƯỜNG HỢP 1: NGƯỜI CHƠI ĐÃ THOÁT GIỮA CHỪNG ---
            Debug.Log($"Pending node detected at {currentRunData.mapData.pendingNodePoint}. Returning player to the action...");

            string sceneToLoad = currentRunData.mapData.pendingNodeSceneName;

            // KIỂM TRA AN TOÀN: Đề phòng trường hợp tên scene bị rỗng
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError($"Pending node was found, but scene name is empty! Fallback to map to avoid crashing.");

                // Xóa trạng thái pending bị lỗi và lưu lại để sửa file save
                currentRunData.mapData.pendingNodePoint = new Vector2Int(-1, -1);
                RunSaveService.SaveRun(currentRunData);

                // Tải lại scene map một cách an toàn
                SceneManager.LoadScene($"Zone{currentRunData.mapData.currentZone}");
            }
            else
            {
                // Tải scene đã được lưu một cách bình thường
                SceneManager.LoadScene(sceneToLoad);
            }
        }
        else
        {
            // --- TRƯỜNG HỢP 2: NGƯỜI CHƠI AN TOÀN TRÊN MAP ---
            string sceneToLoad = $"Zone{currentRunData.mapData.currentZone}";
            Debug.Log($"No pending node. Returning to map scene: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
    public bool HandlePlayerDefeat()
    {
        if (currentRunData == null) return true;
        currentRunData.playerData.steadfastDurability--;
        Debug.Log($"Player was defeated! Steadfast Heart remaining: {currentRunData.playerData.steadfastDurability}");
        if (playerStatusUI != null) playerStatusUI.UpdateSteadfastHeart(currentRunData.playerData.steadfastDurability);
        RunSaveService.SaveRun(currentRunData);
        return currentRunData.playerData.steadfastDurability <= 0;
    }

    // HÀM MỚI: Dùng để hồi lại khi qua checkpoint
    public void RestoreSteadfastHeart()
    {
        if (currentRunData == null) return;
        currentRunData.playerData.steadfastDurability = 3;
        Debug.Log("Checkpoint reached! Steadfast Heart restored.");
        if (playerStatusUI != null) playerStatusUI.UpdateSteadfastHeart(currentRunData.playerData.steadfastDurability);
        RunSaveService.SaveRun(currentRunData);
    }

    public void SavePlayerStateToRunData()
    {
        if (playerInstance == null || currentRunData == null) return;

        var inventory = playerInstance.GetComponent<Inventory>();
        var equipment = playerInstance.GetComponent<Equipment>();
        var character = playerInstance.GetComponent<Character>();

        // Lưu HP/Mana
        currentRunData.playerData.currentHP = character.currentHP;
        currentRunData.playerData.currentMana = character.currentMana;

        // Lưu Inventory (phải đảm bảo đủ số slot)
        currentRunData.playerData.inventoryItemIds.Clear();
        for (int i = 0; i < inventory.maxSlots; i++)
        {
            GearItem item = (i < inventory.items.Count) ? inventory.items[i] : null;
            currentRunData.playerData.inventoryItemIds.Add(item != null ? item.name : "");
        }

        // Lưu Equipment
        currentRunData.playerData.itemIds.Clear();
        for (int i = 0; i < equipment.equipmentSlots.Length; i++)
        {
            GearItem item = equipment.equipmentSlots[i];
            currentRunData.playerData.itemIds.Add(item != null ? item.name : "");
        }

        RunSaveService.SaveRun(currentRunData);
        Debug.Log("Player state saved to RunData.");
    }

    public void LoadStateFromRunData()
    {
        if (playerInstance == null || currentRunData == null) return;

        var inventory = playerInstance.GetComponent<Inventory>();
        var equipment = playerInstance.GetComponent<Equipment>();
        var character = playerInstance.GetComponent<Character>();

        // Tải Inventory
        inventory.items.Clear(); // Xóa sạch trước khi tải
                                 // Lấp đầy bằng các giá trị từ RunData
        foreach (var itemId in currentRunData.playerData.inventoryItemIds)
        {
            inventory.items.Add(string.IsNullOrEmpty(itemId) ? null : GetItemByID(itemId));
        }
        inventory.OnInventoryChanged?.Invoke();

        // Tải Equipment
        for (int i = 0; i < equipment.equipmentSlots.Length; i++)
        {
            // Thêm kiểm tra an toàn
            if (i < currentRunData.playerData.itemIds.Count)
            {
                string itemId = currentRunData.playerData.itemIds[i];
                equipment.equipmentSlots[i] = string.IsNullOrEmpty(itemId) ? null : GetItemByID(itemId);
            }
        }
        equipment.OnEquipmentChanged?.Invoke();

        // Sau khi tải trang bị, yêu cầu tính toán lại chỉ số
        equipment.ApplyGearStats();

        // Tải HP/Mana và cập nhật UI
        character.currentHP = currentRunData.playerData.currentHP;
        character.currentMana = currentRunData.playerData.currentMana;
        character.UpdateHPUI();
    }
}