// BattleManager.cs
using Map;
using Newtonsoft.Json;
using Presets;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using StatusEffects;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Assets.Scripts.Data;
using DG.Tweening;

/// <summary>
/// Battle system manager for turn-based combat
/// </summary>
public class BattleManager : MonoBehaviour
{
    #region Variables

    [Header("Slots (UI Transforms)")]
    public Transform playerSlot;       // assign PlayerSlot (Transform)
    public Transform[] enemySlots;     // assign EnemySlot transforms

    [Header("Prefabs")]
    public GameObject playerPrefab;    // UI prefab for player
    public GameObject enemyPrefab;     // UI prefab for enemies

    [Header("Combat")]
    public float attackDelay = 0.35f;
    [Tooltip("Final HP = cfg.enemyStats.HP * hpUnit. Set to 1 if your templates already store absolute HP, Mana for 5.")]
    public int hpUnit = 10;
    public int manaUnit = 5;
    private bool playerHasExtraTurn = false; // True if player gets an extra turn from AGI
    private bool hasUsedExtraActionThisTurn = false; // Tracks if player has already used extra action this turn (cap at 1)

    [Tooltip("Tốc độ di chuyển của sprite khi tấn công (pixels/giây)")]
    public float moveSpeed = 500f;
    [Tooltip("Khoảng cách giữ sprite tấn công và mục tiêu (pixels)")]
    public float attackOffset = 30f; // Reduced from 100f to 30% (30f)
    [Tooltip("Thời gian chờ sau khi Player kết thúc hành động (nếu không có lượt thêm)")]
    public float playerActionEndDelay = 0.3f; // Khoảng nghỉ ngắn sau khi Player đánh xong
    [Tooltip("Thời gian chờ sau khi Enemy kết thúc tấn công trước khi Player bắt đầu lượt mới")]
    public float enemyTurnEndDelay = 0.5f; // Khoảng nghỉ dài hơn sau lượt Enemy

    [Header("Debug / Enemy Overrides")]
    [Tooltip("If true, combat ignores map payload and uses the template below.")]
    public bool overrideUseTemplate = false;
    [Tooltip("Template to sample stats from.")]
    public EnemyTemplateSO overrideTemplate;
    [Tooltip("Floor used when sampling from template.")]
    public int overrideFloor = 1;
    [Tooltip("If true, use the custom stats and kind below (bypasses template and map payload).")]
    public bool overrideUseCustomStats = false;
    public EnemyKind overrideKind = EnemyKind.Normal;
    public StatBlock overrideStats;

    [Header("UI References")]
    public BattleUIManager uiManager;   // Assign your UIManager here
    public GameObject defeatPanel;
    public Image battleBackground;
    public EnemyInfoPanel enemyInfoPanel; // Tham chiếu đến script panel (Từ Bước 3)
    public Button inspectTagButton; // Nút "Tag" duy nhất (Từ Bước 2)
    public TextMeshProUGUI inspectTagButtonText; // Text bên trong nút Tag
    public Button attackButton;

    [Header("Status Effect Display")]
    public GameObject statusEffectIconPrefab; // Prefab for status effect icons
    public StatusEffectIconDatabase statusEffectIconDatabase; // Database of status effect icons

    [Header("Turn Display")]
    public TextMeshProUGUI turnText;    // UI text to display turn number
    
    [Header("Skill System")]
    public SkillManager skillManager;   // Reference to skill manager
    public PassiveSkillManager passiveSkillManager;   // Reference to passive skill manager

    // runtime
    private GameObject playerInstance;
    private GameObject[] enemyInstances;
    private Character playerCharacter; // Reference to the player's Character component for quick access
    private EnemyTemplateSO[] enemyTemplates; 

    // state & selection
    private int selectedEnemyIndex = -1;
    private SkillData selectedSkill = null; // Stores the currently selected skill
    private bool playerTurn = true;
    
    // turn counter
    private int currentTurn = 1;
    
    // Public properties for SkillManager access
    public bool PlayerTurn 
    { 
        get { return playerTurn; } 
    }
    

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        // Auto-assign SkillManager if not set
        if (skillManager == null)
        {
            skillManager = FindFirstObjectByType<SkillManager>();
            if (skillManager != null)
            {
                Debug.Log("[BATTLE] Auto-assigned SkillManager from scene");
            }
            else
            {
                Debug.LogError("[BATTLE] No SkillManager found in scene! Please assign one in Inspector.");
            }
        }
        
