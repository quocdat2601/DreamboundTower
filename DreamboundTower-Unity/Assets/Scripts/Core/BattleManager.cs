// BattleManager.cs
using Map;
using Presets;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Battle system manager for turn-based combat
/// </summary>
public class BattleManager : MonoBehaviour
{
    #region Variables

    [Header("Slots (UI Transforms)")]
    public Transform playerSlot;       // assign PlayerSlot (Transform)
    public Transform[] enemySlots;     // assign EnemySlot transforms

    [Header("Prefabs")]
    public GameObject playerPrefab;    // UI prefab for player
    public GameObject enemyPrefab;     // UI prefab for enemies

    [Header("Combat")]
    public float attackDelay = 0.35f;
    [Tooltip("Final HP = cfg.enemyStats.HP * hpUnit. Set to 1 if your templates already store absolute HP.")]
    public int hpUnit = 10;

    // --- UPDATED SECTION ---
    [Header("Debug / Player Overrides")]
    [Tooltip("Default Race data if no GameManager is found")]
    public RacePresetSO fallbackRace;
    [Tooltip("Default Class data if no GameManager is found")]
    public ClassPresetSO fallbackClass;
    [Tooltip("If checked, custom stats below will be used instead of fallback Race/Class stats")]
    public bool overridePlayerStats = false;
    public StatBlock customPlayerStats;
    // -----------------------

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
    public Slider playerHealthBar;      // Drag the big health bar from PlayerStatusSection here
    public BattleUIManager uiManager;   // Assign your UIManager here
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
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(false);
        }
        // Initialize the UI Manager
        if (uiManager != null)
        {
            uiManager.Initialize(this);
        }

        SpawnPlayer();
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
        // Priority 1: Find the player instance from GameManager
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            playerInstance = GameManager.Instance.playerInstance;
            playerInstance.transform.SetParent(playerSlot, false);
            playerInstance.transform.localPosition = Vector3.zero;
            playerInstance.transform.localScale = Vector3.one;
            playerInstance.SetActive(true);

            playerCharacter = playerInstance.GetComponent<Character>();
            Debug.Log("[BATTLE] Player Spawned from GameManager.");
        }
        // Priority 2: Spawn a default player for testing purposes
        else
        {
            Debug.LogWarning("Player instance from GameManager not found. Spawning a default player for testing.");
            if (playerPrefab == null || playerSlot == null) return;
            if (fallbackRace == null || fallbackClass == null)
            {
                Debug.LogError("Fallback Race/Class is not set in BattleManager Inspector!");
                return;
            }

            playerInstance = Instantiate(playerPrefab, playerSlot, false);
            playerCharacter = playerInstance.GetComponent<Character>();
            var playerSkills = playerInstance.GetComponent<PlayerSkills>();
            var playerImage = playerInstance.GetComponent<Image>();

            // --- NEW OVERRIDE LOGIC ---
            // Always get visuals and skills from fallback SOs
            Sprite characterSprite = null;
            switch (fallbackClass.id)
            {
                case "class_cleric": characterSprite = fallbackRace.clericSprite; break;
                case "class_mage": characterSprite = fallbackRace.mageSprite; break;
                case "class_warrior": characterSprite = fallbackRace.warriorSprite; break;
                case "class_rogue": characterSprite = fallbackRace.rogueSprite; break;
            }
            if (playerImage != null) playerImage.sprite = characterSprite;

            if (playerSkills != null)
            {
                playerSkills.LearnSkills(fallbackRace, fallbackClass);
            }

            // Override stats if requested
            if (overridePlayerStats)
            {
                playerCharacter.baseMaxHP = customPlayerStats.HP;
                playerCharacter.baseAttackPower = customPlayerStats.STR;
                playerCharacter.baseDefense = customPlayerStats.DEF;
                playerCharacter.baseMana = customPlayerStats.MANA;
                playerCharacter.baseIntelligence = customPlayerStats.INT;
                playerCharacter.baseAgility = customPlayerStats.AGI;
                Debug.LogWarning("[BATTLE] Player spawned with CUSTOM OVERRIDE stats on fallback model.");
            }
            else // Otherwise, use stats from fallback SOs
            {
                StatBlock baseStats = fallbackRace.baseStats;
                playerCharacter.baseMaxHP = baseStats.HP;
                playerCharacter.baseAttackPower = baseStats.STR;
                playerCharacter.baseDefense = baseStats.DEF;
                playerCharacter.baseMana = baseStats.MANA;
                playerCharacter.baseIntelligence = baseStats.INT;
                playerCharacter.baseAgility = baseStats.AGI;
                Debug.LogWarning($"[BATTLE] Player spawned with FALLBACK stats (Race: {fallbackRace.displayName}, Class: {fallbackClass.displayName}).");
            }
            playerCharacter.ResetToBaseStats();
        }

        // --- COMMON ACTIONS AFTER GETTING A PLAYER INSTANCE ---
        if (playerInstance != null)
        {
            // Connect UI and Log stats/skills
            if (playerCharacter != null && playerHealthBar != null)
            {
                playerCharacter.hpSlider = playerHealthBar;
                playerCharacter.UpdateHPUI();
            }

            PlayerSkills skills = playerInstance.GetComponent<PlayerSkills>();
            if (uiManager != null && skills != null)
            {
                uiManager.CreatePlayerSkillIcons(skills);
            }
            if (playerCharacter != null)
            {
                Debug.Log($"[BATTLE] Player Stats: HP={playerCharacter.maxHP}, STR={playerCharacter.attackPower}, DEF={playerCharacter.defense}, MANA={playerCharacter.mana}, INT={playerCharacter.intelligence}, AGI={playerCharacter.agility}");
                if (skills != null)
                {
                    foreach (var pSkill in skills.passiveSkills) Debug.Log($"[BATTLE] Player has Passive: {pSkill.displayName}");
                    foreach (var aSkill in skills.activeSkills) Debug.Log($"[BATTLE] Player has Active: {aSkill.displayName}");
                }
            }
        }
    }
    //Get baseStats of player
    public Character GetPlayerCharacter()
    {
        return playerCharacter;
    }

    // Spawns enemies based on overrides or data passed from the map
    void SpawnEnemies()
    {
        if (enemySlots == null || enemySlots.Length == 0) return;

        // Khởi tạo mảng instance với kích thước tối đa
        enemyInstances = new GameObject[enemySlots.Length];

        // --- Tải dữ liệu kẻ địch từ Map (không đổi) ---
        var cfg = Resources.Load<CombatConfigSO>("CombatConfig");
        if (cfg == null)
        {
            int kind, hp, str, def, mana, intel, agi, absFloor; string arch;
            if (Map.MapTravel.TryReadAndClearPendingEnemy(out kind, out hp, out str, out def, out mana, out intel, out agi, out absFloor, out arch))
            {
                cfg = ScriptableObject.CreateInstance<CombatConfigSO>();
                cfg.enemyKind = (EnemyKind)kind;
                cfg.enemyStats.HP = hp; cfg.enemyStats.STR = str; cfg.enemyStats.DEF = def;
                cfg.enemyStats.MANA = mana; cfg.enemyStats.INT = intel; cfg.enemyStats.AGI = agi;
                cfg.absoluteFloor = absFloor; cfg.enemyArchetypeId = arch;
                Debug.Log($"[BATTLE] Loaded enemy from PlayerPrefs fallback: floor={absFloor}, kind={(EnemyKind)kind}");
            }
            else
            {
                Debug.LogWarning("[BATTLE] CombatConfig not found and no pending enemy in PlayerPrefs. Use inspector overrides if desired.");
            }
        }
        int enemyCount = 1;
        EnemyKind encounterKind = EnemyKind.Normal;

        // Ưu tiên 1 & 2: Ghi đè (Override)
        if (overrideUseCustomStats || overrideUseTemplate)
        {
            encounterKind = overrideUseTemplate ? overrideTemplate.kind : overrideKind;
        }
        // Ưu tiên 3: Dữ liệu từ Map
        else if (cfg != null)
        {
            encounterKind = cfg.enemyKind;
        }

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

        // --- Vòng lặp tạo kẻ địch (đã được tái cấu trúc) ---
        for (int i = 0; i < enemyCount; i++)
        {
            if (enemyPrefab == null || enemySlots[i] == null) continue;

            var enemyGO = Instantiate(enemyPrefab, enemySlots[i], false);
            enemyGO.transform.localPosition = Vector2.zero;
            enemyGO.transform.localScale = Vector3.one;
            enemyGO.name = "Enemy_" + i;
            enemyInstances[i] = enemyGO;

            // Logic để lấy đúng template và stats (tương tự code cũ của bạn)
            EnemyTemplateSO finalTemplate = null;
            StatBlock finalStats;

            if (overrideUseCustomStats)
            {
                finalStats = overrideStats;
                ApplyEnemyData(enemyGO, finalStats, null);
            }
            else if (overrideUseTemplate && overrideTemplate != null)
            {
                finalTemplate = overrideTemplate;
                finalStats = finalTemplate.GetStatsAtFloor(overrideFloor);
                ApplyEnemyData(enemyGO, finalStats, finalTemplate);
            }
            else if (cfg != null)
            {
                finalTemplate = FindTemplateByArchetype(cfg.enemyArchetypeId);
                if (finalTemplate != null)
                {
                    finalStats = finalTemplate.GetStatsAtFloor(cfg.absoluteFloor);
                    ApplyEnemyData(enemyGO, finalStats, finalTemplate);
                }
                else
                {
                    Debug.LogError($"[BATTLE] Could not find EnemyTemplateSO: {cfg.enemyArchetypeId}");
                }
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

    /// <summary>
    /// Applies stats and a random sprite from a template to an enemy GameObject.
    /// </summary>
    void ApplyEnemyData(GameObject enemyGO, StatBlock stats, EnemyTemplateSO template)
    {
        if (enemyGO == null) return;

        // Gán hình ảnh ngẫu nhiên từ template (nếu có)
        if (template != null && template.sprites != null && template.sprites.Count > 0)
        {
            Image enemyImage = enemyGO.GetComponent<Image>();
            if (enemyImage != null)
            {
                enemyImage.sprite = template.sprites[Random.Range(0, template.sprites.Count)];
            }
        }

        // Gán chỉ số cho component Character
        var ch = enemyGO.GetComponent<Character>();
        if (ch != null)
        {
            ch.baseMaxHP = stats.HP * hpUnit;
            ch.baseAttackPower = stats.STR;
            ch.baseDefense = stats.DEF;
            ch.baseMana = stats.MANA;
            ch.baseIntelligence = stats.INT;
            ch.baseAgility = stats.AGI;
            ch.ResetToBaseStats();
            ch.currentHP = ch.maxHP;
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
        // Trước khi tải lại, nếu có người chơi từ GameManager, hãy hủy nó đi
        // để tránh tạo ra 2 người chơi ở lần tải lại tiếp theo
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            // Quan trọng: Chỉ hủy instance trong scene, không hủy GameManager
            Destroy(GameManager.Instance.playerInstance);
            GameManager.Instance.playerInstance = null;
        }

        // Tải lại scene hiện tại
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

        if (DidEnemyDie(enemyIndex))
        {
            enemyInstances[enemyIndex] = null;
            selectedEnemyIndex = -1;
            RefreshSelectionVisual();
        }

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
            target.TakeDamage(damage);
            Debug.Log($"[BATTLE] Player used {skill.displayName} on {target.name} for {damage} damage.");
        }

        yield return new WaitForSeconds(attackDelay);

        if (DidEnemyDie(enemyIndex))
        {
            enemyInstances[enemyIndex] = null;
            selectedEnemyIndex = -1;
            RefreshSelectionVisual();
        }

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
                yield return StartCoroutine(DefeatRoutine());
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

        if (playerCharacter != null && playerCharacter.currentHP <= 0)
        {
            yield return StartCoroutine(DefeatRoutine());
            yield break;
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
        // TODO: Show victory screen, give rewards
        yield return new WaitForSeconds(2f);

        MapTravel.MarkBattleCompleted();
        RestorePersistentPlayer();
        MapTravel.ReturnToMap();
    }

    // Coroutine for when the player is defeated
    IEnumerator DefeatRoutine()
    {
        Debug.Log("[BATTLE] Player died. Game over.");
        busy = true;
        // Kích hoạt Defeat Panel
        if (defeatPanel != null)
        {
            defeatPanel.SetActive(true); // <-- THÊM DÒNG NÀY
        }
        // TODO: Show defeat screen, handle game over logic
        yield return new WaitForSeconds(2f);
        RestorePersistentPlayer();
        // SceneManager.LoadScene("MainMenu"); // Example
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
        DontDestroyOnLoad(persistentPlayer);
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
}