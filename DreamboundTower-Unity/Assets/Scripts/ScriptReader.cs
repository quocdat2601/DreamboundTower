using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Ink dialogue reader; advances with Space/Enter or left-click; supports both input systems.
public class ScriptReader : MonoBehaviour
{
    private Story _StoryScript; // Runtime Ink story instance.

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
        _StoryScript.BindExternalFunction("LoseMana", (int amount) => LoseMana(amount));

        _StoryScript.BindExternalFunction("GainStat", (string name, int amount) => GainStat(name, amount));
        _StoryScript.BindExternalFunction("GainRandomStat", (int amount) => GainRandomStat(amount));

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
            // ĐÂY LÀ KẾT THÚC KỊCH BẢN
            Debug.Log("Kịch bản kết thúc! (Được gọi từ DisplayNextLine)");
            // Tương lai: Gọi EventSceneManager.Instance.FinishEvent()
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
            var button = CreateChoiceButton(choice.text);

            // THAY ĐỔI: Chúng ta sẽ "bắt" chỉ số (index)
            int choiceIndex = i;

            // THAY ĐỔI: Gán listener bằng chỉ số (index), KHÔNG PHẢI 'choice'
            button.onClick.AddListener(() => OnClickChoiceButton(choiceIndex));
        }
    }

    Button CreateChoiceButton(string text)
    {
        //Instantiate the button prefab
        var choiceButton = Instantiate(choiceBasePrefab);
        choiceButton.transform.SetParent(choiceHolder.transform, false);

        //Change the text in the button prefab
        var buttonText = choiceButton.GetComponentInChildren<TMP_Text>();
        buttonText.text = text;

        return choiceButton;
    }

    // THAY THẾ HÀM NÀY:
    void OnClickChoiceButton(int choiceIndex)
    {
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
    public void HealMana(int amount)
    {
        Debug.LogError($"THỰC THI (HÀM MỚI): Hồi {amount} Mana!");
        // Tương lai: GameManager.Instance.Player.RestoreMana(amount);
    }

    public void SetFlag(string flagName)
    {
        Debug.LogError($"THỰC THI (HÀM MỚI): Gắn cờ {flagName}!");
        // Tương lai: GameManager.Instance.currentRunData.currentRunEventFlags.Add(flagName);
    }
    public void GainGold(int amount)
    {
        Debug.LogError($"THỰC THI: Thay đổi {amount} Vàng!");
        // Tương lai: GameManager.Instance.PlayerData.AddGold(amount);
    }

    public void GainItem(string itemName)
    {
        Debug.LogError($"THỰC THI: Nhận Item '{itemName}'!");
        // Tương lai: Inventory.Instance.AddItem(itemName);
    }

    public void GainRelic(string relicName)
    {
        Debug.LogError($"THỰC THI: Nhận Relic '{relicName}'!");
        // Tương lai: Inventory.Instance.AddRelic(relicName);
    }

    public void HealHP(int amount, string type)
    {
        if (type == "PERCENT")
        {
            Debug.LogError($"THỰC THI: Hồi {amount}% HP!");
            // Tương lai: GameManager.Instance.Player.HealPercent(amount);
        }
        else // Mặc định là "FLAT"
        {
            Debug.LogError($"THỰC THI: Hồi {amount} HP!");
            // Tương lai: GameManager.Instance.Player.HealFlat(amount);
        }
    }

    public void LoseHP(int amount, string type)
    {
        if (type == "PERCENT")
        {
            Debug.LogError($"THỰC THI: Mất {amount}% HP!");
            // Tương lai: GameManager.Instance.Player.TakeDamagePercent(amount);
        }
        else // Mặc định là "FLAT"
        {
            Debug.LogError($"THỰC THI: Mất {amount} HP!");
            // Tương lai: GameManager.Instance.Player.TakeDamageFlat(amount);
        }
    }

    public void HealMana(int amount, string type)
    {
        if (type == "PERCENT")
        {
            Debug.LogError($"THỰC THI: Hồi {amount}% Mana!");
            // Tương lai: GameManager.Instance.Player.RestoreManaPercent(amount);
        }
        else // Mặc định là "FLAT"
        {
            Debug.LogError($"THỰC THI: Hồi {amount} Mana!");
            // Tương lai: GameManager.Instance.Player.RestoreManaFlat(amount);
        }
    }

    public void LoseMana(int amount)
    {
        Debug.LogError($"THỰC THI: Mất {amount} Mana!");
        // Tương lai: GameManager.Instance.Player.LoseMana(amount);
    }

    public void GainStat(string statName, int amount)
    {
        Debug.LogError($"THỰC THI: Nhận {amount} {statName}!");
        // Tương lai: GameManager.Instance.PlayerData.ModifyStat(statName, amount);
    }

    public void GainRandomStat(int amount)
    {
        Debug.LogError($"THỰC THI: Nhận {amount} chỉ số ngẫu nhiên!");
        // Tương lai: GameManager.Instance.PlayerData.ModifyRandomStat(amount);
    }

    public void RemoveFlag(string flagName)
    {
        Debug.LogError($"THỰC THI: Xóa cờ '{flagName}'!");
        // Tương lai: GameManager.Instance.currentRunData.currentRunEventFlags.Remove(flagName);
    }

    public void AddBuff(string buffName, int duration)
    {
        Debug.LogError($"THỰC THI: Thêm Buff '{buffName}' trong {duration} lượt!");
        // Tương lai: GameManager.Instance.Player.AddBuff(buffName, duration);
    }

    public void AddDebuff(string debuffName, int duration)
    {
        Debug.LogError($"THỰC THI: Thêm Debuff '{debuffName}' trong {duration} lượt!");
        // Tương lai: GameManager.Instance.Player.AddDebuff(debuffName, duration);
    }

    public void ModifySteadfast(int amount)
    {
        Debug.LogError($"THỰC THI: Thay đổi Steadfast {amount}!");
        // Tương lai: GameManager.Instance.PlayerData.ModifySteadfast(amount);
    }

    public void StartCombat(string combatType)
    {
        Debug.LogError($"THỰC THI: BẮT ĐẦU TRẬN CHIẾN LOẠI '{combatType}'!");
        // Tương lai: EventSceneManager.Instance.StartCombat(combatType);
    }
    // (Tạm thời chúng ta sẽ return giá trị giả lập (mock))
    // (Tương lai: Bạn sẽ thay thế 'return 12;' bằng 'return GameManager.Instance.currentRunData.playerData.STR;')

    public int GetPlayerSTR()
    {
        Debug.Log("Hỏi chỉ số: STR (Giả lập trả về 12)");
        return 12;
        // TƯƠNG LAI: return GameManager.Instance.currentRunData.playerData.STR;
    }

    public int GetPlayerINT()
    {
        Debug.Log("Hỏi chỉ số: INT (Giả lập trả về 12)");
        return 12;
        // TƯƠNG LAI: return GameManager.Instance.currentRunData.playerData.INT;
    }

    public string GetPlayerRace()
    {
        Debug.Log("Hỏi chỉ số: Race (Giả lập trả về 'Demon')");
        return "Demon";
        // TƯƠNG LAI: return GameManager.Instance.currentRunData.playerData.Race.ToString();
    }

    public bool HasFlag(string flagName)
    {
        Debug.Log($"Kiểm tra cờ: {flagName} (Giả lập trả về 'true')");
        return true;
        // TƯƠNG LAI: return GameManager.Instance.currentRunData.currentRunEventFlags.Contains(flagName);
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
