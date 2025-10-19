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
    
    [Header("Turn Display")]
    public TextMeshProUGUI turnText;    // UI text to display turn number
    
    [Header("Skill System")]
    public SkillManager skillManager;   // Reference to skill manager
    public PassiveSkillManager passiveSkillManager;   // Reference to passive skill manager

    // runtime
    private GameObject playerInstance;
    private GameObject[] enemyInstances;
    private Character playerCharacter; // Reference to the player's Character component for quick access

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

        SpawnEnemies();
        AutoBindEnemySlotButtons();
        RefreshSelectionVisual();
        
        // Debug battle state after initialization
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
                
                // Initialize PassiveSkillManager
                if (passiveSkillManager != null)
                {
                    passiveSkillManager.playerCharacter = playerCharacter;
                    passiveSkillManager.playerSkills = playerInstance.GetComponent<PlayerSkills>();
                    passiveSkillManager.ApplyAllPassiveSkills();
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
                        Debug.Log("[BATTLE] Auto-assigned PassiveSkillManager from scene");
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

            // ✅ SỬA LẠI LOGIC CHỌN SLOT
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

            // ✅ SỬA LẠI LOGIC TẠO QUÁI
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
                ApplyEnemyData(enemyGO, finalStats, finalTemplate);
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

    void ApplyEnemyData(GameObject enemyGO, StatBlock stats, EnemyTemplateSO template)
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
        
        // End of complete turn - increment turn counter
        currentTurn++;
        playerTurn = true;
        UpdateTurnDisplay();
        
        // Regenerate mana at the start of player turn
        if (playerCharacter != null)
        {
            // Simple mana regeneration: 5% of max mana per turn
            int oldMana = playerCharacter.currentMana;
            playerCharacter.RegenerateManaPercent(5.0f);
            if (playerCharacter.currentMana > oldMana)
            {
                Debug.Log($"[BATTLE] Mana regenerated: {playerCharacter.currentMana - oldMana} (now {playerCharacter.currentMana}/{playerCharacter.mana})");
            }
            
            // Reduce shield turns
            playerCharacter.ReduceShieldTurns();
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
            
            // Now that we know the skill can be used, change battle state
            playerTurn = false;
            
            // Use the skill (consumes mana and sets cooldown)
            skillManager.UseSkill(skill);
            
            // Calculate damage
            int damage = skillManager.CalculateSkillDamage(skill);
            
            // Apply skill effect based on target type
            switch (skill.target)
            {
                case TargetType.SingleEnemy:
                    if (enemyIndex >= 0 && enemyIndex < enemyInstances.Length)
                    {
                        var target = enemyInstances[enemyIndex].GetComponent<Character>();
                        if (target != null)
                        {
                            // Use shield system for player, regular damage for enemies
                            if (target == playerCharacter)
                            {
                                target.TakeDamageWithShield(damage, playerCharacter);
                            }
                            else
                            {
                                target.TakeDamage(damage, playerCharacter);
                            }
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
                                enemyTarget.TakeDamage(damage, playerCharacter);
                            }
                        }
                    }
                    break;
                    
                case TargetType.Self:
                    // Apply self-targeting effects (buffs, heals, shields)
                    ApplySelfTargetSkill(skill, damage);
                    break;
                    
                case TargetType.AllAlly:
                    // Apply to all allies (including player)
                    ApplySelfTargetSkill(skill, damage);
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

        if (AllEnemiesDead())
        {
            yield return StartCoroutine(VictoryRoutine());
            yield break;
        }

        yield return StartCoroutine(EnemyTurnRoutine());
        
        // Reduce skill cooldowns at the end of each turn
        if (skillManager != null)
        {
            skillManager.ReduceSkillCooldowns();
        }
        
        // End of complete turn - increment turn counter
        currentTurn++;
        playerTurn = true;
        UpdateTurnDisplay();
        
        // Regenerate mana at the start of player turn
        if (playerCharacter != null)
        {
            // Simple mana regeneration: 5% of max mana per turn
            int oldMana = playerCharacter.currentMana;
            playerCharacter.RegenerateManaPercent(5.0f);
            if (playerCharacter.currentMana > oldMana)
            {
                Debug.Log($"[BATTLE] Mana regenerated: {playerCharacter.currentMana - oldMana} (now {playerCharacter.currentMana}/{playerCharacter.mana})");
            }
            
            // Reduce shield turns
            playerCharacter.ReduceShieldTurns();
        }
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

    // Checks if a specific enemy has died
    bool DidEnemyDie(int index)
    {
        if (index < 0 || index >= enemyInstances.Length || enemyInstances[index] == null) return true;
        var enemyChar = enemyInstances[index].GetComponent<Character>();
        return enemyChar != null && enemyChar.currentHP <= 0;
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
    
    /// <summary>
    /// Applies self-targeting skills (buffs, heals, shields, etc.)
    /// </summary>
    void ApplySelfTargetSkill(SkillData skill, int effectValue)
    {
        if (playerCharacter == null) return;
        
        // For now, we'll implement basic effects based on skill name or description
        // In a full system, you'd have more sophisticated effect handling
        
        string skillName = skill.displayName.ToLower();
        
        if (skillName.Contains("heal") || skillName.Contains("restore"))
        {
            // Healing skill
            int healAmount = effectValue;
            playerCharacter.RestoreHealth(healAmount);
            Debug.Log($"[BATTLE] Player healed for {healAmount} HP");
        }
        else if (skillName.Contains("shield") || skillName.Contains("barrier"))
        {
            // Shield skill - apply shield based on skill description
            int shieldAmount = Mathf.RoundToInt(playerCharacter.maxHP * 0.15f); // 15% of max HP
            int turns = 2; // 2 turns duration
            float reflection = 20f; // 20% damage reflection
            
            playerCharacter.ApplyShield(shieldAmount, turns, reflection);
            Debug.Log($"[BATTLE] Player gained shield: {shieldAmount} HP for {turns} turns, {reflection}% reflection");
        }
        else if (skillName.Contains("buff") || skillName.Contains("boost"))
        {
            // Buff skill - could add temporary stat bonuses
            Debug.Log($"[BATTLE] Player gained buff effect (value: {effectValue})");
            // TODO: Implement buff system
        }
        else
        {
            // Generic self-targeting skill
            Debug.Log($"[BATTLE] Applied self-targeting skill '{skill.displayName}' with value {effectValue}");
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

        // Nếu là kẻ địch
        for (int i = 0; i < enemyInstances.Length; i++)
        {
            if (enemyInstances[i] != null && enemyInstances[i] == deadCharacter.gameObject)
            {
                // SAU KHI ĐỢI, KIỂM TRA LẠI MÁU CỦA KẺ ĐỊCH
                // Nếu HP của nó lớn hơn 0 (do được hồi sinh), thì không làm gì cả!
                if (deadCharacter.currentHP > 0)
                {
                    Debug.Log($"[BATTLE] {deadCharacter.name} đã được hồi sinh, không hủy đối tượng.");
                    yield break;
                }

                // Unsubscribe from death event to prevent errors
                deadCharacter.OnDeath -= HandleCharacterDeath;

                // Wait for death animation to complete (1 second) before destroying
                yield return new WaitForSeconds(1.1f); // Slightly longer than animation duration

                // Destroy the enemy GameObject
                Destroy(enemyInstances[i]);
                Debug.Log($"[BATTLE] Destroyed enemy {i}");

                // Mark slot as empty
                enemyInstances[i] = null;
                Debug.Log($"[BATTLE] Marked enemy slot {i} as null");

                // If this enemy was selected as target, deselect it
                if (selectedEnemyIndex == i)
                {
                    selectedEnemyIndex = -1;
                    RefreshSelectionVisual();
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

    #endregion

}
