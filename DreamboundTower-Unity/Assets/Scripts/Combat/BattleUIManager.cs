// File: BattleUIManager.cs
using UnityEngine;
using System.Collections.Generic;

public class BattleUIManager : MonoBehaviour
{
    [Header("Skill UI")]
    public GameObject skillIconPrefab;
    public Transform skillIconContainer;

    private BattleManager battleManager;

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

        if (playerSkills == null) return;

        foreach (var activeSkill in playerSkills.activeSkills)
        {
            if (activeSkill == null) continue;

            GameObject iconGO = Instantiate(skillIconPrefab, skillIconContainer);
            SkillIconUI iconUI = iconGO.GetComponent<SkillIconUI>();
            if (iconUI != null)
            {
                iconUI.Setup(activeSkill);
                // Đăng ký lắng nghe: Khi icon này được click, hãy gọi hàm OnPlayerSelectSkill trong BattleManager
                iconUI.OnSkillClicked.AddListener(battleManager.OnPlayerSelectSkill);
            }
        }
    }
}