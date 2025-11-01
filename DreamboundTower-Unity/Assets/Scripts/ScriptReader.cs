using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using Presets;

using UnityEngine.SceneManagement;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Ink dialogue reader; advances with Space/Enter or left-click; supports both input systems.
public class ScriptReader : MonoBehaviour
{
    private Story _StoryScript; // Runtime Ink story instance.
    private Character player
    {
        get
        {
            // Kiểm tra xem GameManager và playerInstance có tồn tại KHÔNG TRƯỚC KHI lấy component
            if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
            {
                return GameManager.Instance.playerInstance.GetComponent<Character>();
            }
            return null; // Trả về null nếu playerInstance không tồn tại
        }
    }
    private PlayerData data { get { return GameManager.Instance.currentRunData.playerData; } }

    public TMP_Text dialogueBox; // Dialogue UI text.
    //public TMP_Text nameTag;     // Speaker name UI (optional).
    [Header("Speaker UI")]
    public GameObject nameTagObject; // Kéo GameObject "NameTag" (parent object) vào đây
    private TMP_Text nameTagText;
    private bool nameTagWasActive = false;

    [SerializeField]
    private GridLayoutGroup choiceHolder;

    [SerializeField]
    private Button choiceBasePrefab;

    [Header("Text Animation")]
    [SerializeField]
    private float textSpeed = 0.05f; // Delay between characters (0.01=fast, 0.1=slow)
    [SerializeField]
    private bool skipAnimation = false; // Skip text animation completely

    // Animation state variables
    private Coroutine textAnimationCoroutine;
    private string currentText = ""; // Full text being animated
    private bool isAnimating = false; // Currently showing text animation

    private EventSceneManager eventManager;

    // Initialize story and display first line.
    void Start()
    {
        // Tự động tìm Text component con của NameTag
        if (nameTagObject != null)
        {
            nameTagText = nameTagObject.GetComponentInChildren<TMP_Text>();
        }

        // Ẩn khung tên khi bắt đầu
        nameTagObject.SetActive(false);
        dialogueBox.text = ""; // Xóa text demo
    }

    // Listen for advance input each frame.
    void Update()
    {
        if (_StoryScript == null) return;

        if (IsAdvancePressed())
        {
            if (isAnimating)
            {
                SkipTextAnimation();
            }
            else
            {
                DisplayNextLine();
            }
        }
    }

    public void SetStory(TextAsset inkJson)
    {
        _StoryScript = new Story(inkJson.text);

        //====================================================================
        // STEP 2: KẾT NỐI (BIND) CÁC HÀM
        //====================================================================

        // Kết nối các hàm Tên/Lời kể
        _StoryScript.BindExternalFunction("Name", (string charName) => ChangeNameTag(charName));
        _StoryScript.BindExternalFunction("ClearName", () => ClearNameTag());

        // Kết nối các hàm Kết quả
        _StoryScript.BindExternalFunction("GainGold", (int amount) => GainGold(amount));
        _StoryScript.BindExternalFunction("GainItem", (string name) => GainItem(name));
        _StoryScript.BindExternalFunction("GainRelic", (string name) => GainRelic(name));

        _StoryScript.BindExternalFunction("HealHP", (int amount, string type) => HealHP(amount, type));
        _StoryScript.BindExternalFunction("LoseHP", (int amount, string type) => LoseHP(amount, type));
        _StoryScript.BindExternalFunction("HealMana", (int amount, string type) => HealMana(amount, type));
        _StoryScript.BindExternalFunction("LoseMana", (int amount, string type) => LoseMana(amount, type));

        _StoryScript.BindExternalFunction("GainStat", (string name, int amount) => GainStat(name, amount));
        _StoryScript.BindExternalFunction("GainRandomStat", (int numStats, int amountPerStat) => GainRandomStat(numStats, amountPerStat));

        _StoryScript.BindExternalFunction("SetFlag", (string name) => SetFlag(name));
        _StoryScript.BindExternalFunction("RemoveFlag", (string name) => RemoveFlag(name));

        _StoryScript.BindExternalFunction("AddBuff", (string name, int duration) => AddBuff(name, duration));
        _StoryScript.BindExternalFunction("AddDebuff", (string name, int duration) => AddDebuff(name, duration));

        _StoryScript.BindExternalFunction("ModifySteadfast", (int amount) => ModifySteadfast(amount));
        _StoryScript.BindExternalFunction("StartCombat", (string type) => StartCombat(type));

        _StoryScript.BindExternalFunction("GetSTR", () => GetPlayerSTR());
        _StoryScript.BindExternalFunction("GetINT", () => GetPlayerINT());
        _StoryScript.BindExternalFunction("GetRace", () => GetPlayerRace());
        _StoryScript.BindExternalFunction("HasFlag", (string flag) => HasFlag(flag));
        _StoryScript.BindExternalFunction("GetGold", () => GetGold());
        _StoryScript.BindExternalFunction("HasGold", (int amount) => HasGold(amount));
        // Bắt đầu hiển thị dòng đầu tiên
        DisplayNextLine();
    }

