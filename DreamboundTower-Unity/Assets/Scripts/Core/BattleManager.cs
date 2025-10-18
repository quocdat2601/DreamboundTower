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
    public BattleUIManager uiManager;   // Assign your UIManager here
    public GameObject defeatPanel;

    // runtime
    private GameObject playerInstance;
    private GameObject[] enemyInstances;
    private Character playerCharacter; // Reference to the player's Character component for quick access

    // state & selection
    private int selectedEnemyIndex = -1;
    private SkillData selectedSkill = null; // Stores the currently selected skill
    private bool playerTurn = true;
    private bool busy = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
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

        SpawnEnemies();
        AutoBindEnemySlotButtons();
        RefreshSelectionVisual();
    }
    void Update()
    {
        // Check for debug key presses if the battle is not busy
        if (busy) return;

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
                // Nạp HP/Mana (nếu bạn muốn duy trì HP/Mana giữa các trận đấu)
                if (GameManager.Instance.currentRunData != null)
                {
                    playerCharacter.currentHP = GameManager.Instance.currentRunData.playerData.currentHP;
                    // playerCharacter.currentMana = GameManager.Instance.currentRunData.playerData.currentMana;
                }

                // Đăng ký lắng nghe sự kiện "chết"
                playerCharacter.OnDeath += HandleCharacterDeath;

                // Cập nhật UI lần cuối
                playerCharacter.UpdateHPUI();

                Debug.Log("[BATTLE] Player Spawned from GameManager.");
                Debug.Log($"[BATTLE] Player Stats: HP={playerCharacter.currentHP}/{playerCharacter.maxHP}, STR={playerCharacter.attackPower}, DEF={playerCharacter.defense}, MANA={playerCharacter.mana}, INT={playerCharacter.intelligence}, AGI={playerCharacter.agility}");
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

        // Khởi tạo mảng instance với kích thước tối đa
        enemyInstances = new GameObject[enemySlots.Length];

        // --- LOGIC MỚI: Ưu tiên đọc từ các nguồn dữ liệu khác nhau ---
        string archetypeId = null;
        int absoluteFloor = 1;
        EnemyKind encounterKind = EnemyKind.Normal;

        // Ưu tiên 1: Ghi đè (Override) từ Inspector để debug
        if (overrideUseCustomStats)
        {
            Debug.LogWarning("[BATTLE] Using CUSTOM STATS override for spawning.");
            encounterKind = overrideKind;
            // Với custom stats, không có archetype cụ thể, nhưng ta có thể gán một tên tạm
            archetypeId = "CustomOverride";
        }
        else if (overrideUseTemplate)
        {
            Debug.LogWarning("[BATTLE] Using TEMPLATE override for spawning.");
            encounterKind = overrideTemplate.kind;
            archetypeId = overrideTemplate.name;
            absoluteFloor = overrideFloor;
        }
        // Ưu tiên 2: Đọc từ RunData (luồng game chính)
        else if (GameManager.Instance != null && GameManager.Instance.currentRunData != null)
        {
            var mapData = GameManager.Instance.currentRunData.mapData;
            archetypeId = mapData.pendingEnemyArchetypeId;
            absoluteFloor = mapData.pendingEnemyFloor;
            encounterKind = (EnemyKind)mapData.pendingEnemyKind;
            Debug.Log($"[BATTLE] Loaded enemy data from RunData: floor={absoluteFloor}, kind={encounterKind}");
        }
        // Lựa chọn cuối: Dùng fallback từ PlayerPrefs (để phòng trường hợp cũ)
        else
        {
            Debug.LogWarning("[BATTLE] RunData not found. Falling back to PlayerPrefs.");
            int kind, hp, str, def, mana, intel, agi;
            if (Map.MapTravel.TryReadAndClearPendingEnemy(out kind, out hp, out str, out def, out mana, out intel, out agi, out absoluteFloor, out archetypeId))
            {
                encounterKind = (EnemyKind)kind;
                Debug.Log($"[BATTLE] Loaded enemy from PlayerPrefs fallback: floor={absoluteFloor}, kind={encounterKind}");
            }
            else
            {
                Debug.LogError("[BATTLE] No enemy data found from any source! Cannot spawn enemies.");
                return;
            }
        }

        // Tính toán số lượng kẻ địch dựa trên loại encounter
        int enemyCount = 1;
        switch (encounterKind)
        {
            case EnemyKind.Normal:
                enemyCount = Random.Range(1, 4); // Ngẫu nhiên 1, 2, hoặc 3
                break;
            case EnemyKind.Elite:
                enemyCount = Random.Range(1, 3); // Ngẫu nhiên 1 hoặc 2
                break;
            case EnemyKind.Boss:
                enemyCount = 1;
                break;
        }

        // Đảm bảo không tạo nhiều kẻ địch hơn số slot có sẵn
        enemyCount = Mathf.Min(enemyCount, enemySlots.Length);
        Debug.Log($"[BATTLE] Spawning {enemyCount} enemies for a {encounterKind} encounter.");

        // Lấy danh sách tất cả các "binh chủng" có cấp bậc phù hợp từ kho của GameManager
        List<EnemyTemplateSO> availableEnemies = GameManager.Instance.allEnemyTemplates
            .Where(template => template.kind == encounterKind)
            .ToList();

        // Nếu không có quái vật nào phù hợp trong kho, báo lỗi và dừng lại
        if (availableEnemies.Count == 0)
        {
            Debug.LogError($"[BATTLE] Không tìm thấy EnemyTemplateSO nào có Kind là '{encounterKind}' trong kho!");
            return;
        }

        // Vòng lặp tạo kẻ địch
        for (int i = 0; i < enemyCount; i++)
        {
            if (enemyPrefab == null || enemySlots[i] == null) continue;

            // Chọn ngẫu nhiên một loài quái vật TỪ DANH SÁCH CÓ SẴN cho MỖI LẦN lặp
            EnemyTemplateSO finalTemplate = availableEnemies[Random.Range(0, availableEnemies.Count)];

            var enemyGO = Instantiate(enemyPrefab, enemySlots[i], false);
            enemyGO.transform.localPosition = Vector2.zero;
            enemyGO.transform.localScale = Vector3.one;
            enemyGO.name = "Enemy_" + i;
            enemyInstances[i] = enemyGO;

            // Nếu là override custom stats, áp dụng trực tiếp
            if (overrideUseCustomStats)
            {
                ApplyEnemyData(enemyGO, overrideStats, null);
                continue;
            }
            // Áp dụng dữ liệu từ template vừa được chọn ngẫu nhiên
            if (finalTemplate != null)
            {
                StatBlock finalStats = finalTemplate.GetStatsAtFloor(absoluteFloor);
                ApplyEnemyData(enemyGO, finalStats, finalTemplate);
            }
            else // Trường hợp này rất khó xảy ra
            {
                Debug.LogError($"[BATTLE] Lỗi không mong muốn: finalTemplate là null.");
            }
        }

        // Tắt các slot kẻ địch không được sử dụng
        for (int i = enemyCount; i < enemySlots.Length; i++)
        {
            if (enemySlots[i] != null)
            {
                enemySlots[i].gameObject.SetActive(false);
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
            if (enemyImage != null)
            {
                // Chọn một sprite ngẫu nhiên từ danh sách
                enemyImage.sprite = template.sprites[Random.Range(0, template.sprites.Count)];
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

    /// <summary>
    /// Finds an EnemyTemplateSO in the Resources/EnemyTemplates folder by its asset name.
    /// </summary>
    private EnemyTemplateSO FindTemplateByArchetype(string archetypeId)
    {
        if (string.IsNullOrEmpty(archetypeId)) return null;

        var templates = Resources.LoadAll<EnemyTemplateSO>("EnemyTemplates");
        return templates.FirstOrDefault(t => t.name == archetypeId);
    }

    #endregion

    #region Player Actions

    // Called by SkillIconUI when a skill button is clicked
    public void OnPlayerSelectSkill(BaseSkillSO skill)
    {
        if (!playerTurn || busy) return;
        if (skill is SkillData activeSkill)
        {
            Debug.Log($"Player selected skill: {activeSkill.displayName}");
            selectedSkill = activeSkill;
            // TODO: Highlight selected skill, maybe show targeting UI cursor
        }
    }

    public void OnPlayerDeselectSkill()
    {
        Debug.Log("Player deselected skill.");
        selectedSkill = null;
        // (Logic để bỏ highlight mục tiêu sẽ ở đây)
    }

    // Called from the UI Attack button's OnClick event
    public void OnAttackButton()
    {
        if (!playerTurn || busy) return;
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
        if (!playerTurn || busy) return;
        if (enemyInstances == null || index < 0 || index >= enemyInstances.Length || enemyInstances[index] == null) return;

        // If a skill is selected, clicking an enemy executes the skill
        if (selectedSkill != null)
        {
            StartCoroutine(PlayerUseSkillRoutine(selectedSkill, index));
        }
        // If no skill is selected, clicking an enemy selects them for a basic attack
        else
        {
            selectedEnemyIndex = index;
            RefreshSelectionVisual();
        }
    }

    // Updates the visual feedback for the selected enemy
    void RefreshSelectionVisual()
    {
        if (enemySlots == null) return;
        for (int i = 0; i < enemySlots.Length; i++)
        {
            var slotImg = enemySlots[i]?.GetComponent<Image>();
            if (slotImg == null) continue;

            if (i == selectedEnemyIndex)
            {
                slotImg.color = new Color(1f, 0.95f, 0.8f, 0.3f);
            }
            else
            {
                slotImg.color = new Color(1f, 1f, 1f, 0f);
            }
        }
    }
    #endregion

    #region Turn Coroutines

    // Coroutine for the player's basic attack
    IEnumerator PlayerAttackRoutine(int enemyIndex)
    {
        busy = true;
        playerTurn = false;

        var target = enemyInstances[enemyIndex].GetComponent<Character>();
        if (target != null)
        {
            playerCharacter.Attack(target);
            Debug.Log($"[BATTLE] Player attacked {target.name} for {playerCharacter.attackPower} damage");
        }

        yield return new WaitForSeconds(attackDelay);


        if (AllEnemiesDead())
        {
            yield return StartCoroutine(VictoryRoutine());
            yield break;
        }

        yield return StartCoroutine(EnemyTurnRoutine());
        playerTurn = true;
        busy = false;
    }

    // Coroutine for the player using a skill
    IEnumerator PlayerUseSkillRoutine(SkillData skill, int enemyIndex)
    {
        busy = true;
        playerTurn = false;

        Debug.Log($"Executing skill '{skill.displayName}'...");
        // TODO: Subtract mana, check cooldown
        // TODO: Apply skill effects (damage, buffs/debuffs)

        // Example: Apply damage from skill
        var target = enemyInstances[enemyIndex].GetComponent<Character>();
        if (target != null)
        {
            // This is a simplified damage calculation, you can use your TooltipFormatter logic here
            int damage = skill.baseDamage; // Add scaling stat calculation here
            target.TakeDamage(damage, playerCharacter);
            Debug.Log($"[BATTLE] Player used {skill.displayName} on {target.name} for {damage} damage.");
        }

        yield return new WaitForSeconds(attackDelay);

        selectedSkill = null; // Reset selected skill after use

        if (AllEnemiesDead())
        {
            yield return StartCoroutine(VictoryRoutine());
            yield break;
        }

        yield return StartCoroutine(EnemyTurnRoutine());
        playerTurn = true;
        busy = false;
    }

    // Coroutine for the enemy's turn
    IEnumerator EnemyTurnRoutine()
    {
        Debug.Log("--- Enemy Turn ---");
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
                Debug.Log($"[BATTLE] Enemy {i} attacked player for {enemyChar.attackPower} damage");
            }
            yield return new WaitForSeconds(attackDelay);
        }

        Debug.Log("--- Player Turn ---");
    }

    #endregion

    #region Battle Outcome

    // Coroutine for when the player is victorious
    IEnumerator VictoryRoutine()
    {
        Debug.Log("[BATTLE] Victory! All enemies defeated!");
        busy = true;
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
        busy = true;

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
        if (enemyInstances == null) return true;
        foreach (var e in enemyInstances) if (e != null) return false;
        return true;
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
        // Dùng một coroutine nhỏ để đợi một chút, cho phép các hiệu ứng
        // như animation chết hoặc gimmick hồi sinh có thời gian để chạy.
        StartCoroutine(ProcessDeathRoutine(deadCharacter));
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

                Debug.Log($"[BATTLE] Dọn dẹp kẻ địch đã chết: {deadCharacter.name}");

                // Hủy đăng ký sự kiện để tránh lỗi
                deadCharacter.OnDeath -= HandleCharacterDeath;

                // Hủy đối tượng GameObject
                Destroy(enemyInstances[i]);

                // Đánh dấu là slot đó đã trống
                enemyInstances[i] = null;

                // Nếu kẻ địch này đang được chọn làm mục tiêu, hãy bỏ chọn
                if (selectedEnemyIndex == i)
                {
                    selectedEnemyIndex = -1;
                    RefreshSelectionVisual();
                }
                break; // Thoát khỏi vòng lặp vì đã tìm thấy và xử lý
            }
        }
    }

    #endregion

}
