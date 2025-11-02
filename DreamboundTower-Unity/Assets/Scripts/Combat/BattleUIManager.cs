// File: BattleUIManager.cs
using UnityEngine;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using Presets;

public class BattleUIManager : MonoBehaviour
{
    private BattleManager battleManager;
    private List<SkillIconUI> spawnedSkillIcons = new List<SkillIconUI>();
    private SkillIconUI selectedIcon = null;
    
    private float lastCooldownUpdate = 0f;
    private const float COOLDOWN_UPDATE_INTERVAL = 0.1f; // Update every 0.1 seconds

    public void Initialize(BattleManager manager)
    {
        this.battleManager = manager;
        ConnectSkillIconEvents();
    }
    
    void Update()
    {
        // Periodically update cooldown displays
        if (Time.time - lastCooldownUpdate > COOLDOWN_UPDATE_INTERVAL)
        {
            RefreshAllSkillCooldowns();
            lastCooldownUpdate = Time.time;
        }
    }
    
    /// <summary>
    /// Refreshes cooldown display for all skill icons
    /// </summary>
    private void RefreshAllSkillCooldowns()
    {
        foreach (var icon in spawnedSkillIcons)
        {
            if (icon != null)
            {
                icon.RefreshCooldownDisplay();
            }
        }
    }

    private void ConnectSkillIconEvents()
    {
        // Dọn dẹp listener và danh sách cũ
        if (spawnedSkillIcons != null)
        {
            foreach (var icon in spawnedSkillIcons)
            {
                if (icon != null) icon.OnSkillClicked.RemoveAllListeners();
            }
        }
        spawnedSkillIcons.Clear();
        selectedIcon = null;

        if (PlayerHUDController.Instance == null)
        {
            Debug.LogError("[BattleUI] Không tìm thấy PlayerHUDController để kết nối sự kiện!");
            return;
        }

        // Lấy tất cả các icon skill đã được tạo bởi PlayerHUDController
        SkillIconUI[] skillIcons = PlayerHUDController.Instance.skillIconContainer.GetComponentsInChildren<SkillIconUI>();

        foreach (var iconUI in skillIcons)
        {
            // Thêm vào danh sách để quản lý
            spawnedSkillIcons.Add(iconUI);

            // Kết nối sự kiện click
            iconUI.OnSkillClicked.AddListener(OnSkillIconClicked);

            // Kết nối tooltip trigger
            TooltipTrigger trigger = iconUI.GetComponent<TooltipTrigger>();
            if (trigger != null)
            {
                // ✅ GHI ĐÈ KẾT NỐI
                // 1. Xóa listener mặc định đã được thêm bởi PlayerHUDController
                trigger.OnSkillHoverEnter.RemoveAllListeners();
                trigger.OnHoverExit.RemoveAllListeners();

                // 2. Thêm listener nâng cao của BattleUIManager
                trigger.OnSkillHoverEnter.AddListener(ShowTooltip);
                trigger.OnHoverExit.AddListener(HideTooltip);
            }
        }
        Debug.Log($"[BattleUI] Đã kết nối sự kiện cho {spawnedSkillIcons.Count} skill icons.");
    }

    public void ShowTooltip(BaseSkillSO skill, Character caster, RectTransform iconTransform)
    {
        if (caster == null)
        {
            Debug.LogWarning("ShowTooltip received null caster from event.");
            // Thử lấy lại caster từ BattleManager làm dự phòng
            caster = battleManager.GetPlayerCharacter();
            if (caster == null) return; // Nếu vẫn null thì không hiển thị
        }
        // Ra lệnh cho TooltipManager hiển thị
        TooltipManager.Instance.ShowSkillTooltip(skill, caster, iconTransform);
    }
    public void HideTooltip()
    {
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
    
    /// <summary>
    /// Deselects the currently selected skill icon (called when skill is used)
    /// </summary>
    public void DeselectSkillIcon()
    {
        if (selectedIcon != null)
        {
            selectedIcon.SetSelected(false);
            selectedIcon = null;
        }
    }
}