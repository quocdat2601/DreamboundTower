using Presets;
using StatusEffects;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [HideInInspector] // Ẩn khỏi Inspector để tránh nhầm lẫn
    public bool isDebugRun = false; // Cờ này sẽ được bật bởi các script fallback

    [Header("Player Data")]
    public RunData currentRunData;
    public GameObject playerInstance;

    [Header("Debug / Player Overrides")]
    [Tooltip("Race mặc định nếu RunData trống")]
    public RacePresetSO fallbackRace;
    [Tooltip("Class mặc định nếu RunData trống")]
    public ClassPresetSO fallbackClass;
    [Tooltip("Nếu được chọn, sẽ dùng chỉ số tùy chỉnh bên dưới thay vì chỉ số của Race/Class")]
    public bool overridePlayerStats = false;
    public StatBlock customPlayerStats;

    [Header("Persistent UI")]
    // ✅ CHỈ GIỮ LẠI THAM CHIẾU TRỰC TIẾP NÀY
    public PlayerStatusController playerStatusUI;

    [Header("Prefabs")]
    public GameObject playerPrefab;

    [Header("Asset Databases")]
    public List<RacePresetSO> allRaces;
    public List<ClassPresetSO> allClasses;
    public List<GearItem> allItems;
    public List<EnemyTemplateSO> allEnemyTemplates;
    public List<EventDataSO> allEvents;

    [Header("Pause & Settings Panels")]
    public Button pauseButton;
    public GameObject pauseMenuPanel;
    public GameObject blockerPanel;
    public GameObject settingsPanel;
    public GameObject settingsMenuPrefab;
    private GameObject currentSettingsInstance;

    public float lastRunTime = 0f;
    private bool isPaused = false;
    private bool isPausable = false;

    // Cheat system flags
    private bool godModeCheatActive = false;

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

        // Setup PassiveSkillManager
        SetupPassiveSkillManager();

        // Setup StatusEffectManager
        SetupStatusEffectManager();

        // Setup SkillDatabase

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (blockerPanel != null) blockerPanel.SetActive(false);

        // Disable DefeatPanel on PersistentUICanvas on startup
        DisableDefeatPanel();

        if (pauseButton != null)
        {
            pauseButton.onClick.AddListener(TogglePause);
        }

        if (SceneManager.GetActiveScene().name == "Initialization")
        {
            SceneManager.LoadScene("MainMenu");
        }

        if (currentRunData == null)
        {
            currentRunData = new RunData();
            // THÊM VÀO: Khởi tạo HP/Mana khi tạo RunData mới LẦN ĐẦU
            if (overridePlayerStats)
            {
                currentRunData.playerData.currentHP = customPlayerStats.HP * 10; // Giả sử HP_UNIT = 10
                currentRunData.playerData.currentMana = customPlayerStats.MANA * 5; // Giả sử MANA_UNIT = 5
                Debug.LogWarning("[GameManager - Debug] Khởi tạo HP/Mana trong RunData từ Override Stats.");
            }
            else if (fallbackRace != null) // Hoặc khởi tạo từ fallback nếu có
            {
                currentRunData.playerData.currentHP = fallbackRace.baseStats.HP * 10;
                currentRunData.playerData.currentMana = fallbackRace.baseStats.MANA * 5;
                Debug.LogWarning("[GameManager - Debug] Khởi tạo HP/Mana trong RunData từ Fallback Race.");
            }
            else // Giá trị mặc định cuối cùng
            {
                currentRunData.playerData.currentHP = 100;
                currentRunData.playerData.currentMana = 50;
            }
        }
        // THÊM VÀO: Hoặc đảm bảo HP/Mana đầy nếu TẢI RunData cũ khi đang bật override
        else if (overridePlayerStats)
        {
            // Nếu tải RunData cũ nhưng muốn test với override, hồi đầy HP/Mana theo override
            currentRunData.playerData.currentHP = customPlayerStats.HP * 10;
            currentRunData.playerData.currentMana = customPlayerStats.MANA * 5;
            Debug.LogWarning("[GameManager - Debug] Hồi đầy HP/Mana trong RunData đã tải theo Override Stats.");
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void SetupPassiveSkillManager()
    {
        // Check if PassiveSkillManager already exists
        if (FindFirstObjectByType<PassiveSkillManager>() == null)
        {
            // Create a new GameObject with PassiveSkillManager
            GameObject passiveSkillManagerGO = new GameObject("PassiveSkillManager");
            passiveSkillManagerGO.AddComponent<PassiveSkillManager>();

            // Make it persistent across scenes
            DontDestroyOnLoad(passiveSkillManagerGO);

            Debug.Log("[GAMEMANAGER] Created PassiveSkillManager GameObject");
        }
    }

    /// <summary>
    /// Helper method to find and disable DefeatPanel on PersistentUICanvas
    /// </summary>
    private void DisableDefeatPanel()
    {
        // Try to find DefeatPanel through playerStatusUI first
        if (playerStatusUI != null)
        {
            Transform persistentCanvas = playerStatusUI.transform.parent;
            if (persistentCanvas != null)
            {
                Transform defeatPanelTransform = persistentCanvas.Find("DefeatPanel");
                if (defeatPanelTransform != null)
                {
                    defeatPanelTransform.gameObject.SetActive(false);
                    return;
                }
            }
        }

        // Fallback: Search directly in GameManager's children for PersistentUICanvas
        Transform persistentCanvasTransform = transform.Find("PersistentUICanvas");
        if (persistentCanvasTransform != null)
        {
            Transform defeatPanelTransform = persistentCanvasTransform.Find("DefeatPanel");
            if (defeatPanelTransform != null)
            {
                defeatPanelTransform.gameObject.SetActive(false);
                return;
            }
        }

        // Last resort: Search all children recursively
        Transform defeatPanel = FindChildRecursive(transform, "DefeatPanel");
        if (defeatPanel != null)
        {
            defeatPanel.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Recursively search for a child by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
            {
                return child;
            }
            Transform result = FindChildRecursive(child, name);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    void SetupStatusEffectManager()
    {
        // Check if StatusEffectManager already exists
        if (FindFirstObjectByType<StatusEffectManager>() == null)
        {
            // Create a new GameObject with StatusEffectManager
            GameObject statusEffectManagerGO = new GameObject("StatusEffectManager");
            statusEffectManagerGO.AddComponent<StatusEffectManager>();

            // Make it persistent across scenes
            DontDestroyOnLoad(statusEffectManagerGO);

            // StatusEffectManager created
        }
    }


    private void Update()
    {
        if (!isPausable) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettingsMenu();
            }
            else
            {
                TogglePause();
            }
        }

        // Kiểm tra tổ hợp phím (Ví dụ: Ctrl + shift + L cho Legendary)

        // Chúng ta sẽ cho phép cheat ở bất cứ đâu:
        if (Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) &&
            Keyboard.current.lKey.wasPressedThisFrame)
        {
            // Kiểm tra để tránh spam cheat
            if (SceneManager.GetActiveScene().name != "MainGame") // Đảm bảo không đang ở trong Combat
            {
                Debug.LogWarning("--- CHEAT CODE ACTIVATED: LOADING F100 LEGENDARY RUN ---");
                // Gọi Coroutine để xử lý việc tải
                StartCoroutine(LoadCheatRun());
            }
        }
        if (currentRunData != null && !isPaused)
        {
            currentRunData.playerData.totalTimePlayed += Time.unscaledDeltaTime;
        }

        // God Mode cheat (Ctrl + Shift + G)
        if (Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) &&
            Keyboard.current.gKey.wasPressedThisFrame)
        {
            ToggleGodModeCheat();
        }

        // Kill Player cheat (Ctrl + Shift + K)
        if (Keyboard.current != null &&
            (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.rightCtrlKey.isPressed) &&
            (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed) &&
            Keyboard.current.kKey.wasPressedThisFrame)
        {
            KillPlayer();
        }
    }

    #region Setting/Pause
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            Time.timeScale = 0f;
            pauseMenuPanel.SetActive(true);
            if (blockerPanel != null) blockerPanel.SetActive(true);
        }
        else
        {
            ResumeGame(); // Dùng hàm Resume để đảm bảo settings cũng được đóng
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (blockerPanel != null) blockerPanel.SetActive(false);
        // ✅ BỔ SUNG LOGIC DỌN DẸP
        if (currentSettingsInstance != null)
        {
            Destroy(currentSettingsInstance);
        }
    }

    public void OpenSettingsMenu()
    {
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);

        if (currentSettingsInstance == null && settingsMenuPrefab != null)
        {
            currentSettingsInstance = Instantiate(settingsMenuPrefab, settingsPanel.transform);
            // Giả sử nút Close là nút duy nhất hoặc nút đầu tiên trong prefab
            Button closeButton = currentSettingsInstance.GetComponentInChildren<Button>();

            if (closeButton != null)
            {
                // Gán hàm CloseSettingsMenu vào sự kiện onClick của nút này
                closeButton.onClick.AddListener(CloseSettingsMenu);
                Debug.Log("Đã tự động gán hàm Close cho nút trong prefab Settings.");
            }
            else
            {
                Debug.LogWarning("Không tìm thấy Button nào trong settingsMenuPrefab để gán hàm Close.");
            }
        }
    }

    public void CloseSettingsMenu()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

        // ✅ BỔ SUNG LOGIC DỌN DẸP
        if (currentSettingsInstance != null)
        {
            Destroy(currentSettingsInstance);
        }
    }

    // --- CÁC HÀM CHO NÚT BẤM (TÍCH HỢP LOGIC MỚI) ---

    /// <summary>
    /// Quay về Menu chính, xóa dữ liệu run hiện tại.
    /// </summary>
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f; // Luôn luôn reset timescale trước khi chuyển scene
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Chức năng mới: Thử lại từ checkpoint GẦN NHẤT.
    /// Sẽ trừ 1 Steadfast Heart và kiểm tra hết "mạng".
    /// </summary>
    public void RetryAtCheckpoint()
    {
        Time.timeScale = 1f;
        if (currentRunData == null) return;
        if (SceneManager.GetActiveScene().name == "MainGame")
        {
            BattleManager battleManager = FindFirstObjectByType<BattleManager>();
            if (battleManager != null)
            {
                battleManager.RestorePersistentPlayer();
                Debug.Log("[GameManager] Đã gọi RestorePersistentPlayer() từ RetryAtCheckpoint.");
            }
            else
            {
                Debug.LogWarning("[GameManager] Đang ở MainGame nhưng không tìm thấy BattleManager!");
            }
        }
        // 1. Kiểm tra xem CÓ còn "mạng" không (lớn hơn 0)
        if (currentRunData.playerData.steadfastDurability <= 0)
        {
            Debug.LogWarning("RetryAtCheckpoint: Đã hết Steadfast Heart. Hiển thị Defeat Panel.");
            ShowDefeatPanel(); // Hiển thị màn hình thua (theo yêu cầu mới của bạn)
            return;
        }

        // 2. TRỪ 1 "MẠNG"
        currentRunData.playerData.steadfastDurability--;
        if (playerStatusUI != null)
            playerStatusUI.UpdateSteadfastHeart(currentRunData.playerData.steadfastDurability);

        // 3. KIỂM TRA LẠI (theo yêu cầu mới của bạn)
        // Nếu vừa dùng "mạng" cuối cùng
        if (currentRunData.playerData.steadfastDurability <= 0)
        {
            Debug.LogWarning("RetryAtCheckpoint: Vừa dùng Steadfast Heart cuối cùng. Hiển thị Defeat Panel.");
            RunSaveService.SaveRun(currentRunData); // Lưu lại số "mạng" = 0
            ShowDefeatPanel(); // Hiển thị màn hình thua
            return;
        }

        // 4. LƯU LẠI SỐ "MẠNG" VỪA BỊ TRỪ
        int durabilityAfterCost = currentRunData.playerData.steadfastDurability;

        // 5. TẢI LẠI CHECKPOINT (Nếu vẫn còn "mạng")
        if (currentRunData.checkpointPlayerData == null || currentRunData.checkpointPlayerData.currentHP == 0)
        {
            Debug.LogError("RetryAtCheckpoint: checkpointPlayerData bị null hoặc rỗng! Không thể tải checkpoint.");
            QuitToMainMenu(); // Lỗi nghiêm trọng, thoát ra menu
            return;
        }

        // Clone() để đảm bảo chúng không tham chiếu cùng một đối tượng
        currentRunData.playerData = currentRunData.checkpointPlayerData.Clone();

        // 6. PHỤC HỒI LẠI SỐ "MẠNG" ĐÚNG (vì checkpointPlayerData chứa số mạng cũ)
        currentRunData.playerData.steadfastDurability = durabilityAfterCost;

        Debug.Log($"[GameManager] CHECKPOINT LOADED. HP: {currentRunData.playerData.currentHP}, Gold: {currentRunData.playerData.gold}, Hearts: {currentRunData.playerData.steadfastDurability}");

        // 7. Reset map
        currentRunData.mapData.currentMapJson = null;
        currentRunData.mapData.path.Clear();
        currentRunData.mapData.pendingNodePoint = new Vector2Int(-1, -1);
        currentRunData.mapData.pendingNodeSceneName = null;

        // 8. Lưu lại trạng thái (đã tải checkpoint và bị trừ "mạng")
        RunSaveService.SaveRun(currentRunData);

        // 9. Tải lại scene map
        string zoneSceneToLoad = "Zone" + currentRunData.mapData.currentZone;
        SceneManager.LoadScene(zoneSceneToLoad);
    }
    #endregion

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Thêm dòng log này để biết chắc chắn hàm có được gọi không
        Debug.Log($"<color=yellow>OnSceneLoaded: Đã tải scene '{scene.name}'</color>");

        if (playerInstance != null)
        {
            bool shouldBeActive = scene.name == "MainGame" || scene.name == "ShopScene" || scene.name == "RestScene" || scene.name == "EventScene" || scene.name == "MysteryScene" || scene.name.StartsWith("Zone");
            playerInstance.SetActive(shouldBeActive);
        }

        if (playerStatusUI != null)
        {
            bool isGameplayScene = scene.name.StartsWith("Zone") || scene.name == "MainGame"
                                   || scene.name == "EventScene" || scene.name == "RestScene"
                                   || scene.name == "ShopScene" || scene.name == "MysteryScene";

            // Thêm dòng log này để xem kết quả của việc kiểm tra scene
            Debug.Log($"<color=cyan>Is Gameplay Scene? {isGameplayScene}</color>");

            playerStatusUI.gameObject.SetActive(isGameplayScene);

            isPausable = isGameplayScene;
            // Nếu đây là scene gameplay, ra lệnh cho PlayerHUDController tìm và cập nhật
            if (isGameplayScene && PlayerHUDController.Instance != null)
            {
                PlayerHUDController.Instance.FindAndRefresh();
            }
            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(isGameplayScene);
            }

            if (!isGameplayScene && isPaused)
            {
                ResumeGame();
            }
        }
        else
        {
            // Thêm dòng log này để biết nếu playerStatusUI bị null
            Debug.LogError("LỖI: playerStatusUI trong GameManager đang bị null!");
        }

        // Disable DefeatPanel in MainMenu and map scenes (it should only show in battle scenes)
        if (scene.name == "MainMenu" || scene.name.StartsWith("Zone"))
        {
            DisableDefeatPanel();
        }

        // Load player state (inventory, equipment) from RunData when entering gameplay scenes
        if (playerInstance != null && currentRunData != null)
        {
            if (scene.name == "EventScene" || scene.name == "ShopScene" || scene.name == "RestScene" || scene.name == "MysteryScene" || scene.name == "BattleScene" || scene.name == "MainGame" || scene.name.StartsWith("Zone"))
            {
                LoadStateFromRunData();
                Debug.Log($"[GameManager] Loaded player state from RunData for scene: {scene.name}");
            }
        }

        if (AudioManager.Instance != null)
        {
            // Kiểm tra tên scene để quyết định nhạc
            if (scene.name.StartsWith("Zone")) // Nếu là scene Map (Zone1, Zone2...)
            {
                AudioManager.Instance.PlayRandomMapMusic();
            }
            else if (scene.name == "BattleScene") // <<-- THAY "BattleScene" bằng tên scene Combat của bạn
            {
                AudioManager.Instance.PlayRandomCombatMusic();
            }
            // else if (scene.name == "MainMenu")
            // {
            //     AudioManager.Instance.PlayMainMenuMusic(); // Nếu bạn có hàm/nhạc riêng cho Menu
            // }
            else // Các scene khác (Event, Shop, Rest...) có thể dùng nhạc Map
            {
                AudioManager.Instance.PlayRandomMapMusic(); // Hoặc tạo list nhạc riêng
            }
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
        if (playerPrefab == null) return;

        RacePresetSO raceData = null;
        ClassPresetSO classData = null;

        // Ưu tiên 1: Đọc từ RunData
        if (currentRunData != null && !string.IsNullOrEmpty(currentRunData.playerData.selectedRaceId) && !string.IsNullOrEmpty(currentRunData.playerData.selectedClassId))
        {
            raceData = GetRaceByID(currentRunData.playerData.selectedRaceId);
            classData = GetClassByID(currentRunData.playerData.selectedClassId);
            Debug.Log("Initializing player from RunData.");
        }

        // Ưu tiên 2: Dùng Fallback (nếu RunData trống hoặc không tìm thấy SO)
        if (raceData == null || classData == null)
        {
            if (fallbackRace == null || fallbackClass == null)
            {
                Debug.LogError("InitializePlayerCharacter: RunData is empty AND Fallback Race/Class in GameManager is not set! Cannot create player.");
                return;
            }
            raceData = fallbackRace;
            classData = fallbackClass;
            Debug.LogWarning("InitializePlayerCharacter: Using FALLBACK Race/Class.");

            // Cũng nên khởi tạo RunData nếu chưa có
            if (currentRunData == null) currentRunData = new RunData();
            currentRunData.playerData.selectedRaceId = raceData.id;
            currentRunData.playerData.selectedClassId = classData.id;
            currentRunData.playerData.steadfastDurability = 3; // Khởi tạo giá trị fallback
            currentRunData.playerData.gold = 50; // Ví dụ
        }

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
            if (overridePlayerStats)
            {
                // Dùng chỉ số override

                // Initialize PlayerData.currentStats to override stats if uninitialized (all zeros)
                if (currentRunData.playerData.currentStats.STR == 0 && currentRunData.playerData.currentStats.DEF == 0 &&
                     currentRunData.playerData.currentStats.INT == 0 && currentRunData.playerData.currentStats.MANA == 0 &&
                     currentRunData.playerData.currentStats.AGI == 0)
                {
                    currentRunData.playerData.currentStats = new StatBlock
                    {
                        HP = customPlayerStats.HP,
                        STR = customPlayerStats.STR,
                        DEF = customPlayerStats.DEF,
                        INT = customPlayerStats.INT,
                        MANA = customPlayerStats.MANA,
                        AGI = customPlayerStats.AGI
                    };
                    Debug.Log("[GameManager] Initialized PlayerData.currentStats from override stats.");
                }

                // Set Character base stats from PlayerData.currentStats (includes permanent gains from events)
                StatBlock playerStats = currentRunData.playerData.currentStats;
                playerCharacter.baseMaxHP = playerStats.HP * HP_UNIT;
                playerCharacter.baseAttackPower = playerStats.STR;
                playerCharacter.baseDefense = playerStats.DEF;
                playerCharacter.baseMana = playerStats.MANA * MANA_UNIT;
                playerCharacter.baseIntelligence = playerStats.INT;
                playerCharacter.baseAgility = playerStats.AGI;
                Debug.LogWarning("[GameManager] ĐÃ SỬ DỤNG CHỈ SỐ OVERRIDE ĐỂ TẠO NHÂN VẬT!");
            }
            else
            {
                // 3.1. GÁN "GIẤY KHAI SINH" (BASE STATS) TỪ RACE SO
                StatBlock baseStats = raceData.baseStats;

                // Initialize PlayerData.currentStats to race stats if uninitialized (all zeros)
                if (currentRunData.playerData.currentStats.STR == 0 && currentRunData.playerData.currentStats.DEF == 0 &&
                     currentRunData.playerData.currentStats.INT == 0 && currentRunData.playerData.currentStats.MANA == 0 &&
                     currentRunData.playerData.currentStats.AGI == 0)
                {
                    // First time: initialize from race
                    currentRunData.playerData.currentStats = new StatBlock
                    {
                        HP = baseStats.HP,
                        STR = baseStats.STR,
                        DEF = baseStats.DEF,
                        INT = baseStats.INT,
                        MANA = baseStats.MANA,
                        AGI = baseStats.AGI
                    };
                    Debug.Log("[GameManager] Initialized PlayerData.currentStats from race stats.");
                }

                // Set Character base stats from PlayerData.currentStats (includes permanent gains from events)
                StatBlock playerStats = currentRunData.playerData.currentStats;
                playerCharacter.baseMaxHP = playerStats.HP * HP_UNIT;
                playerCharacter.baseAttackPower = playerStats.STR;
                playerCharacter.baseDefense = playerStats.DEF;
                playerCharacter.baseMana = playerStats.MANA * MANA_UNIT;
                playerCharacter.baseIntelligence = playerStats.INT;
                playerCharacter.baseAgility = playerStats.AGI;
            }
            // 3.2. RESET CHỈ SỐ THỰC CHIẾN VỀ TRẠNG THÁI GỐC
            playerCharacter.ResetToBaseStats();

            // 3.3. CỘNG DỒN CHỈ SỐ TỪ TRANG BỊ (nếu có trong save file)
            equipment.ApplyGearStats(); // Hàm này cần đảm bảo nó đọc trang bị từ RunData

            // 3.4. HỒI ĐẦY MÁU VÀ MANA CHO LẦN ĐẦU TIÊN
            if (currentRunData.playerData.currentHP <= 0)
            {
                // Đây là lần đầu (New Game), hồi đầy
                playerCharacter.currentHP = playerCharacter.maxHP;
                playerCharacter.currentMana = playerCharacter.mana;

                // --- ✅ QUAN TRỌNG: Cập nhật RunData (trong memory) ---
                // Đồng bộ HP/Mana mới tạo vào RunData
                currentRunData.playerData.currentHP = playerCharacter.currentHP;
                currentRunData.playerData.currentMana = playerCharacter.currentMana;
            }
            else
            {
                // Đây là lần "Continue" hoặc "Retry", tải từ RunData
                playerCharacter.currentHP = currentRunData.playerData.currentHP;
                playerCharacter.currentMana = currentRunData.playerData.currentMana;
            }

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

        // Ensure ConditionalPassiveManager is attached to player character
        var conditionalPassiveManager = playerInstance.GetComponent<ConditionalPassiveManager>();
        if (conditionalPassiveManager == null)
        {
            conditionalPassiveManager = playerInstance.AddComponent<ConditionalPassiveManager>();
        }

        // Apply passive skills after all stat modifications are complete
        var passiveSkillManager = playerInstance.GetComponent<PassiveSkillManager>();
        if (passiveSkillManager != null)
        {
            passiveSkillManager.playerCharacter = playerCharacter;
            passiveSkillManager.playerSkills = playerSkills;
            passiveSkillManager.ApplyAllPassiveSkills();
        }

        Debug.Log("Player Character Initialized from RunData!");

        // Bước 5: Lưu trạng thái ban đầu và cập nhật giao diện
        //SavePlayerStateToRunData();
        if (currentRunData.checkpointPlayerData.currentHP == 0 || currentRunData.mapData.pendingEnemyFloor <= 1)
        {
            SaveCheckpoint();
            Debug.Log("[GameManager] Initial checkpoint (Floor 1) saved.");
        }
        if (playerCharacter != null && playerStatusUI != null)
        {
            playerStatusUI.UpdateHealth(playerCharacter.currentHP, playerCharacter.maxHP);
            // Update Mana UI
            playerStatusUI.UpdateSteadfastHeart(currentRunData.playerData.steadfastDurability);
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

        ////TEST
        //if (currentRunData.currentRunEventFlags == null)
        //{
        //    currentRunData.currentRunEventFlags = new System.Collections.Generic.HashSet<string>();
        //}
        //currentRunData.currentRunEventFlags.Add("RIVAL_HOSTILE");
        //currentRunData.currentRunEventFlags.Add("SHRINE_DESECRATED");
        //Debug.LogWarning("--- TEST HACK: Đã thêm cờ RIVAL_HOSTILE vào currentRunEventFlags! ---");

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
        if (currentRunData == null) return true; // Nếu không có data, coi như run kết thúc

        // Chỉ kiểm tra xem người chơi CÓ CÒN "mạng" hay không.
        // Tuyệt đối KHÔNG có dòng "steadfastDurability--" ở đây.
        bool canRetry = currentRunData.playerData.steadfastDurability > 0;

        Debug.Log($"Player was defeated! Can retry: {canRetry} (Hearts: {currentRunData.playerData.steadfastDurability})");

        // Trả về "runEnded". 
        // Nếu canRetry=true (còn mạng) => runEnded=false (chưa kết thúc).
        // Nếu canRetry=false (hết mạng) => runEnded=true (kết thúc).
        return !canRetry;
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
            string itemNameToSave = item != null ? item.name : "";
            currentRunData.playerData.itemIds.Add(itemNameToSave);
            if (item != null)
            {
                Debug.Log($"[SAVE EQUIPMENT] Slot {i} ({equipment.GetGearTypeFromSlot(i)}): Saving '{item.name}' (itemName: '{item.itemName}')");
            }
            else
            {
                Debug.Log($"[SAVE EQUIPMENT] Slot {i} ({equipment.GetGearTypeFromSlot(i)}): Empty slot, saving empty string");
            }
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
        Debug.Log($"[LOAD EQUIPMENT] Starting equipment load. itemIds.Count = {currentRunData.playerData.itemIds.Count}, equipmentSlots.Length = {equipment.equipmentSlots.Length}");
        for (int i = 0; i < equipment.equipmentSlots.Length; i++)
        {
            GearType expectedSlotType = equipment.GetGearTypeFromSlot(i);
            // Thêm kiểm tra an toàn
            if (i < currentRunData.playerData.itemIds.Count)
            {
                string itemId = currentRunData.playerData.itemIds[i];
                Debug.Log($"[LOAD EQUIPMENT] Slot {i} ({expectedSlotType}): itemId = '{itemId}'");
                if (string.IsNullOrEmpty(itemId))
                {
                    Debug.Log($"[LOAD EQUIPMENT] Slot {i} ({expectedSlotType}): Empty itemId, setting to null");
                    equipment.equipmentSlots[i] = null;
                }
                else
                {
                    GearItem loadedItem = GetItemByID(itemId);
                    // Fallback: Try finding by itemName if GetItemByID fails
                    if (loadedItem == null && allItems != null)
                    {
                        Debug.Log($"[LOAD EQUIPMENT] Slot {i}: GetItemByID('{itemId}') returned null, trying itemName search in allItems (count: {allItems.Count})...");
                        // Check if item exists in allItems but with different name
                        var itemsMatchingItemName = allItems.Where(item => item.itemName == itemId).ToList();
                        if (itemsMatchingItemName.Count > 0)
                        {
                            loadedItem = itemsMatchingItemName[0];
                            Debug.Log($"[LOAD EQUIPMENT] Slot {i}: Found item by itemName: '{loadedItem.itemName}' (name: '{loadedItem.name}')");
                        }
                        else
                        {
                            // Check if item might be in allItems with a different name
                            var itemsWithMatchingItemName = allItems.Where(item => item.itemName != null && item.itemName.Equals(itemId, System.StringComparison.OrdinalIgnoreCase)).ToList();
                            if (itemsWithMatchingItemName.Count > 0)
                            {
                                loadedItem = itemsWithMatchingItemName[0];
                                Debug.Log($"[LOAD EQUIPMENT] Slot {i}: Found item by case-insensitive itemName match: '{loadedItem.itemName}' (name: '{loadedItem.name}')");
                            }
                            else
                            {
                                Debug.LogWarning($"[LOAD EQUIPMENT] Slot {i}: Item '{itemId}' not found in allItems by name or itemName. Check if this item is in GameManager's allItems list.");
                            }
                        }
                    }
                    else if (loadedItem != null)
                    {
                        Debug.Log($"[LOAD EQUIPMENT] Slot {i}: Found item by name: '{loadedItem.name}' (itemName: '{loadedItem.itemName}')");
                    }

                    if (loadedItem == null)
                    {
                        Debug.LogWarning($"[LOAD EQUIPMENT] Slot {i} ({expectedSlotType}): Could not find item with ID '{itemId}'. Searched by name and itemName. Total items in allItems: {allItems?.Count ?? 0}");
                        equipment.equipmentSlots[i] = null;
                    }
                    else
                    {
                        // Validate that the loaded item matches the slot type
                        Debug.Log($"[LOAD EQUIPMENT] Slot {i}: Loaded item '{loadedItem.itemName}' has gearType {loadedItem.gearType}, expected {expectedSlotType}");
                        if (loadedItem.gearType != expectedSlotType)
                        {
                            Debug.LogWarning($"[LOAD EQUIPMENT] Slot {i} ({expectedSlotType}): Item '{loadedItem.itemName}' (type: {loadedItem.gearType}) does not match slot type. Clearing slot.");
                            equipment.equipmentSlots[i] = null;
                        }
                        else
                        {
                            equipment.equipmentSlots[i] = loadedItem;
                            Debug.Log($"[LOAD EQUIPMENT] Slot {i} ({expectedSlotType}): Successfully loaded {loadedItem.itemName} into slot {i}");
                        }
                    }
                }
            }
            else
            {
                // If itemIds list is shorter than equipment slots, set remaining slots to null
                Debug.Log($"[LOAD EQUIPMENT] Slot {i} ({expectedSlotType}): Index {i} is beyond itemIds.Count ({currentRunData.playerData.itemIds.Count}), setting to null");
                equipment.equipmentSlots[i] = null;
            }
        }
        equipment.OnEquipmentChanged?.Invoke();

        // Sync base stats from PlayerData.currentStats (includes race base + permanent gains from events)
        // This ensures that stat gains from events (via GainStat) are reflected in the Character component
        var playerStats = currentRunData.playerData.currentStats;
        var raceData = GetRaceByID(currentRunData.playerData.selectedRaceId);

        if (raceData != null)
        {
            var raceStats = raceData.baseStats;

            // Check if playerStats is initialized (not all zeros)
            bool isPlayerStatsInitialized = !(playerStats.STR == 0 && playerStats.DEF == 0 &&
                                               playerStats.INT == 0 && playerStats.MANA == 0 &&
                                               playerStats.AGI == 0);

            if (isPlayerStatsInitialized)
            {
                // Set Character base stats directly from PlayerData.currentStats (which already includes race + gains)
                character.baseMaxHP = playerStats.HP * HP_UNIT;
                character.baseAttackPower = playerStats.STR;
                character.baseDefense = playerStats.DEF;
                character.baseMana = playerStats.MANA * MANA_UNIT;
                character.baseIntelligence = playerStats.INT;
                character.baseAgility = playerStats.AGI;
            }
            else
            {
                // Fallback: if currentStats is uninitialized, use race stats
                character.baseMaxHP = raceStats.HP * HP_UNIT;
                character.baseAttackPower = raceStats.STR;
                character.baseDefense = raceStats.DEF;
                character.baseMana = raceStats.MANA * MANA_UNIT;
                character.baseIntelligence = raceStats.INT;
                character.baseAgility = raceStats.AGI;
            }

            // IMPORTANT: Get active status effects BEFORE resetting stats
            List<StatusEffect> activeEffects = new List<StatusEffect>();
            if (StatusEffectManager.Instance != null)
            {
                activeEffects = StatusEffectManager.Instance.GetActiveEffects(character);
            }

            // Apply gear stats (this calls ResetToBaseStats internally, then applies gear bonuses)
            // This resets stats to base, so status effects are temporarily removed
            equipment.ApplyGearStats();

            // IMPORTANT: Reapply status effects AFTER gear stats are applied
            // This ensures debuffs/buffs are applied on top of gear bonuses
            if (StatusEffectManager.Instance != null && activeEffects.Count > 0)
            {
                foreach (var effect in activeEffects)
                {
                    // Reapply the effect to restore its stat modifications
                    effect.OnApply(character);
                }
                Debug.Log($"[LoadStateFromRunData] Reapplied {activeEffects.Count} status effects after resetting stats and applying gear.");

                // Update stats UI to show the reapplied effects
                var statsUIManager = FindFirstObjectByType<PlayerInfoUIManager>();
                if (statsUIManager != null && statsUIManager.statsPanel != null && statsUIManager.statsPanel.activeSelf)
                {
                    statsUIManager.UpdateStatsDisplay();
                }
            }
        }
        else
        {
            // If no race data, still need to apply gear stats
            equipment.ApplyGearStats();
        }

        // Tải HP/Mana và cập nhật UI
        character.currentHP = currentRunData.playerData.currentHP;
        character.currentMana = currentRunData.playerData.currentMana;
        character.UpdateHPUI();

        // Cập nhật UI vàng ban đầu sau khi load dữ liệu run
        UpdatePlayerGoldUI(currentRunData.playerData.gold);
    }
    public void UpdatePlayerGoldUI(int amount)
    {
        // Clamp gold to be non-negative for the current run
        int clamped = amount < 0 ? 0 : amount;
        if (currentRunData != null)
        {
            currentRunData.playerData.gold = clamped;
        }
        if (playerStatusUI != null)
        {
            playerStatusUI.UpdateGold(clamped);
        }
    }

    /// <summary>
    /// Coroutine để chuẩn bị và tải trận đấu Boss F100 với full đồ Legendary.
    /// </summary>
    private IEnumerator LoadCheatRun()
    {
        // 1. Đảm bảo có RunData (Giữ nguyên)
        if (currentRunData == null)
        {
            Debug.LogWarning("[Cheat] Không tìm thấy RunData, tạo mới...");
            currentRunData = new RunData();
            currentRunData.playerData.steadfastDurability = 99;
        }

        // 2. Đảm bảo có Player Instance (Giữ nguyên)
        if (playerInstance == null)
        {
            Debug.LogWarning("[Cheat] Không tìm thấy PlayerInstance, khởi tạo bằng Fallback...");
            InitializePlayerCharacter();
            yield return null;
        }

        // 3. Lấy Inventory của Player (Giữ nguyên)
        Inventory inventory = playerInstance.GetComponent<Inventory>();
        if (inventory == null)
        {
            Debug.LogError("[Cheat] Thất bại: Không tìm thấy component Inventory trên PlayerInstance!");
            yield break;
        }

        // --- ✅ SỬA LẠI LOGIC NẠP ĐỒ ---
        Debug.Log("[Cheat] Đang nạp đồ Legendary (chỉ vào data)...");
        inventory.items.Clear(); // Xóa list data
        foreach (GearItem item in allItems)
        {
            if (item.rarity == ItemRarity.Legendary)
            {
                // THAY ĐỔI: Thêm trực tiếp vào List data, KHÔNG GỌI inventory.AddItem(item)
                inventory.items.Add(item);
                // Debug.Log($"[Cheat] Đã thêm data: {item.itemName}"); // Bỏ log này để tránh spam
            }
        }
        // XÓA: Không gọi cập nhật UI của scene cũ
        // inventory.OnInventoryChanged?.Invoke(); 
    }

    /// <summary>
    /// Toggles God Mode cheat: makes player invincible and able to one-shot enemies
    /// </summary>
    private void ToggleGodModeCheat()
    {
        godModeCheatActive = !godModeCheatActive;

        if (playerInstance == null)
        {
            Debug.LogWarning("[CHEAT] Player instance not found!");
            return;
        }

        Character playerCharacter = playerInstance.GetComponent<Character>();
        if (playerCharacter == null)
        {
            Debug.LogWarning("[CHEAT] Player Character component not found!");
            return;
        }

        if (godModeCheatActive)
        {
            // Enable God Mode
            playerCharacter.isInvincible = true;
            playerCharacter.cheatDamageMultiplier = 9999f; // One-shot damage

            // Apply to all characters in battle (if in battle scene)
            if (SceneManager.GetActiveScene().name == "MainGame")
            {
                var battleManager = FindFirstObjectByType<BattleManager>();
                if (battleManager != null && battleManager.playerCharacter != null)
                {
                    battleManager.playerCharacter.isInvincible = true;
                    battleManager.playerCharacter.cheatDamageMultiplier = 9999f;
                }
            }

            Debug.LogWarning("[CHEAT] God Mode ENABLED - Invincibility and One-Shot Damage Active!");
        }
        else
        {
            // Disable God Mode
            playerCharacter.isInvincible = false;
            playerCharacter.cheatDamageMultiplier = 1.0f;

            // Apply to all characters in battle (if in battle scene)
            if (SceneManager.GetActiveScene().name == "MainGame")
            {
                var battleManager = FindFirstObjectByType<BattleManager>();
                if (battleManager != null && battleManager.playerCharacter != null)
                {
                    battleManager.playerCharacter.isInvincible = false;
                    battleManager.playerCharacter.cheatDamageMultiplier = 1.0f;
                }
            }

            Debug.LogWarning("[CHEAT] God Mode DISABLED");
        }
    }

    /// <summary>
    /// Instantly kills the player
    /// </summary>
    public void KillPlayer()
    {
        if (playerInstance == null)
        {
            Debug.LogWarning("[CHEAT] Player instance not found!");
            return;
        }

        Character playerCharacter = playerInstance.GetComponent<Character>();
        if (playerCharacter == null)
        {
            Debug.LogWarning("[CHEAT] Player Character component not found!");
            return;
        }

        // Check if in battle scene
        if (SceneManager.GetActiveScene().name == "MainGame")
        {
            var battleManager = FindFirstObjectByType<BattleManager>();
            if (battleManager != null && battleManager.playerCharacter != null)
            {
                battleManager.playerCharacter.Kill();
                Debug.LogWarning("[CHEAT] Player killed!");
                return;
            }
        }

        // Fallback: kill player character directly
        playerCharacter.Kill();
        Debug.LogWarning("[CHEAT] Player killed!");
    }

    /// <summary>
    /// Tìm và hiển thị DefeatPanel (trên PersistentUICanvas) từ GameManager.
    /// Hàm này sẽ tự động ẩn nút Retry.
    /// </summary>
    public void ShowDefeatPanel()
    {
        Transform defeatPanelTransform = null;

        // Logic tìm DefeatPanel (tương tự như trong DisableDefeatPanel)
        if (playerStatusUI != null)
        {
            Transform persistentCanvas = playerStatusUI.transform.parent;
            if (persistentCanvas != null)
            {
                defeatPanelTransform = persistentCanvas.Find("DefeatPanel");
            }
        }

        if (defeatPanelTransform == null)
        {
            // Fallback: Tìm trong con của GameManager
            Transform persistentCanvasTransform = transform.Find("PersistentUICanvas");
            if (persistentCanvasTransform != null)
            {
                defeatPanelTransform = persistentCanvasTransform.Find("DefeatPanel");
            }
        }

        if (defeatPanelTransform != null)
        {
            GameObject defeatPanel = defeatPanelTransform.gameObject;

            // Tìm và xử lý các nút
            Button[] allButtons = defeatPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in allButtons)
            {
                string buttonName = btn.name.ToLower();

                // 1. ẨN nút Retry
                if (buttonName.Contains("retry"))
                {
                    btn.gameObject.SetActive(false);
                }

                // 2. GÁN lại nút Quit (để chắc chắn nó gọi QuitToMainMenu)
                if (buttonName.Contains("quit"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(QuitToMainMenu);
                }
            }

            // 3. Hiển thị Panel
            defeatPanel.SetActive(true);
            Time.timeScale = 0f; // Tạm dừng game
        }
        else
        {
            Debug.LogError("[GameManager] Không thể tìm thấy DefeatPanel trên PersistentUICanvas!");
        }
    }

    /// <summary>
    /// Lưu trạng thái hiện tại của người chơi (HP, items, stats) làm checkpoint.
    /// </summary>
    public void SaveCheckpoint()
    {
        if (currentRunData == null || currentRunData.playerData == null) return;

        // Sao chép sâu dữ liệu "sống" vào dữ liệu "checkpoint"
        // Dùng hàm Clone() chúng ta đã tạo ở Bước 1
        currentRunData.checkpointPlayerData = currentRunData.playerData.Clone();

        // Lưu game ngay lập tức
        RunSaveService.SaveRun(currentRunData);

        Debug.Log($"[GameManager] CHECKPOINT SAVED. HP: {currentRunData.checkpointPlayerData.currentHP}, Gold: {currentRunData.checkpointPlayerData.gold}");
    }

}