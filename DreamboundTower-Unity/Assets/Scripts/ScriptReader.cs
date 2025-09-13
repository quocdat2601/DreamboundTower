using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using TMPro;
using System.Collections;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Ink dialogue reader; advances with Space/Enter or left-click; supports both input systems.
public class ScriptReader : MonoBehaviour
{
    // Ink JSON exported from Ink.
    [SerializeField]
    private TextAsset _InkJsonFile;

    private Story _StoryScript; // Runtime Ink story instance.

    public TMP_Text dialogueBox; // Dialogue UI text.
    public TMP_Text nameTag;     // Speaker name UI (optional).

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
        loadStory();
        DisplayNextLine();
    }

    // Listen for advance input each frame.
    void Update()
    {
        if (IsAdvancePressed())
        {
            if (isAnimating)
            {
                SkipTextAnimation(); // First press: skip animation
            }
            else
            {
                DisplayNextLine(); // Second press: advance dialogue
            }
        }
    }

    // True if Space/Enter/NumpadEnter or left-click was pressed this frame.
    private bool IsAdvancePressed()
    {
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

    // Create story from JSON and changeName base on story.
    void loadStory()
    {
        _StoryScript = new Story(_InkJsonFile.text);
        _StoryScript.BindExternalFunction("Name", (string charName) => ChangeNameTag(charName)); 
    }

    // Advance story one line and update UI; shows end marker if finished.
    public void DisplayNextLine()
    {
        if (_StoryScript.canContinue)
        {
            string text = _StoryScript.Continue();
            text = text?.Trim();
            currentText = text;
            
            if (skipAnimation)
            {
                dialogueBox.text = text; // Show immediately if animation disabled
            }
            else
            {
                StartTextAnimation(text); // Start character-by-character animation
            }
        }
        else if (_StoryScript.currentChoices.Count > 0)
        {
            DisplayChoices(); // Show choice buttons when available
        }
    }

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

    public void DisplayChoices()
    {
        if (choiceHolder.GetComponentsInChildren<Button>().Length > 0) return; // Already showing choices
        for (int i = 0; i < _StoryScript.currentChoices.Count; i++)
        {
            var choice = _StoryScript.currentChoices[i];
            var button = CreateChoiceButton(choice.text);

            button.onClick.AddListener(() => OnClickChoiceButton(choice));
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

    void OnClickChoiceButton(Choice choice)
    {
        _StoryScript.ChooseChoiceIndex(choice.index);
        RefreshChoiceView();
        DisplayNextLine();
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

    public void ChangeNameTag(string name)
    {
        string speakerName = name;
        nameTag.text = speakerName;
    }

    // Public method to toggle animation (for settings menu)
    public void SetSkipAnimation(bool skip)
    {
        skipAnimation = skip;
    }

    // Public method to set text speed (for settings menu)
    public void SetTextSpeed(float speed)
    {
        textSpeed = speed;
    }
}