    // True if Space/Enter/NumpadEnter or left-click was pressed this frame.
    private bool IsAdvancePressed()
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            // Bỏ qua cú nhấp chuột này, không coi nó là "Advance"
            return false;
        }
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null && Mouse.current == null) return false;
        bool keyboardPressed = Keyboard.current != null && (
            Keyboard.current.spaceKey.wasPressedThisFrame
            || Keyboard.current.enterKey.wasPressedThisFrame
            || Keyboard.current.numpadEnterKey.wasPressedThisFrame);
        bool mousePressed = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        return keyboardPressed || mousePressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return)
            || Input.GetKeyDown(KeyCode.KeypadEnter)
            || Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    // Advance story one line and update UI; shows end marker if finished.
    public void DisplayNextLine()
    {
        if (_StoryScript.canContinue)
        {
            ClearNameTag();

            string text = _StoryScript.Continue();
            text = text?.Trim();
            currentText = text;

            HandleTags(_StoryScript.currentTags);

            // Bỏ if/else, cứ chạy animation
            StartTextAnimation(text);
        }
        else if (_StoryScript.currentChoices.Count > 0)
        {
            ClearNameTag();
            DisplayChoices();
        }
        else
        {
            Debug.Log("Kịch bản kết thúc! (Được gọi từ DisplayNextLine)");
            // Call "Đạo diễn" to finish
            // Check if we started with a debug event
            bool isDebugMode = FindFirstObjectByType<EventSceneManager>().debugEventToLoad != null;
            FindFirstObjectByType<EventSceneManager>().FinishEvent(isDebugMode); // Pass true if in debug
        }
    }

    #region Animation Coroutines
    // Start text animation coroutine
    private void StartTextAnimation(string text)
    {
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine); // Stop previous animation
        }
        
        isAnimating = true;
        textAnimationCoroutine = StartCoroutine(AnimateText(text));
    }

    // Animate text character by character
    private IEnumerator AnimateText(string text)
    {
        dialogueBox.text = "";
        
        for (int i = 0; i <= text.Length; i++)
        {
            if (!isAnimating) break; // Skip if animation was interrupted
            
            dialogueBox.text = text.Substring(0, i);
            yield return new WaitForSeconds(textSpeed);
        }
        
        isAnimating = false;
        textAnimationCoroutine = null;
    }

    // Skip text animation and show full text
    private void SkipTextAnimation()
    {
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
            textAnimationCoroutine = null;
        }
        
        dialogueBox.text = currentText; // Show complete text immediately
        isAnimating = false;
    }
    #endregion

    #region Choice Handling

    public void DisplayChoices()
    {
        if (choiceHolder.GetComponentsInChildren<Button>().Length > 0) return;

        // THÊM VÀO: MỘT DÒNG LOG TEST
        // Nếu bạn thấy log này, có nghĩa là file script CỦA BẠN ĐÃ ĐƯỢC CẬP NHẬT.
        Debug.LogWarning("!!! HÀM DISPLAYCHOICES ĐANG CHẠY (PHIÊN BẢN MỚI) !!!");

        for (int i = 0; i < _StoryScript.currentChoices.Count; i++)
        {
            var choice = _StoryScript.currentChoices[i];
            bool isDisabled = IsChoiceDisabled(choice);
            var button = CreateChoiceButton(choice.text, isDisabled);

            // THAY ĐỔI: Chúng ta sẽ "bắt" chỉ số (index)
            int choiceIndex = i;

            // Only add listener if choice is not disabled
            if (!isDisabled)
            {
                // THAY ĐỔI: Gán listener bằng chỉ số (index), KHÔNG PHẢI 'choice'
                button.onClick.AddListener(() => OnClickChoiceButton(choiceIndex));
            }
            else
            {
                // Disable the button so it can't be clicked
                button.interactable = false;
            }
        }
    }

    bool IsChoiceDisabled(Ink.Runtime.Choice choice)
    {
        // Check if choice has a "disabled" tag
        if (choice.tags != null && choice.tags.Contains("disabled"))
        {
            return true;
        }
        
        // Check if choice text contains gold cost pattern like "(20g)", "(50g)", etc.
        if (choice.text != null)
        {
            // Look for patterns like "(20g)", "(50g)", "(15g)", etc.
            var goldPattern = new System.Text.RegularExpressions.Regex(@"\((\d+)g\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var match = goldPattern.Match(choice.text);
            if (match.Success)
            {
                int goldRequired = int.Parse(match.Groups[1].Value);
                // Check if player has enough gold
                if (data != null && data.gold < goldRequired)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    Button CreateChoiceButton(string text, bool isDisabled = false)
    {
        //Instantiate the button prefab
        var choiceButton = Instantiate(choiceBasePrefab);
        choiceButton.transform.SetParent(choiceHolder.transform, false);

        //Change the text in the button prefab
        var buttonText = choiceButton.GetComponentInChildren<TMP_Text>();
        buttonText.text = text;

        // If disabled, gray out the button
        if (isDisabled)
        {
            // Get the button's image component to adjust color
            var buttonImage = choiceButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                // Darken the button (reduce alpha or use gray color)
                var colors = choiceButton.colors;
                colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray with reduced opacity
                choiceButton.colors = colors;
                
                // Set text color to gray
                if (buttonText != null)
                {
                    buttonText.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
                }
            }
        }

        return choiceButton;
    }

    // THAY THẾ HÀM NÀY:
    void OnClickChoiceButton(int choiceIndex)
    {
        if (choiceIndex < 0 || _StoryScript == null || choiceIndex >= _StoryScript.currentChoices.Count)
        {
            Debug.LogError($"Lỗi Index! choiceIndex ({choiceIndex}) không hợp lệ với currentChoices count ({_StoryScript?.currentChoices?.Count}). Bỏ qua click.");
            return; // Thoát khỏi hàm nếu index không hợp lệ
        }
        
        // Safety check: Don't allow selecting disabled choices
        var choice = _StoryScript.currentChoices[choiceIndex];
        if (IsChoiceDisabled(choice))
        {
            Debug.LogWarning($"Attempted to select disabled choice: {choice.text}");
            return; // Don't execute disabled choices
        }
        
        // XÓA HẾT MỌI THỨ, CHỈ ĐỂ LẠI:
        _StoryScript.ChooseChoiceIndex(choiceIndex);
        RefreshChoiceView();
        DisplayNextLine();

        // (Vẫn giữ lại phần sửa lỗi flow nếu bạn muốn)
        if (isAnimating)
        {
            SkipTextAnimation();
        }
    }
    void RefreshChoiceView()
    {
        if (choiceHolder != null)
        {
            foreach (var button in choiceHolder.GetComponentsInChildren<Button>())
            {
                Destroy(button.gameObject); // Clean up old choice buttons
            }
        }
    }
    #endregion

    #region NameTag Functions
    public void ChangeNameTag(string name)
    {
        nameTagObject.SetActive(true);
        nameTagText.text = name;
        nameTagWasActive = true;
    }
    public void ClearNameTag()
    {
        if (nameTagWasActive)
        {
            nameTagObject.SetActive(false);
            nameTagWasActive = false;
        }
    }
    #endregion

    #region Execute Inky Function
    public int GetGold()
    {
        if (data != null)
        {
            return data.gold;
        }
        return 0;
    }

    public bool HasGold(int amount)
    {
        if (data != null)
        {
            return data.gold >= amount;
        }
        return false;
    }

    public void GainGold(int amount)
    {
        data.gold += amount;
        // Clamp gold to 0 minimum (prevent negative gold)
        if (data.gold < 0)
        {
            data.gold = 0;
            Debug.LogWarning($"GainGold: Attempted to go below 0. Clamped to 0.");
        }
        Debug.Log($"THỰC THI: Thay đổi {amount} Vàng! (Mới: {data.gold})");
        
        // Update UI to reflect the change
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdatePlayerGoldUI(data.gold);
        }
    }

    public void GainItem(string itemName)
    {
        Debug.Log($"THỰC THI: Cố gắng nhận Item '{itemName}'!");

        // 1. Lấy GameManager và Inventory của Player
        GameManager gm = GameManager.Instance;
        Inventory playerInventory = null;

        if (gm != null && gm.playerInstance != null)
        {
            playerInventory = gm.playerInstance.GetComponent<Inventory>();
        }

        // Kiểm tra lỗi nếu không tìm thấy
        if (gm == null)
        {
            Debug.LogError($"ScriptReader.GainItem: Không tìm thấy GameManager Instance!");
            return;
        }
        if (playerInventory == null)
        {
            Debug.LogError($"ScriptReader.GainItem: Không tìm thấy component Inventory trên Player!");
            return;
        }

        // 2. Tìm đối tượng GearItem từ tên (sử dụng hàm trong GameManager)
        GearItem itemToAdd = gm.GetItemByID(itemName);
        
        // Fallback: Try finding by itemName field if GetItemByID fails
        if (itemToAdd == null && gm.allItems != null)
        {
            itemToAdd = gm.allItems.Find(i => i.itemName == itemName);
        }

        if (itemToAdd == null)
        {
            Debug.LogError($"ScriptReader.GainItem: Không tìm thấy GearItem nào có tên '{itemName}' trong database của GameManager!");
            return;
        }

        // 3. Thêm item vào PlayerData list for persistence (kept from HEAD)
        if (data != null && !data.inventoryItemIds.Contains(itemName))
        {
            data.inventoryItemIds.Add(itemName);
        }

        // 4. Thêm item vào Inventory component so it appears in UI
        bool addedSuccessfully = playerInventory.AddItem(itemToAdd);

        // Xử lý kết quả
        if (addedSuccessfully)
        {
            Debug.Log($"THỰC THI: Đã thêm Item '{itemToAdd.itemName}' vào Inventory!");
        }
        else
        {
            Debug.LogWarning($"ScriptReader.GainItem: Inventory đầy! Không thể thêm '{itemName}'.");
        }
    }

    public void GainRelic(string relicName)
    {
        // Relics are treated the same as items - reuse GainItem logic
        GainItem(relicName);
    }

    public void HealHP(int amount, string type)
    {
        // Ưu tiên 1: Tác động lên Player Instance thực tế (nếu có)
        if (player != null)
        {
            if (type == "PERCENT")
            {
                player.HealPercent(amount);
                Debug.Log($"THỰC THI (Player): Hồi {amount}% HP!");
            }
            else // FLAT
            {
                player.Heal(amount);
                Debug.Log($"THỰC THI (Player): Hồi {amount} HP!");
            }
        }
        // Ưu tiên 2: Mô phỏng trên PlayerData (khi test)
        else if (data != null) // 'data' is GameManager.Instance.currentRunData.playerData
        {
            int maxHPToUse = GetMaxHPForCalculation();
            int amountToHeal = amount;
            if (type == "PERCENT")
            {
                amountToHeal = Mathf.RoundToInt(maxHPToUse * (amount / 100f));
                Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Hồi {amount}% ({amountToHeal} flat) HP!");
            }
            else // FLAT
            {
                Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Hồi {amount} HP!");
            }
            data.currentHP = Mathf.Min(data.currentHP + amountToHeal, maxHPToUse);
            Debug.Log($"---> PlayerData HP mới: {data.currentHP} / {maxHPToUse}");
            // Update UI if GameManager is available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerHealthUI(data.currentHP, maxHPToUse);
            }
        }
        else { Debug.LogWarning($"Cannot HealHP: Player instance và PlayerData không hợp lệ."); }
    }

    public void LoseHP(int amount, string type)
    {
        // Ưu tiên 1: Player Instance
        if (player != null)
        {
            int damageAmount = amount;
            if (type == "PERCENT")
            {
                // Use Character's TakeDamagePercent which calculates based on its current maxHP
                player.TakeDamagePercent(amount);
                Debug.Log($"THỰC THI (Player): Mất {amount}% HP!");
            }
            else // FLAT
            {
                // Event/narrative damage cannot be dodged (bypassDodge = true)
                player.TakeDamage(amount, null, DamageType.Physical, false, bypassDodge: true);
                Debug.Log($"THỰC THI (Player): Mất {amount} HP!");
            }
        }
        // Ưu tiên 2: Mô phỏng trên PlayerData
        else if (data != null)
        {
            int maxHPToUse = GetMaxHPForCalculation();
            int amountToLose = amount;
            if (type == "PERCENT")
            {
                amountToLose = Mathf.RoundToInt(maxHPToUse * (amount / 100f));
                Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Mất {amount}% ({amountToLose} flat) HP!");
            }
            else { Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Mất {amount} HP!"); }
            data.currentHP = Mathf.Max(data.currentHP - amountToLose, 0);
            Debug.Log($"---> PlayerData HP mới: {data.currentHP} / {maxHPToUse}");
            // Update UI if GameManager is available
            if (GameManager.Instance != null)
            {
                GameManager.Instance.UpdatePlayerHealthUI(data.currentHP, maxHPToUse);
            }
        }
        else { Debug.LogWarning($"Cannot LoseHP: Player instance và PlayerData không hợp lệ."); }
    }

    public void HealMana(int amount, string type)
    {
        // Ưu tiên 1: Player Instance
        if (player != null)
        {
            if (type == "PERCENT")
            {
                player.RestoreManaPercent(amount);
                Debug.Log($"THỰC THI (Player): Hồi {amount}% Mana!");
            }
            else { player.RestoreMana(amount); Debug.Log($"THỰC THI (Player): Hồi {amount} Mana!"); }
        }
        // Ưu tiên 2: Mô phỏng trên PlayerData
        else if (data != null)
        {
            int maxManaToUse = GetMaxManaForCalculation();
            int amountToRestore = amount;
            if (type == "PERCENT")
            {
                amountToRestore = Mathf.RoundToInt(maxManaToUse * (amount / 100f));
                Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Hồi {amount}% ({amountToRestore} flat) Mana!");
            }
            else { Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Hồi {amount} Mana!"); }
            data.currentMana = Mathf.Min(data.currentMana + amountToRestore, maxManaToUse);
            Debug.Log($"---> PlayerData Mana mới: {data.currentMana} / {maxManaToUse}");
            // Update UI if GameManager is available
            if (GameManager.Instance != null && GameManager.Instance.playerStatusUI != null)
            {
                GameManager.Instance.playerStatusUI.UpdateMana(data.currentMana, maxManaToUse);
            }
        }
        else { Debug.LogWarning($"Cannot HealMana: Player instance và PlayerData không hợp lệ."); }
    }

    public void LoseMana(int amount, string type)
    {
        // Ưu tiên 1: Player Instance
        if (player != null)
        {
            int manaToLose = amount;
            if (type == "PERCENT")
            {
                // Use Character's UseManaPercent
                player.UseManaPercent(amount);
                Debug.Log($"THỰC THI (Player): Mất {amount}% Mana!");
            }
            else // FLAT
            {
                player.UseMana(amount);
                Debug.Log($"THỰC THI (Player): Mất {amount} Mana!");
            }
        }
        // Ưu tiên 2: Mô phỏng trên PlayerData
        else if (data != null)
        {
            int maxManaToUse = GetMaxManaForCalculation();
            int amountToLose = amount;
            if (type == "PERCENT")
            {
                amountToLose = Mathf.RoundToInt(maxManaToUse * (amount / 100f));
                Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Mất {amount}% ({amountToLose} flat) Mana!");
            }
            else { Debug.Log($"THỰC THI (PlayerData - Mô phỏng): Mất {amount} Mana!"); }
            data.currentMana = Mathf.Max(data.currentMana - amountToLose, 0);
            Debug.Log($"---> PlayerData Mana mới: {data.currentMana} / {maxManaToUse}");
            // Update UI if GameManager is available
            if (GameManager.Instance != null && GameManager.Instance.playerStatusUI != null)
            {
                GameManager.Instance.playerStatusUI.UpdateMana(data.currentMana, maxManaToUse);
            }
        }
        else { Debug.LogWarning($"Cannot LoseMana: Player instance và PlayerData không hợp lệ."); }
    }
    public void GainStat(string statName, int amount)
    {
        // Update PlayerData (persistent storage)
        switch (statName.ToUpper())
        {
            case "STR": data.currentStats.STR += amount; break;
            case "DEF": data.currentStats.DEF += amount; break;
            case "INT": data.currentStats.INT += amount; break;
            case "MANA": data.currentStats.MANA += amount; break;
            case "AGI": data.currentStats.AGI += amount; break;
            case "ALL":
                // Increase all stats by the same amount
                data.currentStats.STR += amount;
                data.currentStats.DEF += amount;
                data.currentStats.INT += amount;
                data.currentStats.MANA += amount;
                data.currentStats.AGI += amount;
                Debug.Log($"THỰC THI: All stats thay đổi +{amount}!");
                break;
            default:
                Debug.LogWarning($"Không tìm thấy stat tên: {statName}");
                return; // Early return if invalid stat name
        }

        // Also update Character component's base stats if player instance exists (so UI updates immediately)
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerCharacter != null)
            {
                switch (statName.ToUpper())
                {
                    case "STR":
                        playerCharacter.baseAttackPower += amount;
                        playerCharacter.attackPower += amount;
                        break;
                    case "DEF":
                        playerCharacter.baseDefense += amount;
                        playerCharacter.defense += amount;
                        break;
                    case "INT":
                        playerCharacter.baseIntelligence += amount;
                        playerCharacter.intelligence += amount;
                        break;
                    case "MANA":
                        playerCharacter.baseMana += amount * 5; // MANA_UNIT = 5
                        playerCharacter.mana += amount * 5;
                        playerCharacter.currentMana = Mathf.Min(playerCharacter.currentMana, playerCharacter.mana);
                        playerCharacter.UpdateManaUI();
                        break;
                    case "AGI":
                        playerCharacter.baseAgility += amount;
                        playerCharacter.agility += amount;
                        playerCharacter.UpdateDerivedStats(); // Recalculate dodge chance
                        break;
                    case "ALL":
                        playerCharacter.baseAttackPower += amount;
                        playerCharacter.baseDefense += amount;
                        playerCharacter.baseIntelligence += amount;
                        playerCharacter.baseMana += amount * 5;
                        playerCharacter.baseAgility += amount;
                        playerCharacter.attackPower += amount;
                        playerCharacter.defense += amount;
                        playerCharacter.intelligence += amount;
                        playerCharacter.mana += amount * 5;
                        playerCharacter.agility += amount;
                        playerCharacter.currentMana = Mathf.Min(playerCharacter.currentMana, playerCharacter.mana);
                        playerCharacter.UpdateDerivedStats();
                        playerCharacter.UpdateManaUI();
                        break;
                }
                
                // Force UI refresh if stats panel is visible
                var statsUIManager = FindFirstObjectByType<PlayerInfoUIManager>();
                if (statsUIManager != null && statsUIManager.statsPanel != null && statsUIManager.statsPanel.activeSelf)
                {
                    statsUIManager.UpdateStatsDisplay();
                }
            }
        }

        if (statName.ToUpper() != "ALL")
        {
            Debug.Log($"THỰC THI: {statName} thay đổi {amount}!");
        }
    }

    public void GainRandomStat(int numStats, int amountPerStat)
    {
        // List of all stats that can be randomly buffed (only combat stats)
        string[] stats = { "STR", "DEF", "INT", "AGI" };
        
        // Randomly select 'numStats' unique stats
        System.Collections.Generic.List<string> selectedStats = new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> availableStats = new System.Collections.Generic.List<string>(stats);
        
        int statsToSelect = Mathf.Min(numStats, availableStats.Count);
        
        for (int i = 0; i < statsToSelect; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, availableStats.Count);
            selectedStats.Add(availableStats[randomIndex]);
            availableStats.RemoveAt(randomIndex);
        }

        // Apply permanent stat gain for each selected stat (amount per stat is configurable)
        foreach (string stat in selectedStats)
        {
            GainStat(stat, amountPerStat);
        }

        Debug.Log($"GainRandomStat: Permanently gained +{amountPerStat} to {selectedStats.Count} random stats: {string.Join(", ", selectedStats)}");
    }

    public void SetFlag(string flagName)
    {
        // (Giả sử RunData có 'currentRunEventFlags')
        GameManager.Instance.currentRunData.currentRunEventFlags.Add(flagName);
        Debug.Log($"THỰC THI: Gắn cờ '{flagName}'!");
    }
    public void RemoveFlag(string flagName)
    {
        GameManager.Instance.currentRunData.currentRunEventFlags.Remove(flagName);
        Debug.Log($"THỰC THI: Xóa cờ '{flagName}'!");
    }

    public void AddBuff(string buffName, int duration)
    {
        var player = GameManager.Instance?.playerInstance?.GetComponent<Character>();
        if (player == null)
        {
            Debug.LogWarning($"AddBuff called but player not available. '{buffName}'");
            return;
        }
        // Map simple buff names to effects
        StatusEffects.StatusEffect effect = null;
        switch (buffName)
        {
            case "STR_PLUS_3":
                effect = new StatusEffects.StatModifierEffect("STR_PLUS", duration, dStr: 3, negative: false, eventBased: true);
                break;
            default:
                Debug.LogWarning($"Unknown buff '{buffName}', no effect applied.");
                return;
        }
        StatusEffectManager.Instance?.ApplyEffect(player, effect);
        Debug.Log($"Applied buff {buffName} for {duration} turns");
    }

    public void AddDebuff(string debuffName, int duration)
    {
        var player = GameManager.Instance?.playerInstance?.GetComponent<Character>();
        if (player == null)
        {
            Debug.LogWarning($"AddDebuff called but player not available. '{debuffName}'");
            return;
        }
        StatusEffects.StatusEffect effect = null;
        switch (debuffName)
        {
            case "POISON":
                effect = new StatusEffects.PoisonEffect(5, duration, eventBased: true);
                break;
            case "HEAL_MINUS_10":
                effect = new StatusEffects.HealBonusEffect(-10, duration, eventBased: true);
                effect.isNegative = true;
                effect.effectName = "Reduced Healing";
                break;
            case "DEF_MINUS_1":
                effect = new StatusEffects.StatModifierEffect("DEF_MINUS", duration, dDef: -1, negative: true, eventBased: true);
                Debug.Log($"[AddDebuff] Applying DEF_MINUS_1: Defense will be reduced by 1. Current defense: {player.defense}, Base defense: {player.baseDefense}");
                break;
            case "STR_MINUS_2":
                effect = new StatusEffects.StatModifierEffect("STR_MINUS", duration, dStr: -2, negative: true, eventBased: true);
                break;
            default:
                Debug.LogWarning($"Unknown debuff '{debuffName}', no effect applied.");
                return;
        }
        StatusEffectManager.Instance?.ApplyEffect(player, effect);
        Debug.Log($"Applied debuff {debuffName} for {duration} turns");
    }

    public void ModifySteadfast(int amount)
    {
        data.steadfastDurability += amount;
        Debug.Log($"THỰC THI: Steadfast thay đổi {amount}! (Mới: {data.steadfastDurability})");
    }

    public void StartCombat(string combatType)
    {
        // Kiểm tra xem eventManager đã được gán chưa
        if (eventManager == null)
        {
            Debug.LogError("ScriptReader.StartCombat: Tham chiếu đến EventSceneManager bị null!");
            return;
        }

        EnemyTemplateSO combatTemplate = null;

        // Kiểm tra combatType
        if (combatType == "RivalChild" || combatType == "ELITE") //do trong inky đặt tên là ELITE
        {
            combatTemplate = eventManager.rivalChildTemplate;
        }
        else if (combatType == "Spirit")
        {
            combatTemplate = eventManager.spiritTemplate;
        }
        // (Thêm else if cho các combat đặc biệt khác nếu cần)

        if (combatTemplate == null)
        {
            Debug.LogError($"ScriptReader.StartCombat: Không tìm thấy EnemyTemplateSO cho combatType '{combatType}'!");
            return;
        }

        // --- BẮT ĐẦU LOGIC CHUẨN BỊ COMBAT (Giống Mimic) ---

        // 1. Lấy RunData
        if (GameManager.Instance == null || GameManager.Instance.currentRunData == null)
        {
            Debug.LogError("ScriptReader.StartCombat: GameManager hoặc RunData bị null!");
            return;
        }
        var runData = GameManager.Instance.currentRunData;
        var mapData = runData.mapData;

        // 2. Set Dữ liệu Pending Enemy
        // Đây là bước quan trọng nhất
        mapData.pendingEnemyArchetypeId = combatTemplate.name; // Sẽ là "RivalChild"
        mapData.pendingEnemyKind = (int)combatTemplate.kind;     // Loại (ví dụ: Normal)

        // Lấy Tầng hiện tại (đã được MapPlayerTracker lưu khi vào Event)
        int currentFloor = mapData.pendingEnemyFloor;
        if (currentFloor <= 0) currentFloor = 1; // Fallback nếu = 0
        mapData.pendingEnemyFloor = currentFloor;

        // 3. Đảm bảo Dữ liệu Pending Node vẫn còn (để quay về)
        // (MapPlayerTracker đã set cái này khi tải EventScene, không cần set lại)
        if (mapData.pendingNodePoint.x == -1)
        {
            Debug.LogWarning("ScriptReader.StartCombat: pendingNodePoint không được set! Có thể lỗi khi quay về Map.");
        }

        // 4. Lưu game và Chuyển Scene
        RunSaveService.SaveRun(runData);
        Debug.Log($"[ScriptReader] Bắt đầu trận chiến đặc biệt: {combatTemplate.name}. Quay về Map sau khi xong.");
        SceneManager.LoadScene("MainGame"); // Tên Scene Combat của bạn
    }

