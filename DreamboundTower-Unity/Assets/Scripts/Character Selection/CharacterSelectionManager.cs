using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Presets;

public class CharacterSelectionManager : MonoBehaviour
{
    // ... (các biến tham chiếu giữ nguyên không đổi) ...
    [Header("UI Panels")]
    public GameObject raceSelectionPanel;
    public GameObject classSelectionPanel;
    public GameObject characterInfoPanel;

    [Header("UI Buttons")]
    public Button nextButton;

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
    public TextMeshProUGUI skillManaCostText;
    public TextMeshProUGUI skillCooldownText;

    [Header("Character Display")]
    public Image characterSpriteHolder;

    private RacePresetSO selectedRace;
    private ClassPresetSO selectedClass;

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
    }

    public void SelectClass(ClassPresetSO charClass)
    {
        selectedClass = charClass;
        characterInfoPanel.SetActive(true);
        UpdateCharacterInfoDisplay();
        CheckIfReadyToProceed();
    }

    void UpdateCharacterInfoDisplay()
    {
        // ... (hàm này không đổi) ...
        if (selectedRace == null || selectedClass == null) return;
        UpdateStatsDisplay();
        UpdateCharacterSprite();
        UpdateSkillDisplay();
    }

    void UpdateStatsDisplay()
    {
        // ... (hàm này không đổi) ...
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
        // ... (hàm này không đổi) ...
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

        // Tạo icon cho các skill, bây giờ chỉ cần truyền SO vào
        if (selectedRace.passiveSkill) CreateSkillIcon(selectedRace.passiveSkill);
        if (selectedClass.passiveSkill) CreateSkillIcon(selectedClass.passiveSkill);
        if (selectedRace.activeSkill) CreateSkillIcon(selectedRace.activeSkill);
        foreach (var skill in selectedClass.activeSkills)
        {
            if (skill) CreateSkillIcon(skill);
        }

        // Mặc định hiển thị skill đầu tiên
        if (selectedRace.passiveSkill) DisplaySkillDetails(selectedRace.passiveSkill);
    }

    private void CreateSkillIcon(SkillData skillSO)
    {
        GameObject skillIconGO = Instantiate(skillIconPrefab, skillIconContainer);
        skillIconGO.GetComponent<SkillIconUI>().Setup(skillSO, this);
    }

    public void DisplaySkillDetails(SkillData dataSO)
    {
        skillNameText.text = dataSO.displayName;
        skillDescriptionText.text = dataSO.description;

        // Skill bị động thường không có cost và cooldown
        bool isPassive = dataSO.cost == 0 && dataSO.cooldown == 0;

        skillManaCostText.gameObject.SetActive(!isPassive);
        skillCooldownText.gameObject.SetActive(!isPassive);

        if (!isPassive)
        {
            skillManaCostText.text = $"Mana: {dataSO.cost}";
            skillCooldownText.text = $"CD: {dataSO.cooldown}";
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
        Debug.Log($"Confirmed! Race: {selectedRace.displayName}, Class: {selectedClass.displayName}.");
    }
}