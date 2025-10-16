// File: BattleUIManager.cs
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using Presets;

public class BattleUIManager : MonoBehaviour
{
    [Header("Skill UI")]
    public GameObject skillIconPrefab;
    public Transform skillIconContainer;

    [Header("Skill Tooltip")]
    public GameObject skillTooltipPanel;
    public TextMeshProUGUI tooltipSkillName;
    public TextMeshProUGUI tooltipDescription;
    [Tooltip("Kéo GameObject cha 'ManaCostBar' vào đây")]
    public GameObject tooltipManaCostBar;
    [Tooltip("Kéo GameObject cha 'CooldownBar' vào đây")]
    public GameObject tooltipCooldownBar;
    [Tooltip("Kéo TextMeshPro 'ManaCostValue' vào đây")]
    public TextMeshProUGUI tooltipManaCostValue;
    [Tooltip("Kéo TextMeshPro 'CD_Value' vào đây")]
    public TextMeshProUGUI tooltipCooldownValue;

    private BattleManager battleManager;
    private List<SkillIconUI> spawnedSkillIcons = new List<SkillIconUI>();
    private SkillIconUI selectedIcon = null;

    public void Initialize(BattleManager manager)
    {
        this.battleManager = manager;
        if (skillTooltipPanel != null)
        {
            skillTooltipPanel.SetActive(false);
        }
    }

    public void CreatePlayerSkillIcons(PlayerSkills playerSkills)
    {
        foreach (Transform child in skillIconContainer)
        {
            Destroy(child.gameObject);
        }
        spawnedSkillIcons.Clear();
        selectedIcon = null;

        if (playerSkills == null) return;

        // --- PHẦN SỬA ĐỔI ---

        // 1. Thu thập tất cả skill vào một danh sách
        List<BaseSkillSO> allSkills = new List<BaseSkillSO>();
        allSkills.AddRange(playerSkills.passiveSkills);
        allSkills.AddRange(playerSkills.activeSkills);

        // 2. Sắp xếp (tùy chọn, nếu bạn muốn passive luôn ở bên trái)
        var sortedSkills = allSkills.OrderByDescending(skill => skill is PassiveSkillData);

        // 3. Tạo icon từ danh sách đã thu thập/sắp xếp
        foreach (var skillSO in sortedSkills)
        {
            if (skillSO == null) continue;

            GameObject iconGO = Instantiate(skillIconPrefab, skillIconContainer);
            SkillIconUI iconUI = iconGO.GetComponent<SkillIconUI>();
            if (iconUI != null)
            {
                // Dùng hàm Setup phù hợp (giả định SkillIconUI có thể xử lý cả hai)
                iconUI.Setup(skillSO, this);
                iconUI.OnSkillClicked.AddListener(OnSkillIconClicked);
                spawnedSkillIcons.Add(iconUI);
            }
        }
    }

    public void ShowTooltip(BaseSkillSO skill, RectTransform iconTransform)
    {
        if (skillTooltipPanel == null || skill == null) return;

        Character playerCharacter = battleManager.GetPlayerCharacter();
        if (playerCharacter == null) return;

        skillTooltipPanel.SetActive(true);
        skillTooltipPanel.transform.position = iconTransform.position + new Vector3(0, iconTransform.sizeDelta.y + 10f, 0);

        if (tooltipSkillName) tooltipSkillName.text = skill.displayName;

        if (skill is SkillData activeSkill)
        {
            // 1. Tạo StatBlock tạm thời từ chỉ số hiện tại của nhân vật
            StatBlock currentStats = new StatBlock
            {
                HP = playerCharacter.maxHP,
                STR = playerCharacter.attackPower,
                DEF = playerCharacter.defense,
                MANA = playerCharacter.mana,
                INT = playerCharacter.intelligence,
                AGI = playerCharacter.agility
            };

            // 2. Truyền StatBlock đó vào TooltipFormatter
            if (tooltipDescription) tooltipDescription.text = $"<b>ACTIVE:</b> {TooltipFormatter.GenerateDescription(activeSkill, currentStats)}";

            if (tooltipManaCostBar) tooltipManaCostBar.SetActive(true);
            if (tooltipCooldownBar) tooltipCooldownBar.SetActive(true);
            if (tooltipManaCostValue) tooltipManaCostValue.text = activeSkill.cost.ToString();
            if (tooltipCooldownValue) tooltipCooldownValue.text = activeSkill.cooldown.ToString();
        }
        else if (skill is PassiveSkillData passiveSkill)
        {
            if (tooltipDescription) tooltipDescription.text = $"<b>PASSIVE:</b> {passiveSkill.descriptionTemplate}";
            if (tooltipManaCostBar) tooltipManaCostBar.SetActive(false);
            if (tooltipCooldownBar) tooltipCooldownBar.SetActive(false);
        }
    }

    public void HideTooltip()
    {
        if (skillTooltipPanel != null) skillTooltipPanel.SetActive(false);
    }

    private void OnSkillIconClicked(BaseSkillSO skillSO)
    {
        SkillIconUI clickedIcon = spawnedSkillIcons.FirstOrDefault(icon => icon.GetSkillName() == skillSO.displayName);
        if (clickedIcon == null) return;

        if (selectedIcon == clickedIcon)
        {
            selectedIcon.SetSelected(false);
            selectedIcon = null;
            battleManager.OnPlayerDeselectSkill();
        }
        else
        {
            if (selectedIcon != null) selectedIcon.SetSelected(false);
            selectedIcon = clickedIcon;
            selectedIcon.SetSelected(true);
            battleManager.OnPlayerSelectSkill(skillSO);
        }
    }
}