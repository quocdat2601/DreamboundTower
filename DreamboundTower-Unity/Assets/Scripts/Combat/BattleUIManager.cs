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

    private BattleManager battleManager;
    private List<SkillIconUI> spawnedSkillIcons = new List<SkillIconUI>();
    private SkillIconUI selectedIcon = null;

    public void Initialize(BattleManager manager)
    {
        this.battleManager = manager;
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
        // Bây giờ, nhiệm vụ của hàm này là:
        // 1. Lấy dữ liệu cần thiết (characterStats)
        Character playerCharacter = battleManager.GetPlayerCharacter();
        if (playerCharacter == null) return;
        StatBlock currentStats = new StatBlock
        {
            HP = playerCharacter.maxHP,
            STR = playerCharacter.attackPower,
            DEF = playerCharacter.defense,
            MANA = playerCharacter.mana,
            INT = playerCharacter.intelligence,
            AGI = playerCharacter.agility
        };
        // 2. Ra lệnh cho TooltipManager hiển thị
        TooltipManager.Instance.ShowSkillTooltip(skill, currentStats);
    }
    public void HideTooltip()
    {
        // Chỉ cần ra lệnh cho TooltipManager ẩn tất cả
        TooltipManager.Instance.HideAllTooltips();
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