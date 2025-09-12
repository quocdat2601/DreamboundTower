using UnityEngine;
using UnityEngine.UI;
using Ink.Runtime;
using TMPro;
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
    // Initialize story.
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
            DisplayNextLine();
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
        if (_StoryScript.canContinue) //CHECKING IF THERE IS CONTENT TO GO THROUGH
        {
            string text = _StoryScript.Continue(); //Get next line
            text = text?.Trim(); //Remove white space from text
            dialogueBox.text = text; //Display new text
        }
        else if (_StoryScript.currentChoices.Count > 0)
        {
            DisplayChoices();
            //dialogueBox.text = "HET ROI";
        }
    }

    public void DisplayChoices()
    {
        if (choiceHolder.GetComponentsInChildren<Button>().Length > 0) return; //check if button holder have choice in it.
        for (int i = 0; i < _StoryScript.currentChoices.Count; i++)
        {
            var choice = _StoryScript.currentChoices[i];
            var button = CreateChoiceButton(choice.text); //Create a choice button

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
                Destroy(button.gameObject);
            }
        }
    }

    public void ChangeNameTag(string name)
    {
        string speakerName = name;
        nameTag.text = speakerName;
    }
}