public int GetPlayerSTR()
    {
        // Ưu tiên 1: Đọc từ Override Stats nếu bật
        if (GameManager.Instance != null && GameManager.Instance.overridePlayerStats)
        {
            // SỬA LỖI: Dùng customPlayerStats
            int statValue = GameManager.Instance.customPlayerStats.STR;
            Debug.Log($"Hỏi chỉ số: STR (Đọc từ Override Stats: {statValue})");
            return statValue;
        }

        // Ưu tiên 2: Đọc từ Player Instance thực tế (nếu tồn tại)
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerCharacter != null)
            {
                int statValue = playerCharacter.attackPower; // STR corresponds to attackPower
                Debug.Log($"Hỏi chỉ số: STR (Đọc từ Character Component: {statValue})");
                return statValue;
            }
        }

        // Ưu tiên 3: Đọc từ RunData (dữ liệu lưu - KÉM TIN CẬY)
        if (GameManager.Instance != null && GameManager.Instance.currentRunData != null && GameManager.Instance.currentRunData.playerData != null)
        {
            int statValue = GameManager.Instance.currentRunData.playerData.currentStats.STR;
            Debug.Log($"Hỏi chỉ số: STR (Đọc từ RunData Fallback: {statValue})");
            return statValue;
        }

        Debug.LogWarning("Không thể lấy chỉ số STR. Trả về 0.");
        return 0;
    }

    public int GetPlayerINT()
    {
        // Ưu tiên 1: Override Stats
        if (GameManager.Instance != null && GameManager.Instance.overridePlayerStats)
        {
            // SỬA LỖI: Dùng customPlayerStats
            int statValue = GameManager.Instance.customPlayerStats.INT;
            Debug.Log($"Hỏi chỉ số: INT (Đọc từ Override Stats: {statValue})");
            return statValue;
        }

        // Ưu tiên 2: Character Component
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            var playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerCharacter != null)
            {
                int statValue = playerCharacter.intelligence; //
                Debug.Log($"Hỏi chỉ số: INT (Đọc từ Character Component: {statValue})");
                return statValue;
            }
        }

        // Ưu tiên 3: RunData (KÉM TIN CẬY)
        if(GameManager.Instance != null && GameManager.Instance.currentRunData != null && GameManager.Instance.currentRunData.playerData != null)
        {
            int statValue = GameManager.Instance.currentRunData.playerData.currentStats.INT;
            Debug.Log($"Hỏi chỉ số: INT (Đọc từ RunData Fallback: {statValue})");
            return statValue;
        }

        Debug.LogWarning("Không thể lấy chỉ số INT. Trả về 0.");
        return 0;
    }

    public string GetPlayerRace()
    {
        string raceId = null;

        // Ưu tiên 1: RunData (Chế độ chơi bình thường)
        if (GameManager.Instance != null && GameManager.Instance.currentRunData != null && GameManager.Instance.currentRunData.playerData != null && !string.IsNullOrEmpty(GameManager.Instance.currentRunData.playerData.selectedRaceId))
        {
            raceId = GameManager.Instance.currentRunData.playerData.selectedRaceId;
            Debug.Log($"Hỏi chỉ số: Race (Đọc từ RunData: {raceId})");
        }
        // Ưu tiên 2: FallbackRace từ GameManager (Dành cho debug)
        else if (GameManager.Instance != null && GameManager.Instance.fallbackRace != null && !string.IsNullOrEmpty(GameManager.Instance.fallbackRace.id))
        {
            raceId = GameManager.Instance.fallbackRace.id;
            Debug.LogWarning($"Không tìm thấy Race trong RunData. Sử dụng FallbackRace từ GameManager (Debug): {raceId}");
        }

        // Xử lý raceId nếu tìm thấy (từ 1 trong 2 nguồn trên)
        if (!string.IsNullOrEmpty(raceId))
        {
            // Lấy phần tên sau dấu "_" (ví dụ: "race_human" -> "human")
            string raceName = raceId.Contains("_") ? raceId.Split('_')[1] : raceId;

            // Viết hoa chữ cái đầu
            if (raceName.Length > 0)
            {
                raceName = char.ToUpper(raceName[0]) + raceName.Substring(1).ToLower();
            }
            Debug.Log($" RaceName: {raceName}");
            return raceName; // Trả về "Human", "Demon", etc.
        }

        // Fallback cuối cùng (Nếu cả RunData và FallbackRace đều không có)
        Debug.LogWarning("Không thể lấy Race từ RunData hoặc FallbackRace. Trả về 'Human'.");
        return "Human"; // Giá trị mặc định cuối cùng
    }

    public bool HasFlag(string flagName)
    {
        // Flag chỉ tồn tại trong RunData
        if (GameManager.Instance != null && GameManager.Instance.currentRunData != null && GameManager.Instance.currentRunData.currentRunEventFlags != null)
        {
            bool flagExists = GameManager.Instance.currentRunData.currentRunEventFlags.Contains(flagName);
            Debug.Log($"Kiểm tra cờ: {flagName} (RunData trả về: {flagExists})");
            return flagExists;
        }

        Debug.LogWarning($"Không thể kiểm tra cờ {flagName} (RunData không hợp lệ). Trả về 'false'.");
        return false;
    }

    private int GetMaxHPForCalculation()
    {
        // Ưu tiên 1: Override Stats (Nhân với HP_UNIT từ GDD)
        if (GameManager.Instance != null && GameManager.Instance.overridePlayerStats)
        {
            // Assuming HP_UNIT is consistently 10 as per GDD [cite: 40, 240]
            return GameManager.Instance.customPlayerStats.HP * 10;
        }
        // Ưu tiên 2: RunData (Nhân với HP_UNIT)
        // SỬA LỖI: Đã xóa "&& data.currentStats != null"
        if (data != null)
        {
            // Assuming HP_UNIT is consistently 10
            return data.currentStats.HP * 10;
        }
        Debug.LogWarning("GetMaxHPForCalculation: Không thể lấy MaxHP từ Override hoặc RunData. Trả về 100.");
        return 100; // Giá trị mặc định nếu không lấy được
    }

    private int GetMaxManaForCalculation()
    {
        // Ưu tiên 1: Override Stats (Nhân với MANA_UNIT)
        if (GameManager.Instance != null && GameManager.Instance.overridePlayerStats)
        {
             // Assuming MANA_UNIT is consistently 5 based on previous context/GDD guess
             // (You might want a constant like GameManager.MANA_UNIT instead of hardcoding 5)
            return GameManager.Instance.customPlayerStats.MANA * 5;
        }
        // Ưu tiên 2: RunData (Nhân với MANA_UNIT)
        // SỬA LỖI: Đã xóa "&& data.currentStats != null"
        if (data != null)
        {
             // Assuming MANA_UNIT is consistently 5
            return data.currentStats.MANA * 5;
        }
        Debug.LogWarning("GetMaxManaForCalculation: Không thể lấy MaxMana từ Override hoặc RunData. Trả về 50.");
        return 50; // Giá trị mặc định
    }
    #endregion

    #region Settings
    public void SetSkipAnimation(bool skip)
    {
        skipAnimation = skip;
    }

    public void SetTextSpeed(float speed)
    {
        textSpeed = speed;
    }
    #endregion
    /// <summary>
    /// Nhận tham chiếu từ EventSceneManager để truy cập các template đặc biệt
    /// </summary>
    public void SetEventManagerReference(EventSceneManager manager)
    {
        this.eventManager = manager;
    }
    private void HandleTags(List<string> tags)
    {
        foreach (string tag in tags)
        {
            string[] parts = tag.Split(' ');
            string command = parts[0].Trim();

            switch (command)
            {
                case "Name":
                    // Lấy toàn bộ tên (ví dụ: "Name Skibidi-chan")
                    string nameString = tag.Substring(command.Length).Trim();
                    ChangeNameTag(nameString.Replace("_", " "));
                    break;
                case "End":
                    ClearNameTag();
                    break;
                    // (Tương lai: case "HEAL_HP_10": ... )
            }
        }
    }
   
}
