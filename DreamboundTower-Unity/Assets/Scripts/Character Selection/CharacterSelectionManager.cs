using Presets;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CharacterSelectionManager : MonoBehaviour
{
    // ... (toàn bộ các biến tham chiếu giữ nguyên) ...
    [Header("UI Panels")]
    public GameObject raceSelectionPanel;
    public GameObject classSelectionPanel;
    public GameObject characterInfoPanel;

    [Header("UI Buttons")]
    public Button nextButton;
    public List<Button> raceButtons;
    public List<Button> classButtons;

    [Header("Stats Display")]
    public TextMeshProUGUI hpValueText;
    public TextMeshProUGUI strValueText;
    public TextMeshProUGUI defValueText;
    public TextMeshProUGUI manaValueText;
    public TextMeshProUGUI intValueText;
    public TextMeshProUGUI agiValueText;

    [Header("Skills Display")]
    public GameObject skillIconPrefab;
    public Transform skillIconContainer;
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText;
    public GameObject manaCostBarObject;
    public GameObject cooldownBarObject;
    public TextMeshProUGUI manaCostValueText;
    public TextMeshProUGUI cooldownValueText;

    [Header("Character Display")]
    public Image characterSpriteHolder;

    private RacePresetSO selectedRace;
    private ClassPresetSO selectedClass;
    private List<SkillIconUI> spawnedSkillIcons = new List<SkillIconUI>();

    [Header("Scene Transition")]
    public string nextSceneName = "MapScene"; // Tên scene tiếp theo của bạn

    void Start()
    {
        classSelectionPanel.SetActive(false);
        characterInfoPanel.SetActive(false);
        nextButton.interactable = false;
    }

    public void SelectRace(RacePresetSO race)
    {
        selectedRace = race;
        selectedClass = null;
        characterInfoPanel.SetActive(false);
        nextButton.interactable = false;
        classSelectionPanel.SetActive(true);

        // Cập nhật highlight cho các nút Race
        UpdateSelectionHighlight(raceButtons, selectedRace.displayName);

        // --- DÒNG SỬA LỖI ---
        // Tắt tất cả highlight của các nút Class
        UpdateSelectionHighlight(classButtons, "");
    }

    public void SelectClass(ClassPresetSO charClass)
    {
        selectedClass = charClass;
        characterInfoPanel.SetActive(true);
        UpdateCharacterInfoDisplay();
        CheckIfReadyToProceed();

        UpdateSelectionHighlight(classButtons, selectedClass.displayName);
    }

    // ... (Các hàm còn lại không thay đổi) ...
    void UpdateCharacterInfoDisplay()
    {
        if (selectedRace == null || selectedClass == null) return;
        UpdateStatsDisplay();
        UpdateCharacterSprite();
        UpdateSkillDisplay();
    }
    void UpdateStatsDisplay()
    {
        StatBlock stats = selectedRace.baseStats;
        hpValueText.text = stats.HP.ToString();
        strValueText.text = stats.STR.ToString();
        defValueText.text = stats.DEF.ToString();
        manaValueText.text = stats.MANA.ToString();
        intValueText.text = stats.INT.ToString();
        agiValueText.text = stats.AGI.ToString();
    }
    void UpdateCharacterSprite()
    {
        Sprite spriteToShow = null;
        switch (selectedClass.id)
        {
            case "class_cleric": spriteToShow = selectedRace.clericSprite; break;
            case "class_mage": spriteToShow = selectedRace.mageSprite; break;
            case "class_rogue": spriteToShow = selectedRace.rogueSprite; break;
            case "class_warrior": spriteToShow = selectedRace.warriorSprite; break;
        }
        characterSpriteHolder.sprite = spriteToShow;
    }
    void UpdateSkillDisplay()
    {
        foreach (Transform child in skillIconContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedSkillIcons.Clear();

        List<BaseSkillSO> allSkills = new List<BaseSkillSO>();
        if (selectedRace.passiveSkill) allSkills.Add(selectedRace.passiveSkill);
        if (selectedClass.passiveSkill) allSkills.Add(selectedClass.passiveSkill);
        if (selectedRace.activeSkill) allSkills.Add(selectedRace.activeSkill);
        if (selectedClass.activeSkills != null) allSkills.AddRange(selectedClass.activeSkills.Where(s => s != null));

        var sortedSkills = allSkills.OrderByDescending(skill => skill is PassiveSkillData);

        foreach (var skillSO in sortedSkills)
        {
            CreateSkillIcon(skillSO);
        }

        if (sortedSkills.Any())
        {
            DisplaySkillDetails(sortedSkills.First());
        }
        else
        {
            // Nếu không có skill nào, xóa trắng thông tin
            skillNameText.text = "";
            skillDescriptionText.text = "";
            manaCostBarObject.SetActive(false);
            cooldownBarObject.SetActive(false);
        }
    }
    private void CreateSkillIcon(BaseSkillSO skillSO)
    {
        GameObject skillIconGO = Instantiate(skillIconPrefab, skillIconContainer);
        SkillIconUI skillUI = skillIconGO.GetComponent<SkillIconUI>();

        // Setup cho icon như bình thường
        skillUI.Setup(skillSO);

        // Đăng ký lắng nghe: Khi icon này được click, hãy gọi hàm DisplaySkillDetails
        skillUI.OnSkillClicked.AddListener(DisplaySkillDetails);

        spawnedSkillIcons.Add(skillUI);
    }
    public void DisplaySkillDetails(BaseSkillSO dataSO)
    {
        if (dataSO == null) return;

        skillNameText.text = dataSO.displayName;

        foreach (var iconUI in spawnedSkillIcons)
        {
            bool isSelected = iconUI.GetSkillName() == dataSO.displayName;
            iconUI.SetSelected(isSelected);
        }

        if (dataSO is SkillData activeSkill)
        {
            string dynamicDescription = TooltipFormatter.GenerateDescription(activeSkill, selectedRace.baseStats);
            skillDescriptionText.text = $"<b>ACTIVE:</b> {dynamicDescription}";
            manaCostBarObject.SetActive(true);
            cooldownBarObject.SetActive(true);
            manaCostValueText.text = activeSkill.cost.ToString();
            cooldownValueText.text = activeSkill.cooldown.ToString();
        }
        else if (dataSO is PassiveSkillData passiveSkill)
        {
            skillDescriptionText.text = $"<b>PASSIVE:</b> {passiveSkill.descriptionTemplate}";
            manaCostBarObject.SetActive(false);
            cooldownBarObject.SetActive(false);
        }
    }
    private void UpdateSelectionHighlight(List<Button> buttons, string selectedDisplayName)
    {
        foreach (var button in buttons)
        {
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            var highlight = button.transform.Find("HighlightBorder");

            if (buttonText != null && highlight != null)
            {
                bool isSelected = buttonText.text.Equals(selectedDisplayName, System.StringComparison.OrdinalIgnoreCase);
                highlight.gameObject.SetActive(isSelected);
            }
        }
    }
    void CheckIfReadyToProceed()
    {
        if (selectedRace != null && selectedClass != null)
        {
            nextButton.interactable = true;
        }
    }

    public void ConfirmSelection()
    {
        if (selectedRace == null || selectedClass == null) return;

        GameManager.Instance.selectedRace = selectedRace;
        GameManager.Instance.selectedClass = selectedClass;

        // Ra lệnh cho GameManager tạo nhân vật và sau đó tải scene
        GameManager.Instance.InitializePlayerCharacter();
        GameManager.Instance.LoadNextScene(nextSceneName); // Sử dụng tên scene bạn đã đặt
    }
}