        // Use a coroutine for setup, similar to ShopManager
        StartCoroutine(SetupBattle());
    }

    private IEnumerator SetupBattle()
    {
        // --- FALLBACK MECHANISM ---
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[BATTLE] GameManager not found! Creating temporary one...");
            GameObject gameManagerPrefab = Resources.Load<GameObject>("GameManager");
            if (gameManagerPrefab != null)
            {
                Instantiate(gameManagerPrefab);
                yield return null; // 1. Đợi GameManager.Awake() chạy, gán Instance

                // 2. Yêu cầu GameManager tạo player fallback
                // (Nó sẽ tự dùng fallbackRace/Class đã gán trên prefab GameManager)
                GameManager.Instance.InitializePlayerCharacter();

                // 3. Giả lập sự kiện SceneLoaded để kích hoạt PlayerHUD
                // Đây là bước then chốt để UI được cập nhật
                GameManager.Instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);

                // 4. Đợi 1 frame để UI (PlayerHUD) kịp chạy FindAndRefresh()
                yield return null;
                GameManager.Instance.currentRunData = new RunData();
                // ✅ BÁO HIỆU RẰNG ĐÂY LÀ CHẾ ĐỘ DEBUG
    GameManager.Instance.isDebugRun = true;
            }
            else
            {
                Debug.LogError("[BATTLE] GameManager prefab not found in Resources!");
                yield break;
            }
        }
        // -------------------------

        // Các logic khởi tạo cũ
        if (defeatPanel != null) defeatPanel.SetActive(false);

        // SpawnPlayer giờ chỉ còn 1 nhiệm vụ: tìm và đặt player vào vị trí
        SpawnPlayer();

        // Initialize UI Manager SAU KHI player và HUD đã sẵn sàng
        if (uiManager != null)
        {
            uiManager.Initialize(this);
        }

        // Initialize turn display
        UpdateTurnDisplay();
        
        // Refresh CombatEffectManager canvas reference for new scene
        CombatEffectManager.Instance?.RefreshCanvasReference();

        SpawnEnemies();
        AutoBindEnemySlotButtons();
        RefreshSelectionVisual();
        NotifyCharactersOfNewTurn(currentTurn);

    }
    void Update()
    {
        // Check for debug key presses if the battle is not busy
        // Removed busy check - players can now use multiple actions per turn

        // Make sure a keyboard is connected before trying to read from it
        if (Keyboard.current == null) return;

        // Press 'Q' to instantly win the battle
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            Debug.LogWarning("DEBUG: Force Victory (Q key pressed).");
            StartCoroutine(VictoryRoutine());
        }

        // Press 'E' to instantly lose the battle
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.LogWarning("DEBUG: Force Defeat (E key pressed).");
            StartCoroutine(DefeatRoutine());
        }
    }
    #endregion

    #region Spawning

    // Handles finding the persistent player or spawning a fallback for testing
    void SpawnPlayer()
    {
        // Chỉ còn lại Priority 1: Tìm player instance từ GameManager
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            playerInstance = GameManager.Instance.playerInstance;
            playerInstance.transform.SetParent(playerSlot, false);
            playerInstance.transform.localPosition = Vector3.zero;
            playerInstance.transform.localScale = Vector3.one;
            playerInstance.SetActive(true);

            playerCharacter = playerInstance.GetComponent<Character>();

            if (playerCharacter != null)
            {
                // Initialize SkillManager
                if (skillManager != null)
                {
                    skillManager.AssignPlayerCharacter(playerCharacter);
                    skillManager.battleManager = this;
                }
                
                // Initialize PassiveSkillManager (only if not already applied)
                if (passiveSkillManager != null)
                {
                    passiveSkillManager.playerCharacter = playerCharacter;
                    passiveSkillManager.playerSkills = playerInstance.GetComponent<PlayerSkills>();
                    
                    // Only apply passive skills if they haven't been applied yet
                    if (passiveSkillManager.appliedModifiers.Count == 0)
                    {
                        passiveSkillManager.ApplyAllPassiveSkills();
                    }
                }
                else
                {
                    // Auto-assign PassiveSkillManager if not set
                    passiveSkillManager = FindFirstObjectByType<PassiveSkillManager>();
                    if (passiveSkillManager != null)
                    {
                        passiveSkillManager.playerCharacter = playerCharacter;
                        passiveSkillManager.playerSkills = playerInstance.GetComponent<PlayerSkills>();
                        passiveSkillManager.ApplyAllPassiveSkills();
                    }
                    else
                    {
                        Debug.LogWarning("[BATTLE] No PassiveSkillManager found in scene! Passive skills won't work.");
                    }
                }

                // Nạp HP/Mana (nếu bạn muốn duy trì HP/Mana giữa các trận đấu)
                if (GameManager.Instance.currentRunData != null)
                {
                    playerCharacter.currentHP = GameManager.Instance.currentRunData.playerData.currentHP;
                    playerCharacter.currentMana = GameManager.Instance.currentRunData.playerData.currentMana;
                }

                // Đăng ký lắng nghe sự kiện "chết"
                playerCharacter.OnDeath += HandleCharacterDeath;
                
                // Subscribe to mana changes for UI updates
                playerCharacter.OnManaChanged += OnPlayerManaChanged;

        // Cập nhật UI lần cuối
        playerCharacter.UpdateHPUI();
        playerCharacter.UpdateManaUI();

            }
            else
            {
                Debug.LogError("[BATTLE] playerInstance không có component Character!");
            }
        }
        else
        {
            // Đây là một lỗi nghiêm trọng nếu nó xảy ra
            Debug.LogError("[BATTLE] GameManager.Instance hoặc playerInstance là null! Không thể spawn người chơi.");
        }
    }

    //Get baseStats of player
    public Character GetPlayerCharacter()
    {
        return playerCharacter;
    }
    void SpawnEnemies()
    {
        if (enemySlots == null || enemySlots.Length == 0) return;

        enemyInstances = new GameObject[enemySlots.Length];
        enemyTemplates = new EnemyTemplateSO[enemySlots.Length];

        if (inspectTagButton != null) inspectTagButton.gameObject.SetActive(false);
        if (enemyInfoPanel != null) enemyInfoPanel.HidePanel();
        // --- 1. LẤY DỮ LIỆU ---
        string archetypeId = null;
        int absoluteFloor = 1;
        EnemyKind encounterKind = EnemyKind.Normal;

        // (Code đọc dữ liệu từ Override và RunData của bạn giữ nguyên)
        if (overrideUseCustomStats)
        {
            Debug.LogWarning("[BATTLE] Using CUSTOM STATS override for spawning.");
            encounterKind = overrideKind;
            archetypeId = "CustomOverride";
        }
        else if (overrideUseTemplate)
        {
            Debug.LogWarning("[BATTLE] Using TEMPLATE override for spawning.");
            encounterKind = overrideTemplate.kind;
            archetypeId = overrideTemplate.name;
            absoluteFloor = overrideFloor;
        }
        else if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            var mapData = GameManager.Instance.currentRunData.mapData;
            archetypeId = mapData.pendingEnemyArchetypeId;
            absoluteFloor = mapData.pendingEnemyFloor;
            encounterKind = (EnemyKind)mapData.pendingEnemyKind;
        }
        else
        {
            Debug.LogError("[BATTLE] No enemy data found from any source! Cannot spawn enemies.");
            return;
        }

        // --- 2. KIỂM TRA MIMIC (SAU KHI ĐÃ CÓ archetypeId) ---
        // Tên file SO của bạn phải là "Mimic"
        //bool isMimicEncounter = (archetypeId == "Mimic"); // ✅ ĐẶT Ở ĐÂY
        bool isSpecialEncounter = (archetypeId == "Mimic" || archetypeId == "RivalChild" || archetypeId == "Spirit" );

        // --- 3. TÍNH TOÁN SỐ LƯỢNG KẺ ĐỊCH ---
        int enemyCount = 1;
        if (isSpecialEncounter)
        {
            Debug.Log($"[BATTLE] SPECIAL ENCOUNTER ({archetypeId})! Ép số lượng quái = 1.");
            enemyCount = 1;
        }
        else
        {
            switch (encounterKind)
            {
                case EnemyKind.Normal: enemyCount = UnityEngine.Random.Range(1, 4); break;
                case EnemyKind.Elite: enemyCount = UnityEngine.Random.Range(1, 3); break;
                case EnemyKind.Boss: enemyCount = 1; break;
            }
        }
        enemyCount = Mathf.Min(enemyCount, enemySlots.Length);

        // --- 4. LỌC DANH SÁCH QUÁI VẬT PHÙ HỢP ---
        List<EnemyTemplateSO> availableEnemies = null;
        if (!isSpecialEncounter && !overrideUseCustomStats) // Chỉ cần lọc nếu không phải Mimic hoặc custom stats
        {
            availableEnemies = GameManager.Instance.allEnemyTemplates
            .Where(template =>
            template != null && // Kiểm tra null
            template.kind == encounterKind && // Đúng loại (Normal/Elite/Boss)
            !template.isUniqueOrEventOnly
        )
        .ToList();
            if (availableEnemies.Count == 0)
            {
                Debug.LogError($"[BATTLE] Không tìm thấy EnemyTemplateSO nào có Kind là '{encounterKind}'!");
                return;
            }
        }

        // --- 5. TẠO QUÁI VẬT ---
        for (int i = 0; i < enemyCount; i++)
        {
            if (enemyPrefab == null) continue;

            int slotIndex = i; // Vị trí bình thường
            if (isSpecialEncounter)
            {
                slotIndex = 0; // Ép vào Slot 1 (index 0)
            }
            if (enemySlots[slotIndex] == null)
            {
                Debug.LogError($"[BATTLE] Enemy Slot {slotIndex} bị null!");
                continue;
            }

            // Chọn template
            EnemyTemplateSO finalTemplate;
            if (overrideUseCustomStats)
            {
                finalTemplate = null; // Sẽ chỉ áp dụng custom stats
            }
            else if (isSpecialEncounter)
            {
                finalTemplate = GameManager.Instance.allEnemyTemplates.FirstOrDefault(t => t.name == archetypeId);
            }
            else
            {
                finalTemplate = availableEnemies[UnityEngine.Random.Range(0, availableEnemies.Count)];
            }

            enemyTemplates[slotIndex] = finalTemplate;

            // Tạo GameObject tại đúng slotIndex
            var enemyGO = Instantiate(enemyPrefab, enemySlots[slotIndex], false);
            enemyGO.transform.localPosition = Vector2.zero;
            enemyGO.transform.localScale = Vector3.one;
            enemyGO.name = $"Enemy_{slotIndex}_{(finalTemplate != null ? finalTemplate.name : "Custom")}";

            // Gán vào mảng enemyInstances tại đúng slotIndex
            enemyInstances[slotIndex] = enemyGO;

            // Áp dụng dữ liệu
            if (overrideUseCustomStats)
            {
                ApplyEnemyData(enemyGO, overrideStats, null);
            }
            else if (finalTemplate != null)
            {
                StatBlock finalStats = finalTemplate.GetStatsAtFloor(absoluteFloor);
                ApplyEnemyData(enemyGO, finalStats, finalTemplate, false, true);
            }
            else
            {
                Debug.LogError($"[BATTLE] Lỗi không mong muốn: finalTemplate là null.");
            }
        }

        // Tắt các slot không dùng
        for (int i = 0; i < enemySlots.Length; i++)
        {
            // Nếu slot này không có instance nào (bị rỗng)
            if (enemyInstances[i] == null)
            {
                if (enemySlots[i] != null) enemySlots[i].gameObject.SetActive(false);
            }
        }
    }

    void ApplyEnemyData(GameObject enemyGO, StatBlock stats, EnemyTemplateSO template, bool isSplitChild = false, bool allowSummonerGimmick = true)
    {
        if (enemyGO == null) return;

        // Gán hình ảnh ngẫu nhiên từ template (nếu có)
        if (template != null && template.sprites != null && template.sprites.Count > 0)
        {
            Image enemyImage = enemyGO.GetComponent<Image>();

            if (template.name == "Mimic") // Tên file SO của bạn phải là "Mimic"
            {
                // 1. Đổi background (giả sử sprite nền là Element 1)
                if (battleBackground != null && template.sprites.Count > 1)
                {
                    battleBackground.sprite = template.sprites[1];
                }
                else if (battleBackground != null)
                {
                    Debug.LogWarning("Mimic template cần 2 sprites: [0] cho quái, [1] cho nền.");
                }

                // 2. Gán sprite cho quái (sprite quái là Element 0)
                if (enemyImage != null)
                {
                    enemyImage.sprite = template.sprites[0];
                }
            }
            else // Logic gán sprite ngẫu nhiên cũ cho quái thường
            {
                if (enemyImage != null)
                {
                    enemyImage.sprite = template.sprites[Random.Range(0, template.sprites.Count)];
                }
            }
        }

        // Gán chỉ số
        var ch = enemyGO.GetComponent<Character>();
        if (ch != null)
        {
            ch.baseMaxHP = stats.HP * hpUnit; // Áp dụng hpUnit ở đây
            ch.baseAttackPower = stats.STR;
            ch.baseDefense = stats.DEF;
            ch.baseMana = stats.MANA;
            ch.baseIntelligence = stats.INT;
            ch.baseAgility = stats.AGI;
            ch.ResetToBaseStats();
            ch.currentHP = ch.maxHP; // Hồi đầy máu
            // Đăng ký lắng nghe sự kiện "chết" của kẻ địch này
            ch.OnDeath += HandleCharacterDeath;
        }

        // Duyệt qua tất cả các gimmick trong danh sách của template
        if ((template.gimmick & EnemyGimmick.Resurrect) != 0)
        {
            enemyGO.AddComponent<ResurrectBehavior>();
        }

        // Kiểm tra xem cờ 'CounterAttack' có được bật không
        if ((template.gimmick & EnemyGimmick.CounterAttack) != 0)
        {
            enemyGO.AddComponent<CounterAttackBehavior>();
        }
        if ((template.gimmick & EnemyGimmick.SplitOnDamage) != 0 && !isSplitChild)
        {
            var split = enemyGO.AddComponent<SplitBehavior>();
            // Gán template gốc cho nó để nó biết spawn con nào
            split.myOriginalTemplate = template;
            Debug.Log($"[BATTLE] Đã gán SplitBehavior cho {enemyGO.name}");
        }
        // Gimmick: Ranged (Bất tử)
        if ((template.gimmick & EnemyGimmick.Ranged) != 0)
        {
            enemyGO.AddComponent<RangedBehavior>();
        }

        // Gimmick: Enrage (Nổi giận)
        if ((template.gimmick & EnemyGimmick.Enrage) != 0)
        {
            enemyGO.AddComponent<EnrageBehavior>();
        }
        // Gimmick: Bony (Giảm sát thương)
        if ((template.gimmick & EnemyGimmick.Bony) != 0)
        {
            enemyGO.AddComponent<BonyBehavior>();
        }

        // Gimmick: Thornmail (Giáp gai)
        if ((template.gimmick & EnemyGimmick.Thornmail) != 0)
        {
            enemyGO.AddComponent<ThornmailBehavior>();
        }
        // Gimmick: Regenerator (Tự hồi phục)
        if ((template.gimmick & EnemyGimmick.Regenerator) != 0)
        {
            enemyGO.AddComponent<RegeneratorBehavior>();
        }

        // Gimmick: Summoner
        // Sửa lại điều kiện if này:
        if (allowSummonerGimmick && !isSplitChild && (template.gimmick & EnemyGimmick.Summoner) != 0)
        {
            // Kiểm tra xem có danh sách triệu hồi không
            if (template.summonableEnemies != null && template.summonableEnemies.Count > 0)
            {
                var summoner = enemyGO.AddComponent<SummonerBehavior>();
                summoner.SetSummonList(template.summonableEnemies); // Gán danh sách
                Debug.Log($"[BATTLE] Đã gán SummonerBehavior cho {enemyGO.name}");
            }
            else
            {
                Debug.LogWarning($"[BATTLE] {template.name} có gimmick Summoner nhưng danh sách summonableEnemies rỗng!");
            }
        }
        // Set up status effect display for enemy
        SetupEnemyStatusEffectDisplay(enemyGO);
    }
    
    /// <summary>
    /// Sets up status effect display UI for an enemy
    /// Creates a container for status effect icons above the enemy's HP bar and links it to the enemy's character
    /// Status effects will appear as scaled icons (2x size) for better visibility
    /// </summary>
    private void SetupEnemyStatusEffectDisplay(GameObject enemyGO)
    {
        if (enemyGO == null || statusEffectIconPrefab == null || statusEffectIconDatabase == null)
        {
            return;
        }
        
        Character enemyChar = enemyGO.GetComponent<Character>();
        if (enemyChar == null) return;
        
        // Find the HP bar (it should be a child of the enemy)
        Transform hpBarTransform = null;
        foreach (Transform child in enemyGO.transform)
        {
            if (child.name == "HpBar")
            {
                hpBarTransform = child;
                break;
            }
        }
        
        if (hpBarTransform == null)
        {
            Debug.LogWarning($"[BATTLE] Could not find HpBar for {enemyGO.name}");
            return;
        }
        
        // Create container for status effect icons above HP bar
        GameObject statusEffectContainer = new GameObject("StatusEffectContainer");
        statusEffectContainer.transform.SetParent(enemyGO.transform, false);
        RectTransform containerRect = statusEffectContainer.AddComponent<RectTransform>();
        
        // Position above HP bar (100 units above center)
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, 100);
        containerRect.sizeDelta = new Vector2(400, 80);
        
        // Configure horizontal layout for icons
        var layoutGroup = statusEffectContainer.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layoutGroup.spacing = 8;
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = true;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.padding = new RectOffset(4, 4, 4, 4);
        
        // Auto-size container to fit content
        var sizeFitter = statusEffectContainer.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        sizeFitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
        
        // Setup StatusEffectDisplayManager to track this enemy's effects
        StatusEffectDisplayManager displayManager = statusEffectContainer.AddComponent<StatusEffectDisplayManager>();
        displayManager.statusEffectIconPrefab = statusEffectIconPrefab;
        displayManager.iconContainer = statusEffectContainer.transform;
        displayManager.iconDatabase = statusEffectIconDatabase;
        displayManager.SetTargetCharacter(enemyChar);
        
        // Scale up icons (2x) for better visibility on enemies
        statusEffectContainer.transform.localScale = new Vector3(2.0f, 2.0f, 1.0f);
    }

    #endregion

    #region Player Actions

    // Called by SkillIconUI when a skill button is clicked
    public void OnPlayerSelectSkill(BaseSkillSO skill)
    {
        if (!playerTurn) return;
        if (skill is SkillData activeSkill)
        {
            selectedSkill = activeSkill;
            
            // If it's a self-targeting skill, execute it immediately
            if (activeSkill.target == TargetType.Self || activeSkill.target == TargetType.AllAlly)
            {
                StartCoroutine(PlayerUseSkillRoutine(activeSkill, -1)); // -1 indicates no enemy target
            }
            // TODO: Highlight selected skill, maybe show targeting UI cursor
        }
    }

    public void OnPlayerDeselectSkill()
    {
        selectedSkill = null;
        // (Logic để bỏ highlight mục tiêu sẽ ở đây)
    }

    // Called from the UI Attack button's OnClick event
    public void OnAttackButton()
    {
        if (!playerTurn) return;
        if (playerInstance == null) return;
        if (selectedEnemyIndex < 0 || enemyInstances == null || selectedEnemyIndex >= enemyInstances.Length || enemyInstances[selectedEnemyIndex] == null)
        {
            Debug.LogWarning("No enemy selected to attack.");
            return;
        }
        StartCoroutine(PlayerAttackRoutine(selectedEnemyIndex));
    }
    /// <summary>
    /// Reloads the current active scene.
    /// Call this from a debug button on the UI.
    /// </summary>
    public void ReloadScene()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var playerChar = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerChar != null)
            {
                Debug.Log("[BATTLE] Reloading scene... Resetting player HP/Mana.");

                // 1. Reset dữ liệu "live"
                playerChar.currentHP = playerChar.maxHP;
                playerChar.currentMana = playerChar.mana;
                playerChar.UpdateHPUI();

                // 2. Reset dữ liệu "lưu trữ" trong RunData
                var runData = GameManager.Instance.currentRunData;
                runData.playerData.currentHP = playerChar.maxHP;
                runData.playerData.currentMana = playerChar.mana;

                // (Tùy chọn) Reset cả Steadfast Heart
                // runData.playerData.steadfastDurability = 3;
                // GameManager.Instance.playerStatusUI.UpdateSteadfastHeart(3);

                // 3. Lưu lại trạng thái đã reset
                RunSaveService.SaveRun(runData);
            }

            RestorePersistentPlayer();
        }

        string currentSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(currentSceneName);
    }
    /// <summary>
    /// Kiểm tra xem Player có được hành động thêm dựa trên AGI không.
    /// CHỈ GỌI SAU KHI PLAYER TẤN CÔNG (không gọi cho skills).
    /// Capped at 1 extra action per turn.
    /// </summary>
    private void CheckForDoubleAction()
    {
        if (playerCharacter == null)
        {
            playerHasExtraTurn = false;
            return;
        }

        // Cap: Only allow 1 extra action per turn
        if (hasUsedExtraActionThisTurn)
        {
            playerHasExtraTurn = false;
            Debug.Log("[BATTLE] Double action already used this turn. Capped at 1 per turn.");
            return;
        }

        // --- CÔNG THỨC TÍNH TỶ LỆ ---
        const float CHANCE_PER_AGI = 0.0015f; // 0.15% dưới dạng thập phân
        const float MAX_CHANCE = 0.25f;       // Tối đa 25%
                                              // --- KẾT THÚC CÔNG THỨC ---

        float doubleActionChance = Mathf.Clamp(playerCharacter.agility * CHANCE_PER_AGI, 0f, MAX_CHANCE);

        // Tung xúc xắc
        if (Random.value <= doubleActionChance)
        {
            playerHasExtraTurn = true;
            
            // Show visual effect for double action
            if (CombatEffectManager.Instance != null)
            {
                CombatEffectManager.Instance.ShowDoubleActionEffect(playerCharacter);
            }
            
            Debug.Log($"<color=yellow>[BATTLE] DOUBLE ACTION PROC! Player gets an extra turn! (AGI: {playerCharacter.agility}, Chance: {doubleActionChance * 100f}%)</color>");
        }
        else
        {
            playerHasExtraTurn = false;
        }
    }
    #endregion

    #region Enemy Selection UI

    // Automatically adds Button components and listeners to enemy slots
    void AutoBindEnemySlotButtons()
    {
        if (enemySlots == null) return;
        for (int i = 0; i < enemySlots.Length; i++)
        {
            var slot = enemySlots[i];
            if (slot == null) continue;

            var img = slot.GetComponent<Image>();
            if (img == null) img = slot.gameObject.AddComponent<Image>();

            var btn = slot.GetComponent<Button>();
            if (btn == null) btn = slot.gameObject.AddComponent<Button>();

            int idx = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectEnemy(idx));
        }
    }

    // Called when an enemy slot is clicked
    public void SelectEnemy(int index)
    {
        
        if (!playerTurn) 
        {
            Debug.LogWarning($"[BATTLE] Cannot select enemy - not player turn");
            return;
        }
        
        if (enemyInstances == null || index < 0 || index >= enemyInstances.Length || enemyInstances[index] == null) return;


        // If a skill is selected, check if it needs an enemy target
        if (selectedSkill != null)
        {
            // Self-targeting skills don't need enemy selection
            if (selectedSkill.target == TargetType.Self || selectedSkill.target == TargetType.AllAlly)
            {
                StartCoroutine(PlayerUseSkillRoutine(selectedSkill, -1)); // -1 indicates no enemy target
            }
            else
            {
                // Enemy-targeting skills need enemy selection
                StartCoroutine(PlayerUseSkillRoutine(selectedSkill, index));
            }
        }
        // If no skill is selected, clicking an enemy selects them for a basic attack
        else
        {
            selectedEnemyIndex = index;
            RefreshSelectionVisual();
        }

        // Kiểm tra xem index có hợp lệ và quái vật còn sống không
        bool isValidTarget = index >= 0 && index < enemyInstances.Length && enemyInstances[index] != null;

        if (isValidTarget)
        {
            // Lấy dữ liệu của quái vật được chọn
            Character enemyChar = enemyInstances[index].GetComponent<Character>();
            EnemyTemplateSO enemyTemplate = enemyTemplates[index]; // Lấy từ mảng đã lưu

            if (enemyChar != null && enemyTemplate != null && inspectTagButton != null)
            {
                // 1. Hiện nút Tag
                inspectTagButton.gameObject.SetActive(true);

                // 2. Cập nhật Text trên nút Tag (ví dụ: "Slime")
                if (inspectTagButtonText != null)
                {
                    inspectTagButtonText.text = enemyTemplate.name; // Lấy tên từ template
                }

                // 3. Xóa listener cũ và gán listener mới
                inspectTagButton.onClick.RemoveAllListeners();
                if (enemyInfoPanel != null)
                {
                    // Khi bấm nút, gọi hàm DisplayEnemyInfo của panel
                    inspectTagButton.onClick.AddListener(() => {
                        enemyInfoPanel.DisplayEnemyInfo(enemyChar, enemyTemplate);
                    });
                }
            }
        }
        else // Nếu click ra ngoài, hoặc quái đã chết
        {
            // Ẩn nút Tag và Panel
            if (inspectTagButton != null) inspectTagButton.gameObject.SetActive(false);
            if (enemyInfoPanel != null) enemyInfoPanel.HidePanel();

            // Bỏ chọn luôn (nếu bạn muốn)
            // selectedEnemyIndex = -1;
            // RefreshSelectionVisual();
        }

    }

    /// <summary>
    /// Updates the visual feedback for the selected enemy
    /// </summary>
    void RefreshSelectionVisual()
    {
        if (enemySlots == null) return;
        
        for (int i = 0; i < enemySlots.Length; i++)
        {
            var slotImg = enemySlots[i]?.GetComponent<Image>();
            if (slotImg == null) continue;

            if (i == selectedEnemyIndex)
            {
                // Golden tint for selected enemy
                slotImg.color = new Color(1f, 0.95f, 0.8f, 0.3f);
            }
            else
            {
                // Transparent for unselected enemies
                slotImg.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
    /// <summary>
    /// Ẩn khung vàng chọn mục tiêu mà KHÔNG thay đổi selectedEnemyIndex.
    /// </summary>
    void HideSelectionVisual()
    {
        if (enemySlots == null) return;

        for (int i = 0; i < enemySlots.Length; i++)
        {
            var slotImg = enemySlots[i]?.GetComponent<Image>();
            if (slotImg == null) continue;

            // Luôn đặt về màu trong suốt
            slotImg.color = new Color(1f, 1f, 1f, 0f);
        }
        // (Tùy chọn: Nếu bạn có UI khác để hiển thị target, ẩn nó ở đây)
    }
    /// <summary>
    /// Updates the turn display UI
    /// </summary>
    void UpdateTurnDisplay()
    {
        if (turnText != null)
        {
            turnText.text = $"Turn {currentTurn}";
        }
        
    }
    #endregion

    #region Turn Coroutines
    IEnumerator PlayerAttackRoutine(int enemyIndex)
    {
        // --- Kiểm tra target ban đầu ---
        if (enemyIndex < 0 || enemyIndex >= enemyInstances.Length || enemyInstances[enemyIndex] == null) yield break;
        GameObject targetGO = enemyInstances[enemyIndex]; // Lưu target ban đầu
        var target = targetGO.GetComponent<Character>();
        if (playerInstance == null || target == null) yield break;

        // --- Khóa UI ---
        if (attackButton != null) attackButton.interactable = false;

        // --- Animation và Attack ---
        Vector3 originalPlayerPos = playerInstance.transform.position;
        // Characters now attack from their current position - no movement


        // --- KIỂM TRA SPLIT TRƯỚC KHI GÂY SÁT THƯƠNG ---
        // (Kiểm tra này có thể không cần thiết nếu SplitBehavior đáng tin cậy)
        // Lưu HP trước
        int hpBeforeAttack = target.currentHP;
        // Lấy SplitBehavior (nếu có)
        SplitBehavior splitBehavior = targetGO.GetComponent<SplitBehavior>();
        bool targetCanSplit = splitBehavior != null && !splitBehavior.hasSplit;
        int splitThresholdHP = 0;
        if (targetCanSplit) splitThresholdHP = Mathf.RoundToInt(target.maxHP * splitBehavior.splitHealthThreshold);
        // ---

        playerCharacter.Attack(target); // Gây sát thương -> Có thể trigger SplitBehavior.HandleDamage() ở đây

        // Chờ delay ngắn
        yield return new WaitForSeconds(attackDelay);

        // --- KIỂM TRA VÀ ĐỢI SPLIT SAU KHI GÂY SÁT THƯƠNG ---
        // Kiểm tra xem target ban đầu có còn tồn tại không VÀ HP có giảm qua ngưỡng không
        if (targetCanSplit && // Nó có khả năng Split
            hpBeforeAttack > splitThresholdHP && // HP trước đó trên ngưỡng
            (targetGO == null || target.currentHP <= 0 || target.currentHP <= splitThresholdHP)) // Target đã bị hủy HOẶC HP đã dưới ngưỡng
                                                                                                 // (Kiểm tra targetGO == null là quan trọng nhất nếu SplitBehavior gọi HandleSplit đáng tin cậy)
        {
            // Có vẻ Split đã được kích hoạt bởi SplitBehavior.HandleDamage -> BattleManager.HandleSplit
            Debug.Log($"[BATTLE] Attack seems to have triggered Split for {target.name}. Waiting for SplitRoutine...");

            // Cần một cách để đợi Coroutine được gọi từ SplitBehavior.
            // Cách đơn giản nhất là đợi một khoảng thời gian đủ để SplitRoutine chạy xong.
            // Thời gian này nên dài hơn 1.1s (animation chết) + thời gian spawn.
            float splitWaitTime = 1.5f; // Ví dụ: đợi 1.5 giây
            yield return new WaitForSeconds(splitWaitTime);

            // Hoặc, cách tốt hơn: BattleManager có thể lưu lại Coroutine đang chạy từ HandleSplit
            // Coroutine currentSplitCoroutine = null; // Biến thành viên của BattleManager
            // Trong HandleSplit: currentSplitCoroutine = StartCoroutine(...)
            // Ở đây: if (currentSplitCoroutine != null) yield return currentSplitCoroutine;
            Debug.Log("[BATTLE] Assumed SplitRoutine finished. Continuing PlayerAttackRoutine.");
        }
        // --- KẾT THÚC KIỂM TRA VÀ ĐỢI SPLIT ---


        // Characters attack from their position - no return movement needed


        // --- XỬ LÝ SAU KHI HÀNH ĐỘNG VÀ SPLIT (NẾU CÓ) ĐÃ XONG ---
        // Bây giờ mới kiểm tra thắng (bao gồm cả trường hợp giết quái Split cuối cùng)
        if (AllEnemiesDead())
        {
            if (attackButton != null) attackButton.interactable = true; // Mở khóa trước khi thắng
            yield return StartCoroutine(VictoryRoutine());
            yield break;
        }

        // Check for double action AFTER normal attack (not skills)
        CheckForDoubleAction();

        if (playerHasExtraTurn) // Có lượt thêm?
        {
            playerHasExtraTurn = false;
            hasUsedExtraActionThisTurn = true; // Mark as used, cap at 1 per turn
            playerTurn = true; // Giữ lượt Player
            if (attackButton != null) attackButton.interactable = true; // Mở khóa UI
            Debug.Log("<color=yellow>[BATTLE] Player uses extra turn! (1/1 used this turn)</color>");
            yield break; // Dừng coroutine, Player hành động tiếp
        }

        // 4. Nếu KHÔNG có lượt thêm: Chờ khoảng nghỉ ngắn
        yield return new WaitForSeconds(playerActionEndDelay); // Đợi trước khi địch đánh

        playerTurn = false;
        HideSelectionVisual();
        // 5. Bắt đầu lượt của Enemy (Logic gốc của bạn)
        yield return StartCoroutine(EnemyTurnRoutine());

        // 6. Xử lý cuối lượt chung (Logic gốc của bạn, chạy SAU EnemyTurn)
        if (StatusEffectManager.Instance != null) { StatusEffectManager.Instance.ProcessEndOfTurnEffects(); }
        if (skillManager != null) { skillManager.ReduceSkillCooldowns(); }
        currentTurn++;
        playerTurn = true; // Player nhận lại lượt cho turn KẾ TIẾP
        hasUsedExtraActionThisTurn = false; // Reset extra action flag for new turn
        if (attackButton != null) attackButton.interactable = true;
        UpdateTurnDisplay();
        NotifyCharactersOfNewTurn(currentTurn);
        // ✅ HIỆN LẠI KHUNG VÀNG (NẾU CÓ MỤC TIÊU CŨ VÀ CÒN SỐNG)
        RefreshSelectionVisual(); // Hàm này sẽ tự kiểm tra selectedEnemyIndex
        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.ProcessStartOfTurnEffects();
        }
        // Regenerate mana at the start of player turn
        if (playerCharacter != null)
        {
            // Calculate total mana regeneration: base 5% + passive bonuses
            int oldMana = playerCharacter.currentMana;
            float totalRegenPercent = 5.0f; // Base 5% per turn
            // Add passive skill mana regeneration per turn (Divine Resonance)
            var conditionalManager = playerCharacter.GetComponent<ConditionalPassiveManager>();
            if (conditionalManager != null && conditionalManager.manaRegenPerTurn > 0f)
            {
                totalRegenPercent += conditionalManager.manaRegenPerTurn * 100f; // Convert decimal to percentage
            }
            // Apply combined mana regeneration
            playerCharacter.RegenerateManaPercent(totalRegenPercent);
            if (playerCharacter.currentMana > oldMana)
            {
                Debug.Log($"[BATTLE] Mana regenerated: {playerCharacter.currentMana - oldMana} (now {playerCharacter.currentMana}/{playerCharacter.mana})");
            }
            // Shield turns are now handled automatically by StatusEffectManager
        }
    }

    // Coroutine for the player using a skill
    IEnumerator PlayerUseSkillRoutine(SkillData skill, int enemyIndex)
    {
        
        // Use SkillManager to handle skill usage   
        if (skillManager != null)
        {
            // Check if skill can be used (mana, cooldown, turn state) BEFORE changing battle state
            if (!skillManager.CanUseSkill(skill))
            {
                Debug.LogWarning($"[BATTLE] Cannot use skill '{skill.displayName}' - insufficient mana or on cooldown");
                selectedSkill = null;
                yield break;
            }
            // Skills don't end the player's turn - player can continue using skills/attacks
            
            // Use the skill (consumes mana and sets cooldown)
            skillManager.UseSkill(skill);
            
            // Spawn VFX before processing effects
            SpawnSkillVFX(skill, enemyIndex);
            
            // Apply skill effects using the new system
            switch (skill.target)
            {
                case TargetType.SingleEnemy:
                    if (enemyIndex >= 0 && enemyIndex < enemyInstances.Length)
                    {
                        var target = enemyInstances[enemyIndex].GetComponent<Character>();
                        if (target != null)
                        {
                            // Use the new skill effect processor
                            SkillEffectProcessor.ProcessSkillEffects(skill, playerCharacter, target);
                        }
                    }
                    else
                    {
                        Debug.LogError($"[BATTLE] Invalid enemy index {enemyIndex} for SingleEnemy skill");
                    }
                    break;
                    
                case TargetType.AllEnemies:
                    for (int i = 0; i < enemyInstances.Length; i++)
                    {
                        if (enemyInstances[i] != null)
                        {
                            var enemyTarget = enemyInstances[i].GetComponent<Character>();
                            if (enemyTarget != null)
                            {
                                // Use the new skill effect processor
                                SkillEffectProcessor.ProcessSkillEffects(skill, playerCharacter, enemyTarget);
                            }
                        }
                    }
                    break;
                    
                case TargetType.Self:
                    // Apply self-targeting effects (buffs, heals, shields)
                    SkillEffectProcessor.ProcessSkillEffects(skill, playerCharacter, playerCharacter);
                    break;
                    
                case TargetType.AllAlly:
                    // Apply to all allies (including player)
                    SkillEffectProcessor.ProcessSkillEffects(skill, playerCharacter, playerCharacter);
                    break;
                    
                default:
                    Debug.LogWarning($"[BATTLE] Unhandled target type: {skill.target}");
                    break;
            }
        }
        else
        {
            Debug.LogError("[BATTLE] SkillManager not assigned!");
            selectedSkill = null;
            playerTurn = true;
            yield break;
        }

        yield return new WaitForSeconds(attackDelay);

        selectedSkill = null; // Reset selected skill after use

        // Skills do NOT trigger double action - only normal attacks do
        // (CheckForDoubleAction removed from here)

        // Check if all enemies are dead after skill use
        if (AllEnemiesDead())
        {
            yield return StartCoroutine(VictoryRoutine());
            yield break;
        }

        // Skills don't end the player's turn - player can continue using skills/attacks
        // Only normal attack ends the turn
        
        // Note: The turn-ending logic (cooldown reduction, mana regen, etc.) 
        // should only happen when the player chooses to end their turn with a normal attack
    }

    /// <summary>
    /// Checks if a skill can be used (delegates to SkillManager)
    /// </summary>
    public bool CanUseSkill(SkillData skill)
    {
        if (skillManager != null)
        {
            return skillManager.CanUseSkill(skill);
        }
        return false;
    }
    
    /// <summary>
    /// Checks if a skill is on cooldown (delegates to SkillManager)
    /// </summary>
    public bool IsSkillOnCooldown(SkillData skill)
    {
        if (skillManager != null)
        {
            return skillManager.IsSkillOnCooldown(skill);
        }
        return false;
    }
    
    /// <summary>
    /// Gets skill cooldown (delegates to SkillManager)
    /// </summary>
    public int GetSkillCooldown(SkillData skill)
    {
        if (skillManager != null)
        {
            return skillManager.GetSkillCooldown(skill);
        }
        return 0;
    }

    // Coroutine for the enemy's turn
    IEnumerator EnemyTurnRoutine()
    {
        // Lặp qua tất cả các slot địch
        for (int i = 0; i < (enemyInstances?.Length ?? 0); i++)
        {
            GameObject enemyGO = enemyInstances[i]; // Lấy GameObject của địch ở slot i

            // Bỏ qua nếu slot trống hoặc Player đã chết
            if (enemyGO == null || playerInstance == null) continue;

            var enemyChar = enemyGO.GetComponent<Character>();
            if (enemyChar == null || playerCharacter == null) continue; // Bỏ qua nếu không lấy được Character

            // --- ✅ THÔNG BÁO LƯỢT HIỆN TẠI CHO ENEMY NÀY ---
            // Gửi message "OnNewTurnStarted" đến CHỈ enemy hiện tại
            // Dùng SendMessage thay vì NotifyCharactersOfNewTurn để tránh spam các enemy khác
            enemyGO.SendMessage("OnNewTurnStarted", currentTurn, SendMessageOptions.DontRequireReceiver);
            // --- KẾT THÚC THÔNG BÁO ---

            bool enemyActed = false; // Cờ đánh dấu

            // --- KIỂM TRA SUMMONER ---
            SummonerBehavior summoner = enemyGO.GetComponent<SummonerBehavior>();
            // Hàm TryConsumeSummonIntent() sẽ đọc cờ wantsToSummon (được set bởi OnNewTurnStarted vừa gọi)
            if (summoner != null && summoner.TryConsumeSummonIntent())
            {
                Debug.Log($"<color=#ADD8E6>[{enemyChar.name}] SUMMONER: Đang thử triệu hồi (Turn {currentTurn})...</color>"); // Thêm log Turn
                bool summonSuccess = AttemptSummon(enemyChar, summoner.GetSummonableEnemies());

                if (summonSuccess)
                {
                    Debug.Log($"<color=#ADD8E6>[{enemyChar.name}] SUMMONER: Triệu hồi thành công!</color>");
                    enemyActed = true;
                    yield return new WaitForSeconds(attackDelay); // Chờ sau khi summon
                }
                else
                {
                    Debug.Log($"<color=orange>[{enemyChar.name}] SUMMONER: Triệu hồi thất bại (Turn {currentTurn}). Sẽ tấn công.</color>"); // Thêm log Turn
                }
            }
            if (!enemyActed)
            {
                // Enemies now attack from their current position - no movement
                AudioManager.Instance?.PlayAttackSFX();
                // --- Thực hiện đòn đánh của Enemy ---
                enemyChar.Attack(playerCharacter); // Enemy tấn công Player

                // Chờ một chút sau khi đánh (dùng attackDelay)
                yield return new WaitForSeconds(attackDelay);
                // --- KẾT THÚC ĐÒN ĐÁNH ---
            }
            // Kiểm tra Player có chết sau đòn đánh này không
            if (playerCharacter.currentHP <= 0)
            {
                // Không cần làm gì thêm ở đây, HandleCharacterDeath và ProcessDeathRoutine sẽ xử lý
                yield break; // Kết thúc EnemyTurnRoutine nếu Player chết
            }

            // Chờ một khoảng nghỉ ngắn giữa các đòn đánh của Enemy (Nếu muốn)
            // yield return new WaitForSeconds(0.1f); // Ví dụ: Chờ 0.1s

        } // Kết thúc vòng lặp for (chuyển sang Enemy tiếp theo)

        // --- KẾT THÚC TOÀN BỘ LƯỢT ENEMY ---
        // Chờ khoảng nghỉ dài hơn trước khi trả lại lượt cho Player
        yield return new WaitForSeconds(enemyTurnEndDelay); // Dùng biến delay mới
    }

    #endregion

    #region Battle Outcome

    // Coroutine for when the player is victorious
    IEnumerator VictoryRoutine()
    {
        Debug.Log("[BATTLE] Victory! All enemies defeated!");
        yield return new WaitForSeconds(1.5f);

        string sceneToReturnTo = "Zone1"; // Scene mặc định nếu có lỗi

        if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            var runData = GameManager.Instance.currentRunData;
            var mapData = runData.mapData;
            var pendingNode = mapData.pendingNodePoint;

            // Cập nhật đường đi
            if (mapData.path.All(p => p != pendingNode))
            {
                mapData.path.Add(pendingNode);
            }

            // Kiểm tra xem node vừa hoàn thành có phải là boss không
            if ((EnemyKind)mapData.pendingEnemyKind == EnemyKind.Boss)
            {
                Debug.Log("[BATTLE] Boss defeated! Advancing to the next zone.");
                // Tăng zone và chuẩn bị scene tiếp theo
                mapData.currentZone++;
                mapData.path.Clear(); // Xóa đường đi cũ để bắt đầu zone mới
                sceneToReturnTo = "Zone" + mapData.currentZone; // Ví dụ: "Zone2", "Zone3"
            }
            else
            {
                // Nếu không phải boss, quay lại scene hiện tại
                sceneToReturnTo = "Zone" + mapData.currentZone;
            }

            // Xóa trạng thái "đang chờ"
            mapData.pendingNodePoint = new Vector2Int(-1, -1);
            mapData.pendingNodeSceneName = null;

            if (playerCharacter != null)
            {
                runData.playerData.currentHP = playerCharacter.currentHP;
                // runData.playerData.currentMana = playerCharacter.currentMana;
            }

            // Lưu lại toàn bộ RunData
            RunSaveService.SaveRun(runData);
            Debug.Log("[SAVE SYSTEM] Node completed. Pending status cleared. Game saved.");
        }

        // Chuyển scene về bản đồ
        RestorePersistentPlayer();
        SceneManager.LoadScene(sceneToReturnTo);
    }
    // Coroutine for when the player is defeated
    IEnumerator DefeatRoutine()
    {
        Debug.Log("[BATTLE] Player died.");

        // ✅ GỌI GAMEMANAGER ĐỂ XỬ LÝ HẬU QUẢ
        bool runEnded = GameManager.Instance.HandlePlayerDefeat();

        // Nếu run thực sự kết thúc (hết mạng)
        if (runEnded)
        {
            // Kích hoạt Defeat Panel với lựa chọn "Quit" hoặc xem kết quả
            if (defeatPanel != null)
            {
                // TODO: Bạn có thể muốn có một panel "Run Over" riêng
                // Tạm thời vẫn dùng defeatPanel
                defeatPanel.SetActive(true);
            }
            // Ở đây, nút Retry có thể bị vô hiệu hóa vì không thể retry được nữa
        }
        // Nếu vẫn còn mạng
        else
        {
            // Vẫn kích hoạt Defeat Panel để cho phép người chơi Quit hoặc Retry
            if (defeatPanel != null)
            {
                defeatPanel.SetActive(true);
            }
        }

        // Tạm dừng ở màn hình thua, chờ người chơi tương tác
        // Chúng ta sẽ không tự động chuyển scene nữa
        yield return null; // Chờ vô tận cho đến khi người chơi bấm nút
    }

    #endregion

    #region Helpers
    #region Skill VFX
    
    /// <summary>
    /// Spawns skill VFX at the appropriate target location
    /// For buff skills (Self/AllAlly), VFX will track the character
    /// For attacking skills (SingleEnemy/AllEnemies), VFX spawns at target position
    /// </summary>
    private void SpawnSkillVFX(SkillData skill, int enemyIndex)
    {
        if (skill == null || skill.vfxPrefab == null)
        {
            return; // No VFX to spawn
        }
        
        // Find canvas for VFX parent
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[BATTLE] No Canvas found for skill VFX!");
            return;
        }
        
        switch (skill.target)
        {
            case TargetType.Self:
            case TargetType.AllAlly:
                // Buff skills: Spawn VFX on player and make it track the character
                if (playerCharacter != null)
                {
                    SpawnTrackingVFX(skill.vfxPrefab, playerCharacter, canvas.transform);
                }
                break;
                
            case TargetType.SingleEnemy:
                // Attacking skill: Spawn VFX at enemy position
                if (enemyIndex >= 0 && enemyIndex < enemyInstances.Length && enemyInstances[enemyIndex] != null)
                {
                    var target = enemyInstances[enemyIndex].GetComponent<Character>();
                    if (target != null)
                    {
                        SpawnPositionVFX(skill.vfxPrefab, target, canvas.transform);
                    }
                }
                break;
                
            case TargetType.AllEnemies:
                // AOE skill: Spawn VFX on all enemies
                for (int i = 0; i < enemyInstances.Length; i++)
                {
                    if (enemyInstances[i] != null)
                    {
                        var target = enemyInstances[i].GetComponent<Character>();
                        if (target != null)
                        {
                            SpawnPositionVFX(skill.vfxPrefab, target, canvas.transform);
                        }
                    }
                }
                break;
        }
    }
    
    /// <summary>
    /// Spawns a VFX that tracks the character (for buff skills)
    /// </summary>
    private void SpawnTrackingVFX(GameObject vfxPrefab, Character target, Transform canvasTransform)
    {
        if (vfxPrefab == null || target == null) return;
        
        Vector3 spawnPosition = GetCharacterUIPosition(target);
        GameObject vfx = Instantiate(vfxPrefab, canvasTransform);
        vfx.transform.position = spawnPosition;
        
        // Add VFXTracker component to make it follow the character
        VFXTracker tracker = vfx.GetComponent<VFXTracker>();
        if (tracker == null)
        {
            tracker = vfx.AddComponent<VFXTracker>();
        }
        tracker.targetCharacter = target;
        
        Debug.Log($"[BATTLE] Spawned tracking VFX for {target.name}");
    }
    
    /// <summary>
    /// Spawns a VFX at the character's position (for attacking skills)
    /// </summary>
    private void SpawnPositionVFX(GameObject vfxPrefab, Character target, Transform canvasTransform)
    {
        if (vfxPrefab == null || target == null) return;
        
        Vector3 spawnPosition = GetCharacterUIPosition(target);
        GameObject vfx = Instantiate(vfxPrefab, canvasTransform);
        vfx.transform.position = spawnPosition;
        
        Debug.Log($"[BATTLE] Spawned VFX at {target.name} position");
    }
    
    /// <summary>
    /// Gets the UI position of a character for VFX spawning
    /// </summary>
    private Vector3 GetCharacterUIPosition(Character character)
    {
        if (character == null) return Vector3.zero;
        
        // Try to get the character's UI Image position first
        if (character.characterImage != null)
        {
            return character.characterImage.transform.position;
        }
        
        // Fallback to transform position
        return character.transform.position;
    }
    
    #endregion

    /// <summary>
    /// Moves the persistent player back under the GameManager before leaving the battle scene.
    /// Without this the player instance stays parented to the battle scene's slot and gets
    /// destroyed when the scene unloads, causing the missing player on the map.
    /// </summary>
    void RestorePersistentPlayer()
    {
        if (GameManager.Instance == null) return;

        var persistentPlayer = GameManager.Instance.playerInstance;
        if (persistentPlayer == null) return;

        // Only detach if the player is currently parented under the battle slot.
        if (persistentPlayer.transform.parent == playerSlot)
        {
            persistentPlayer.transform.SetParent(GameManager.Instance.transform, false);
        }

        // Ensure it survives the next scene load and hide it until the map wants to show it.
        //DontDestroyOnLoad(persistentPlayer);
        persistentPlayer.SetActive(false);
    }
    /// <summary>
    /// Cố gắng triệu hồi một đồng minh mới cho Summoner.
    /// </summary>
    /// <param name="summonerChar">Character của Summoner</param>
    /// <param name="summonList">Danh sách các EnemyTemplateSO có thể gọi</param>
    /// <returns>True nếu triệu hồi thành công, False nếu thất bại (hết slot).</returns>
    private bool AttemptSummon(Character summonerChar, List<EnemyTemplateSO> summonList)
    {
        // 1. Tìm slot trống
        int emptySlotIndex = -1;
        for (int i = 0; i < enemyInstances.Length; i++)
        {
            if (enemyInstances[i] == null)
            {
                emptySlotIndex = i;
                break; // Tìm thấy slot trống đầu tiên
            }
        }

        // Nếu không còn slot trống
        if (emptySlotIndex == -1)
        {
            Debug.LogWarning($"[{summonerChar.name}] SUMMONER: Không còn slot trống để triệu hồi!");
            return false; // Triệu hồi thất bại
        }

        // 2. Chọn quái vật để triệu hồi theo logic
        EnemyTemplateSO templateToSummon = SelectEnemyToSummon(summonList);

        // Nếu không chọn được template nào (ví dụ danh sách lỗi)
        if (templateToSummon == null)
        {
            Debug.LogError($"[{summonerChar.name}] SUMMONER: Không thể chọn template từ summonList!");
            return false; // Triệu hồi thất bại
        }

        // 3. Spawn quái vật vào slot trống
        Debug.Log($"[{summonerChar.name}] SUMMONER: Triệu hồi {templateToSummon.name} vào slot {emptySlotIndex}");
        SpawnIndividualSummonedEnemy(emptySlotIndex, templateToSummon);

        return true; // Triệu hồi thành công
    }
    /// <summary>
    /// Spawn một quái vật được triệu hồi vào slot cụ thể.
    /// </summary>
    private void SpawnIndividualSummonedEnemy(int slotIndex, EnemyTemplateSO template)
    {
        if (slotIndex < 0 || slotIndex >= enemySlots.Length || template == null)
        {
            Debug.LogError($"[BATTLE] Lỗi tham số khi spawn quái triệu hồi: Slot={slotIndex}, Template={(template != null ? template.name : "NULL")}");
            return;
        }

        Transform slot = enemySlots[slotIndex];
        if (slot == null)
        {
            Debug.LogError($"[BATTLE] Enemy Slot {slotIndex} bị null!");
            return;
        }

        // Đảm bảo slot được kích hoạt
        slot.gameObject.SetActive(true);

        // Lấy tầng hiện tại (để quái mới có chỉ số đúng)
        int absoluteFloor = overrideUseTemplate ? overrideFloor : (GameManager.Instance?.currentRunData.mapData.pendingEnemyFloor ?? 1);

        // Tạo GameObject
        var enemyGO = Instantiate(enemyPrefab, slot, false);
        enemyGO.transform.localPosition = Vector2.zero;
        enemyGO.transform.localScale = Vector3.one;
        enemyGO.name = $"Enemy_{slotIndex}_{template.name}_Summoned";

        // Lấy chỉ số từ template
        StatBlock finalStats = template.GetStatsAtFloor(absoluteFloor);

        // Dùng hàm ApplyEnemyData để gán chỉ số, sprite, gimmick...
        // QUAN TRỌNG: KHÔNG truyền isSplitChild = true, để nó có thể tự add gimmick nếu cần
        // Tuy nhiên, ApplyEnemyData nên có kiểm tra để không add SummonerBehavior nếu template có cờ Summoner,
        // tránh triệu hồi đệ quy vô hạn. Cần sửa ApplyEnemyData một chút.
        ApplyEnemyData(enemyGO, finalStats, template, false, false); // <<< Sẽ sửa ApplyEnemyData ở bước sau

        // Hồi đầy máu (ApplyEnemyData đã làm)
        var ch = enemyGO.GetComponent<Character>();
        // if (ch != null) { ch.currentHP = ch.maxHP; ch.UpdateHPUI(); } // Không cần nếu ApplyEnemyData đã hồi

        // Đăng ký quái mới vào mảng quản lý
        enemyInstances[slotIndex] = enemyGO;
        enemyTemplates[slotIndex] = template; // Lưu template của con mới

        // Gán lại sự kiện click cho slot này
        var btn = slot.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            int idx = slotIndex;
            btn.onClick.AddListener(() => SelectEnemy(idx));
        }

        // (Tùy chọn: Chơi animation spawn/xuất hiện cho quái mới)
         enemyGO.transform.localScale = Vector3.zero;
         enemyGO.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack);
    }
    /// <summary>
    /// Chọn một EnemyTemplateSO từ danh sách theo tỷ lệ Normal/Elite.
    /// </summary>
    private EnemyTemplateSO SelectEnemyToSummon(List<EnemyTemplateSO> summonList)
    {
        if (summonList == null || summonList.Count == 0) return null;

        // Phân loại Normal và Elite
        List<EnemyTemplateSO> normalSummons = summonList.Where(e => e != null && e.kind == EnemyKind.Normal).ToList();
        List<EnemyTemplateSO> eliteSummons = summonList.Where(e => e != null && e.kind == EnemyKind.Elite).ToList();

        // Trường hợp đặc biệt: Chỉ có Elite
        if (normalSummons.Count == 0 && eliteSummons.Count > 0)
        {
            return eliteSummons[Random.Range(0, eliteSummons.Count)]; // Chọn Elite ngẫu nhiên
        }

        // Trường hợp không có loại nào (lỗi dữ liệu?)
        if (normalSummons.Count == 0 && eliteSummons.Count == 0)
        {
            return null;
        }

        // Roll tỷ lệ 80% Normal / 20% Elite
        float roll = Random.value; // Số ngẫu nhiên từ 0.0 đến 1.0

        if (roll <= 0.8f && normalSummons.Count > 0) // 80% cơ hội VÀ có Normal
        {
            return normalSummons[Random.Range(0, normalSummons.Count)]; // Chọn Normal ngẫu nhiên
        }
        else if (eliteSummons.Count > 0) // Còn lại (20% hoặc không có Normal) VÀ có Elite
        {
            return eliteSummons[Random.Range(0, eliteSummons.Count)]; // Chọn Elite ngẫu nhiên
        }
        else if (normalSummons.Count > 0) // Nếu roll ra Elite nhưng không có Elite -> Quay lại chọn Normal
        {
            return normalSummons[Random.Range(0, normalSummons.Count)];
        }

        return null; // Trường hợp không mong muốn
    }
    // Checks if all enemies are dead
    bool AllEnemiesDead()
    {
        if (enemyInstances == null) 
        {
            Debug.Log("[BATTLE] AllEnemiesDead: enemyInstances is null, returning true");
            return true;
        }
        
        int aliveEnemies = 0;
        foreach (var e in enemyInstances) 
        {
            if (e != null) 
            {
                aliveEnemies++;
            }
        }
        
        Debug.Log($"[BATTLE] AllEnemiesDead: {aliveEnemies} enemies alive out of {enemyInstances.Length}");
        return aliveEnemies == 0;
    }

    /// <summary>
    /// (HÀM 1) Bắt đầu quá trình Tách quái vật (được gọi từ SplitBehavior)
    /// </summary>
    public void HandleSplit(Character originalSlime, EnemyTemplateSO templateToSpawn)
    {
        // Dùng Coroutine để có thể chờ animation chết hoàn thành
        StartCoroutine(SplitRoutine(originalSlime, templateToSpawn));
    }

    /// <summary>
    /// (HÀM 2) Coroutine xử lý logic tách
    /// </summary>
    private IEnumerator SplitRoutine(Character originalSlime, EnemyTemplateSO templateToSpawn)
    {
        Debug.Log($"[BATTLE] Starting SplitRoutine for {originalSlime.name}");

        // 1. Tìm slot và HP gốc (Giữ nguyên)
        int originalSlotIndex = -1;
        int originalMaxHP = originalSlime.maxHP;
        GameObject originalGO = originalSlime.gameObject; // Lưu GameObject để kiểm tra null sau Destroy
        for (int i = 0; i < enemyInstances.Length; i++) { if (enemyInstances[i] == originalGO) { originalSlotIndex = i; break; } }
        if (originalSlotIndex == -1) { Debug.LogError("SplitRoutine: Cannot find original enemy!"); yield break; }

        // --- KHÔNG QUẢN LÝ LƯỢT Ở ĐÂY ---

        // 2. Giết quái gốc
        originalSlime.OnDeath -= HandleCharacterDeath; // Hủy đăng ký trước khi kill
        originalSlime.PlayDeathAnimation();
        yield return new WaitForSeconds(1.1f); // Đợi animation

        // 3. Dọn dẹp quái gốc
        // Chỉ Destroy nếu GameObject gốc vẫn còn (phòng lỗi)
        if (originalGO != null) Destroy(originalGO);
        // Cập nhật mảng ngay sau khi Destroy logic xảy ra
        if (originalSlotIndex >= 0 && originalSlotIndex < enemyInstances.Length) enemyInstances[originalSlotIndex] = null;
        if (originalSlotIndex >= 0 && originalSlotIndex < enemyTemplates.Length) enemyTemplates[originalSlotIndex] = null;
        if (selectedEnemyIndex == originalSlotIndex) selectedEnemyIndex = -1;

        // 4. Tìm slot 2 (Giữ nguyên)
        int secondSlotIndex = -1;
        for (int i = 0; i < enemyInstances.Length; i++) { if (i != originalSlotIndex && enemyInstances[i] == null) { secondSlotIndex = i; break; } }

        // 5. Tính HP mới (Giữ nguyên)
        int newHP = Mathf.Max(1, Mathf.RoundToInt(originalMaxHP * 0.5f));

        // 6. Spawn Quái 1 & 2 (Giữ nguyên)
        SpawnIndividualSplitEnemy(originalSlotIndex, templateToSpawn, newHP);
        if (secondSlotIndex != -1) { SpawnIndividualSplitEnemy(secondSlotIndex, templateToSpawn, newHP); }

        Debug.Log($"[BATTLE] SplitRoutine finished spawning.");
        // --- KHÔNG GỌI ENEMY TURN HAY XỬ LÝ CUỐI LƯỢT ---
    }

    /// <summary>
    /// (HÀM 3) Hàm hỗ trợ để spawn 1 con quái (con)
    /// </summary>
    private void SpawnIndividualSplitEnemy(int slotIndex, EnemyTemplateSO template, int hpToSet)
    {
        if (slotIndex < 0 || slotIndex >= enemySlots.Length || template == null) return;

        Transform slot = enemySlots[slotIndex];

        // Đảm bảo slot được kích hoạt (vì nó có thể đã bị tắt nếu trống)
        slot.gameObject.SetActive(true);

        int absoluteFloor = overrideUseTemplate ? overrideFloor : (GameManager.Instance?.currentRunData.mapData.pendingEnemyFloor ?? 1);

        var enemyGO = Instantiate(enemyPrefab, slot, false);
        enemyGO.transform.localPosition = Vector2.zero;
        enemyGO.transform.localScale = Vector3.one;
        enemyGO.name = $"Enemy_{slotIndex}_{template.name}_Split";

        StatBlock finalStats = template.GetStatsAtFloor(absoluteFloor);

        // ✅ GỌI HÀM APPLY MỚI
        // Ghi rõ "isSplitChild: true" để nó không thêm SplitBehavior
        // Note: SetupEnemyStatusEffectDisplay is already called inside ApplyEnemyData
        ApplyEnemyData(enemyGO, finalStats, template, true, false);

        // GHI ĐÈ HP VỀ 50%
        var ch = enemyGO.GetComponent<Character>();
        if (ch != null)
        {
            // Quan trọng: Gán HP con = 50% HP GỐC
            ch.currentHP = hpToSet;

            // (Tùy chọn: Nếu bạn muốn HP tối đa của nó cũng là 50%)
            // ch.maxHP = hpToSet; 
            // (Hiện tại, nó sẽ có 50/100 HP, điều này có vẻ hợp lý)

            ch.UpdateHPUI(); // Cập nhật thanh máu
        }

        enemyInstances[slotIndex] = enemyGO;

        // Gán lại sự kiện click cho slot này (RẤT QUAN TRỌNG)
        var btn = slot.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            int idx = slotIndex;
            btn.onClick.AddListener(() => SelectEnemy(idx));
        }
    }
    #endregion

    #region Button Actions

    public void OnQuitButton()
    {
        Debug.Log("Quit button pressed. Clearing run data and returning to Main Menu.");

        // Xóa toàn bộ dữ liệu của run hiện tại
        RunSaveService.ClearRun();

        // Tải lại scene Main Menu
        SceneManager.LoadScene("MainMenu");
    }

    public void OnRetryButton()
    {
        Debug.Log("Retry button pressed. Reverting to the last checkpoint.");

        var runData = GameManager.Instance.currentRunData;

        // Nếu hết mạng thì không cho retry
        if (runData.playerData.steadfastDurability <= 0)
        {
            Debug.LogWarning("Cannot retry, no durability left. Forcing quit.");
            OnQuitButton();
            return;
        } // --- LOGIC KHÔI PHỤC CHECKPOINT ---
          // 1. Xóa bản đồ hiện tại để buộc game tạo map mới cho zone
        runData.mapData.currentMapJson = null;

        // 2. Xóa đường đi cũ trên bản đồ
        runData.mapData.path.Clear();

        // 3. Khôi phục lại máu/mana (ví dụ: hồi đầy)
        //    Lưu ý: Chúng ta cần truy cập vào chỉ số max của người chơi
        //    Cách đơn giản nhất là thêm maxHP, maxMana vào PlayerData
        //    (Bạn cần thêm public int maxHP; public int maxMana; vào PlayerData.cs)
        //    Tạm thời, chúng ta sẽ hồi máu dựa trên chỉ số từ Character component
        var playerChar = GameManager.Instance.playerInstance.GetComponent<Character>();
        if (playerChar != null)
        {
            runData.playerData.currentHP = playerChar.maxHP;
            //runData.playerData.currentMana = playerChar.maxMana;
        }

        // 4. Lưu lại trạng thái đã khôi phục
        RunSaveService.SaveRun(runData);

        // 5. Tải lại scene của zone hiện tại để bắt đầu lại
        string zoneSceneToLoad = "Zone" + runData.mapData.currentZone;
        SceneManager.LoadScene(zoneSceneToLoad);
    }

    private void HandleCharacterDeath(Character deadCharacter)
    {
        Debug.Log($"[BATTLE] Character {deadCharacter.name} has died!");
        Debug.Log($"[BATTLE] Starting ProcessDeathRoutine for {deadCharacter.name}");
        // Dùng một coroutine nhỏ để đợi một chút, cho phép các hiệu ứng
        // như animation chết hoặc gimmick hồi sinh có thời gian để chạy.
        StartCoroutine(ProcessDeathRoutine(deadCharacter));
    }
    
    void OnPlayerManaChanged(int currentMana, int maxMana)
    {
        // Update mana UI through PlayerStatusController
        PlayerStatusController statusController = FindFirstObjectByType<PlayerStatusController>();
        if (statusController != null)
        {
            statusController.UpdateMana(currentMana, maxMana);
        }
    }
    
    private IEnumerator ProcessDeathRoutine(Character deadCharacter)
    {
        // Đợi một khoảng thời gian rất ngắn (ví dụ: cuối frame)
        // để các script gimmick (như Resurrect) có cơ hội can thiệp vào currentHP
        yield return new WaitForEndOfFrame();

        // 1. KIỂM TRA XEM ĐÓ LÀ NGƯỜI CHƠI HAY KẺ ĐỊCH

        // Nếu là người chơi
        if (deadCharacter == playerCharacter)
        {
            Debug.Log("[BATTLE] Player death signal received.");
            // Bắt đầu chuỗi sự kiện thua cuộc
            StartCoroutine(DefeatRoutine());
            yield break; // Dừng coroutine ở đây
        }

        for (int i = 0; i < enemyInstances.Length; i++)
        {
            if (enemyInstances[i] != null && enemyInstances[i] == deadCharacter.gameObject)
            {
                if (deadCharacter.currentHP > 0)
                {
                    Debug.Log($"[BATTLE] {deadCharacter.name} đã được hồi sinh, không hủy đối tượng.");
                    // và gọi nó ở đây để quái vật nhấp nháy/phát sáng khi sống lại)
                     deadCharacter.PlayReviveAnimation();

                    yield break;
                }

                // NẾU KHÔNG HỒI SINH (CHẾT THẬT)
                deadCharacter.OnDeath -= HandleCharacterDeath;

                // ✅ CHƠI ANIMATION CHẾT Ở ĐÂY
                deadCharacter.PlayDeathAnimation();

                // Wait for death animation to complete (1 second) before destroying
                yield return new WaitForSeconds(1.1f); // Đợi animation chạy xong

                // --- TEST ONLY: Thưởng vàng khi quái chết ---
                try
                {
                    if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
                    {
                        int goldGain = 5; // mặc định
                        var template = enemyTemplates[i];
                        if (template != null)
                        {
                            // Thưởng cao hơn cho Elite/Boss (giá trị tạm để test)
                            switch (template.kind)
                            {
                                case Presets.EnemyKind.Elite:
                                    goldGain = 15;
                                    break;
                                case Presets.EnemyKind.Boss:
                                    goldGain = 40;
                                    break;
                                default:
                                    goldGain = 5;
                                    break;
                            }
                        }

                        GameManager.Instance.currentRunData.playerData.gold += goldGain;
                        GameManager.Instance.UpdatePlayerGoldUI(GameManager.Instance.currentRunData.playerData.gold);
                        Debug.Log($"[BATTLE][TEST] Awarded {goldGain} gold for killing {deadCharacter.name}. Total: {GameManager.Instance.currentRunData.playerData.gold}");
                    }
                }
                catch { }

                // Destroy the enemy GameObject
                Destroy(enemyInstances[i]);
                Debug.Log($"[BATTLE] Destroyed enemy {i}");

                // Mark slot as empty
                enemyInstances[i] = null;
                Debug.Log($"[BATTLE] Marked enemy slot {i} as null");
                enemyTemplates[i] = null;
                // If this enemy was selected as target, deselect it
                if (selectedEnemyIndex == i)
                {
                    selectedEnemyIndex = -1;
                    //RefreshSelectionVisual();
                    HideSelectionVisual();

                    if (inspectTagButton != null) inspectTagButton.gameObject.SetActive(false);
                    if (enemyInfoPanel != null) enemyInfoPanel.HidePanel();
                }
                
                // Check if all enemies are now dead after destroying this one
                Debug.Log($"[BATTLE] Checking victory condition after destroying enemy {i}");
                if (AllEnemiesDead())
                {
                    Debug.Log($"[BATTLE] All enemies defeated! Starting victory routine...");
                    StartCoroutine(VictoryRoutine());
                }
                
                break; // Exit loop since we found and processed the enemy
            }
        }
    }
    /// <summary>
    /// Gửi thông báo đến tất cả các nhân vật (Player + Enemies) về lượt mới
    /// </summary>
    void NotifyCharactersOfNewTurn(int turn)
    {
        string message = "OnNewTurnStarted";

        // Thông báo cho Player
        if (playerCharacter != null)
        {
            playerCharacter.BroadcastMessage(message, turn, SendMessageOptions.DontRequireReceiver);
        }

        // Thông báo cho tất cả Enemies
        if (enemyInstances == null) return;
        foreach (var enemyGO in enemyInstances)
        {
            if (enemyGO != null)
            {
                enemyGO.SendMessage(message, turn, SendMessageOptions.DontRequireReceiver);
            }
        }
    }
    #endregion

}
