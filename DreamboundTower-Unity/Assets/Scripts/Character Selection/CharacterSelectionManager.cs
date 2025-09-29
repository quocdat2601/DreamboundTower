using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Presets;
using System.Collections.Generic;
using System.Linq;

public class CharacterSelectionManager : MonoBehaviour
{
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

    [Tooltip("Kéo GameObject cha 'ManaCostBar' vào đây")]
    public GameObject manaCostBarObject;
    [Tooltip("Kéo GameObject cha 'CooldownBar' vào đây")]
    public GameObject cooldownBarObject;

    [Tooltip("Kéo TextMeshPro 'ManaCostValue' vào đây")]
    public TextMeshProUGUI manaCostValueText;
    [Tooltip("Kéo TextMeshPro 'CD_Value' vào đây")]
    public TextMeshProUGUI cooldownValueText;

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

    // ... (Các hàm SelectRace, SelectClass, UpdateCharacterInfoDisplay, UpdateStatsDisplay, UpdateCharacterSprite giữ nguyên) ...
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

        List<object> allSkills = new List<object>();
        if (selectedRace.passiveSkill) allSkills.Add(selectedRace.passiveSkill);
        if (selectedClass.passiveSkill) allSkills.Add(selectedClass.passiveSkill);
        if (selectedRace.activeSkill) allSkills.Add(selectedRace.activeSkill);
        if (selectedClass.activeSkills != null)
        {
            foreach (var skill in selectedClass.activeSkills)
            {
                if (skill) allSkills.Add(skill);
            }
        }

        var sortedSkills = allSkills.OrderByDescending(skill => skill is PassiveSkillData);

        foreach (var skill in sortedSkills)
        {
            if (skill is PassiveSkillData passive) CreateSkillIcon(passive);
            else if (skill is SkillData active) CreateSkillIcon(active);
        }

        if (sortedSkills.Any())
        {
            var firstSkill = sortedSkills.First();
            if (firstSkill is PassiveSkillData passive)
            {
                DisplaySkillDetails(passive.displayName, passive.description, 0, 0, true);
            }
            else if (firstSkill is SkillData active)
            {
                DisplaySkillDetails(active.displayName, active.description, active.cost, active.cooldown, false);
            }
        }
    }

    private void CreateSkillIcon(SkillData skillSO)
    {
        if (skillSO == null) return;
        GameObject skillIconGO = Instantiate(skillIconPrefab, skillIconContainer);
        skillIconGO.GetComponent<SkillIconUI>().Setup(skillSO, this);
    }

    private void CreateSkillIcon(PassiveSkillData skillSO)
    {
        if (skillSO == null) return;
        GameObject skillIconGO = Instantiate(skillIconPrefab, skillIconContainer);
        skillIconGO.GetComponent<SkillIconUI>().Setup(skillSO, this);
    }

    public void DisplaySkillDetails(string name, string description, int manaCost, int cooldown, bool isPassive)
    {
        skillNameText.text = name;

        string prefix = isPassive ? "PASSIVE:" : "ACTIVE:";
        skillDescriptionText.text = $"<b>{prefix}</b> {description}";

        // Sử dụng các tham chiếu GameObject cha để ẩn/hiện
        manaCostBarObject.SetActive(!isPassive);
        cooldownBarObject.SetActive(!isPassive);

        if (!isPassive)
        {
            // Sử dụng các tham chiếu Text con để set giá trị
            manaCostValueText.text = manaCost.ToString();
            cooldownValueText.text = cooldown.ToString();
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