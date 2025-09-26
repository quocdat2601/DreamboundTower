// BattleManager.cs
using System.Collections;
using System.Linq;
using UnityEngine;
using Presets;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Map;

/// <summary>
/// Battle system manager for turn-based combat
/// 
/// USAGE:
/// - Manages battle flow and turn order
/// - Handles player and enemy actions
/// - Manages battle UI and visual feedback
/// - Integrates with equipment system for stat bonuses
/// 
/// SETUP:
/// 1. Attach to battle scene GameObject
/// 2. Assign player and enemy slot transforms
/// 3. Assign player and enemy prefabs
/// 4. System automatically manages battle flow
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("Slots (UI Transforms)")]
    public Transform playerSlot;      // assign PlayerSlot (Transform)
    public Transform[] enemySlots;    // assign EnemySlot transforms

    [Header("Prefabs")]
    public GameObject playerPrefab;   // UI prefab for player
    public GameObject enemyPrefab;    // UI prefab for enemies

    [Header("Combat")]
    public float attackDelay = 0.35f;
    [Tooltip("Final HP = cfg.enemyStats.HP * hpUnit. Set to 1 if your templates already store absolute HP.")]
    public int hpUnit = 10;
    [Header("Debug / Overrides")]
    [Tooltip("If true, combat ignores map payload and uses the template below.")]
    public bool overrideUseTemplate = false;
    [Tooltip("Template to sample stats from.")]
    public Presets.EnemyTemplateSO overrideTemplate;
    [Tooltip("Floor used when sampling from template.")]
    public int overrideFloor = 1;

    [Tooltip("If true, use the custom stats and kind below (bypasses template and map payload).")]
    public bool overrideUseCustomStats = false;
    public Presets.EnemyKind overrideKind = Presets.EnemyKind.Normal;
    public Presets.StatBlock overrideStats;
    
    [Header("UI References")]
    public Slider playerHealthBar;  // Drag the big health bar from PlayerStatusSection here

    // runtime
    private GameObject playerInstance;
    private GameObject[] enemyInstances;

    // state & selection
    private int selectedEnemyIndex = -1;
    private bool playerTurn = true;
    private bool busy = false;

    void Start()
    {
        SpawnPlayer();
        SpawnEnemies();
        AutoBindEnemySlotButtons();
        RefreshSelectionVisual();
    }

    #region Spawning
    void SpawnPlayer()
    {
        if (playerPrefab == null || playerSlot == null) return;
        playerInstance = Instantiate(playerPrefab, playerSlot, false);
        var rect = playerInstance.GetComponent<RectTransform>();
        if (rect != null) rect.anchoredPosition = Vector2.zero;
        playerInstance.transform.localScale = Vector3.one;
        
        // Connect the big health bar to the player's Character component
        var playerChar = playerInstance.GetComponent<Character>();
        if (playerChar != null && playerHealthBar != null)
        {
            playerChar.hpSlider = playerHealthBar;
        }
        else if (playerChar != null && playerHealthBar == null)
        {
            Debug.LogWarning("Player Health Bar is not assigned in BattleManager!");
        }
    }

    void SpawnEnemies()
    {
        if (enemySlots == null) return;
        enemyInstances = new GameObject[enemySlots.Length];

        // Load resolved combat config written by the Map scene
        var cfg = Resources.Load<CombatConfigSO>("CombatConfig");
        if (cfg == null)
        {
            // try PlayerPrefs fallback
            int kind, hp, str, def, mana, intel, agi, absFloor; string arch;
            if (Map.MapTravel.TryReadAndClearPendingEnemy(out kind, out hp, out str, out def, out mana, out intel, out agi, out absFloor, out arch))
            {
                cfg = ScriptableObject.CreateInstance<CombatConfigSO>();
                cfg.enemyKind = (Presets.EnemyKind)kind;
                cfg.enemyStats.HP = hp; cfg.enemyStats.STR = str; cfg.enemyStats.DEF = def;
                cfg.enemyStats.MANA = mana; cfg.enemyStats.INT = intel; cfg.enemyStats.AGI = agi;
                cfg.absoluteFloor = absFloor; cfg.enemyArchetypeId = arch;
                Debug.Log($"[BATTLE] Loaded enemy from PlayerPrefs fallback: floor={absFloor}, kind={(Presets.EnemyKind)kind}, HP={hp} STR={str} DEF={def}");
            }
            else
            {
                Debug.LogWarning("[BATTLE] CombatConfig not found in Resources and no pending enemy in PlayerPrefs. Use inspector overrides if desired.");
            }
        }

        for (int i = 0; i < enemySlots.Length; i++)
        {
            if (enemyPrefab == null || enemySlots[i] == null) continue;
            var e = Instantiate(enemyPrefab, enemySlots[i], false);
            var rect = e.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = Vector2.zero;
            e.transform.localScale = Vector3.one;
            e.name = "Enemy_" + i;
            enemyInstances[i] = e;

            // Decision order: custom stats → template → map payload
            if (overrideUseCustomStats)
            {
                var fake = ScriptableObject.CreateInstance<CombatConfigSO>();
                fake.enemyKind = overrideKind;
                fake.enemyStats = overrideStats;
                fake.absoluteFloor = Mathf.Max(1, overrideFloor);
                fake.enemyArchetypeId = "OverrideCustom";
                ApplyEnemyStatsFromConfig(e, fake);
            }
            else if (overrideUseTemplate && overrideTemplate != null)
            {
                var fake = ScriptableObject.CreateInstance<CombatConfigSO>();
                fake.enemyKind = overrideTemplate.kind;
                fake.absoluteFloor = Mathf.Max(1, overrideFloor);
                fake.enemyArchetypeId = overrideTemplate.name;
                fake.enemyStats = overrideTemplate.GetStatsAtFloor(fake.absoluteFloor);
                ApplyEnemyStatsFromConfig(e, fake);
            }
            else if (cfg != null)
            {
                ApplyEnemyStatsFromConfig(e, cfg);
            }
        }
    }
    #endregion

    void ApplyEnemyStatsFromConfig(GameObject enemyGO, CombatConfigSO cfg)
    {
        if (enemyGO == null || cfg == null) return;
        var ch = enemyGO.GetComponent<Character>();
        if (ch == null)
        {
            Debug.LogWarning("[BATTLE] Enemy prefab missing Character component. Stats not applied.");
            return;
        }

        // Map preset stats → Character fields (simple mapping: HP→maxHP, STR→attack, DEF→defense)
        int scaledHP = Mathf.Max(1, cfg.enemyStats.HP * Mathf.Max(1, hpUnit));
        ch.baseMaxHP = scaledHP;
        ch.baseAttackPower = Mathf.Max(0, cfg.enemyStats.STR);
        ch.baseDefense = Mathf.Max(0, cfg.enemyStats.DEF);

        ch.ResetToBaseStats();
        ch.currentHP = ch.maxHP;

        // Full stat dump for verification
        Debug.Log(
            $"[BATTLE] Enemy init @ floor {cfg.absoluteFloor} | Kind={cfg.enemyKind} | Archetype={cfg.enemyArchetypeId}\n" +
            $"RAW   HP={cfg.enemyStats.HP} STR={cfg.enemyStats.STR} DEF={cfg.enemyStats.DEF} MANA={cfg.enemyStats.MANA} INT={cfg.enemyStats.INT} AGI={cfg.enemyStats.AGI}\n" +
            $"FINAL HP={ch.maxHP} (hpUnit={hpUnit}) ATK={ch.attackPower} DEF={ch.defense}");
    }

    #region Selection UI
    // auto-add Button listeners for enemy slots so clicks call SelectEnemy(index)
    void AutoBindEnemySlotButtons()
    {
        if (enemySlots == null) return;

        for (int i = 0; i < enemySlots.Length; i++)
        {
            var slot = enemySlots[i];
            if (slot == null) continue;

            // ensure there's an Image (Button requires a Graphic)
            var img = slot.GetComponent<Image>();
            if (img == null) img = slot.gameObject.AddComponent<Image>();

            // add Button if missing
            var btn = slot.GetComponent<Button>();
            if (btn == null) btn = slot.gameObject.AddComponent<Button>();

            int idx = i; // capture for closure
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SelectEnemy(idx));
        }
    }

    // Called when player clicks a slot (or from code)
    public void SelectEnemy(int index)
    {
        if (!playerTurn || busy) return;
        if (enemyInstances == null) return;
        if (index < 0 || index >= enemyInstances.Length) return;
        if (enemyInstances[index] == null) return;

        selectedEnemyIndex = index;
        RefreshSelectionVisual();
    }

    // subtle visual feedback: minimal color tint for selected target
    void RefreshSelectionVisual()
    {
        if (enemySlots == null) return;
        for (int i = 0; i < enemySlots.Length; i++)
        {
            var slotImg = enemySlots[i]?.GetComponent<Image>();
            if (slotImg == null) continue;
            
            if (i == selectedEnemyIndex)
            {
                // Selected target: very subtle warm tint
                slotImg.color = new Color(1f, 0.95f, 0.8f, 0.3f); // Very subtle warm tint
            }
            else
            {
                // Unselected targets: transparent (no background)
                slotImg.color = new Color(1f, 1f, 1f, 0f); // Completely transparent
            }
        }
    }
    #endregion

    #region Player Attack (button)
    // call this from your Attack button OnClick
    public void OnAttackButton()
    {
        if (!playerTurn || busy) return;
        if (playerInstance == null) return; // Player is dead
        if (selectedEnemyIndex < 0 || enemyInstances == null) return;
        if (selectedEnemyIndex >= enemyInstances.Length) return;
        if (enemyInstances[selectedEnemyIndex] == null) return;

        StartCoroutine(PlayerAttackRoutine(selectedEnemyIndex));
    }

    IEnumerator PlayerAttackRoutine(int enemyIndex)
    {
        busy = true;

        var target = enemyInstances[enemyIndex];
        if (target != null)
        {
            var playerChar = playerInstance.GetComponent<Character>();
            var targetChar = target.GetComponent<Character>();
            if (playerChar != null && targetChar != null)
            {
                playerChar.Attack(targetChar);
                Debug.Log($"[BATTLE] Player attacked {targetChar.name} for {playerChar.attackPower} damage");
            }
        }

        // small delay for feedback
        yield return new WaitForSeconds(attackDelay);

        // if enemy died, clear reference and deselect
        if (enemyInstances[enemyIndex] == null ||
            (enemyInstances[enemyIndex].GetComponent<Character>() != null &&
             enemyInstances[enemyIndex].GetComponent<Character>().currentHP <= 0))
        {
            enemyInstances[enemyIndex] = null;
            selectedEnemyIndex = -1;
        }

        RefreshSelectionVisual();

        // victory check
        if (AllEnemiesDead())
        {
            Debug.Log("[BATTLE] Victory! All enemies defeated!");
            // mark completed node and return to map
            MapTravel.MarkBattleCompleted();
            MapTravel.ReturnToMap();
            busy = false;
            yield break;
        }

        // enemy turn (only if player is still alive)
        if (playerInstance != null)
        {
            playerTurn = false;
            yield return new WaitForSeconds(0.2f);
            yield return StartCoroutine(EnemyTurnRoutine());
            playerTurn = true;
        }
        busy = false;
    }
    #endregion

    #region Enemy Turn
    IEnumerator EnemyTurnRoutine()
    {
        for (int i = 0; i < (enemyInstances?.Length ?? 0); i++)
        {
            var e = enemyInstances[i];
            if (e == null) continue;

            // Check if player is still alive before enemy attacks
            if (playerInstance == null)
            {
                Debug.Log("[BATTLE] Player is dead, skipping enemy attacks.");
                yield break;
            }

            var enemyChar = e.GetComponent<Character>();
            var playerChar = playerInstance.GetComponent<Character>();
            if (enemyChar != null && playerChar != null)
            {
                // enemy attacks player
                enemyChar.Attack(playerChar);
                Debug.Log($"[BATTLE] Enemy {i} attacked player for {enemyChar.attackPower} damage");
            }
        }

        yield return new WaitForSeconds(attackDelay);

        // player death check
        if (playerInstance == null)
        {
            Debug.Log("[BATTLE] Player died. Game over.");
            yield break;
        }

        var playerCharCheck = playerInstance.GetComponent<Character>();
        if (playerCharCheck != null && playerCharCheck.currentHP <= 0)
        {
            Debug.Log("[BATTLE] Player died. Game over.");
            yield break;
        }
    }
    #endregion

    bool AllEnemiesDead()
    {
        if (enemyInstances == null) return true;
        foreach (var e in enemyInstances) if (e != null) return false;
        return true;
    }
}
