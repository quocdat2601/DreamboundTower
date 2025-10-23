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
        bool isMimicEncounter = (archetypeId == "Mimic"); // ✅ ĐẶT Ở ĐÂY

        // --- 3. TÍNH TOÁN SỐ LƯỢNG KẺ ĐỊCH ---
        int enemyCount = 1;
        if (isMimicEncounter)
        {
            Debug.Log("[BATTLE] MIMIC ENCOUNTER! Ép số lượng quái = 1.");
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
        if (!isMimicEncounter && !overrideUseCustomStats) // Chỉ cần lọc nếu không phải Mimic hoặc custom stats
        {
            availableEnemies = GameManager.Instance.allEnemyTemplates
                .Where(template => template.kind == encounterKind)
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
            if (isMimicEncounter)
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
            else if (isMimicEncounter)
            {
                finalTemplate = GameManager.Instance.allEnemyTemplates.FirstOrDefault(t => t.name == "Mimic");
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
                ApplyEnemyData(enemyGO, finalStats, finalTemplate, false);
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

    void ApplyEnemyData(GameObject enemyGO, StatBlock stats, EnemyTemplateSO template, bool isSplitChild = false)
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

    // Coroutine for the player's basic attack
    IEnumerator PlayerAttackRoutine(int enemyIndex)
    {
        playerTurn = false;

        var target = enemyInstances[enemyIndex].GetComponent<Character>();
        if (target != null)
        {
            playerCharacter.Attack(target);
        }

        yield return new WaitForSeconds(attackDelay);

        if (AllEnemiesDead())
        {
            yield return StartCoroutine(VictoryRoutine());
            yield break;
        }

        yield return StartCoroutine(EnemyTurnRoutine());
        
        // Process end-of-turn status effects
        if (StatusEffectManager.Instance != null)
        {
            StatusEffectManager.Instance.ProcessEndOfTurnEffects();
        }
        
        // Reduce skill cooldowns at the end of each turn
        if (skillManager != null)
        {
            skillManager.ReduceSkillCooldowns();
        }
        
        // End of complete turn - increment turn counter
        currentTurn++;
        playerTurn = true;
        UpdateTurnDisplay();
        NotifyCharactersOfNewTurn(currentTurn);
        // Process start-of-turn status effects
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
        for (int i = 0; i < (enemyInstances?.Length ?? 0); i++)
        {
            if (enemyInstances[i] == null) continue;
            if (playerInstance == null)
            {
                yield break;
            }

            var enemyChar = enemyInstances[i].GetComponent<Character>();
            if (enemyChar != null && playerCharacter != null)
            {
                enemyChar.Attack(playerCharacter);
            }
            yield return new WaitForSeconds(attackDelay);
        }

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
        // 1. Tìm slot và HP TỐI ĐA của con quái gốc
        int originalSlotIndex = -1;
        int originalMaxHP = originalSlime.maxHP; // Lấy HP GỐC

        for (int i = 0; i < enemyInstances.Length; i++)
        {
            if (enemyInstances[i] != null && enemyInstances[i] == originalSlime.gameObject)
            {
                originalSlotIndex = i;
                break;
            }
        }

        if (originalSlotIndex == -1)
        {
            Debug.LogError("[BATTLE] Không tìm thấy quái gốc để tách!");
            yield break;
        }

        // 2. Giết con quái gốc (mà không kích hoạt VictoryRoutine)
        originalSlime.OnDeath -= HandleCharacterDeath; // (Rất quan trọng)
        originalSlime.PlayDeathAnimation();

        yield return new WaitForSeconds(1.1f); // Đợi animation chết

        // 3. Dọn dẹp con quái gốc và giải phóng slot
        Destroy(originalSlime.gameObject);
        enemyInstances[originalSlotIndex] = null; // Slot 1 (đã trống)

        // 4. Tìm SLOT 2 (slot trống bất kỳ khác slot gốc)
        int secondSlotIndex = -1;
        for (int i = 0; i < enemyInstances.Length; i++)
        {
            // Nếu slot này trống VÀ không phải là slot gốc
            if (i != originalSlotIndex && enemyInstances[i] == null)
            {
                secondSlotIndex = i;
                break; // Chỉ cần 1 slot
            }
        }

        // 5. Tính HP cho quái mới (50% HP GỐC)
        int newHP = Mathf.Max(1, Mathf.RoundToInt(originalMaxHP * 0.5f));

        // 6. Spawn Quái 1 (vào slot gốc)
        Debug.Log($"[BATTLE] Spawning quái tách ra ở slot {originalSlotIndex} với {newHP} HP");
        SpawnIndividualSplitEnemy(originalSlotIndex, templateToSpawn, newHP);

        // 7. Spawn Quái 2 (vào slot thứ 2, NẾU tìm thấy)
        if (secondSlotIndex != -1)
        {
            Debug.Log($"[BATTLE] Spawning quái tách ra ở slot {secondSlotIndex} với {newHP} HP");
            SpawnIndividualSplitEnemy(secondSlotIndex, templateToSpawn, newHP);
        }
        else
        {
            Debug.LogWarning("[BATTLE] Không tìm thấy slot trống thứ 2, chỉ tách ra 1 quái mới.");
        }
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
        ApplyEnemyData(enemyGO, finalStats, template, true);

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
                    RefreshSelectionVisual();

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
