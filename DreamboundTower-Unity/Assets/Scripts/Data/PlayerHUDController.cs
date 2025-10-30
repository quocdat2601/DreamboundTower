using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Presets;

public class PlayerHUDController : MonoBehaviour
{
    public static PlayerHUDController Instance { get; private set; }

    [Header("Asset References")]
    [Tooltip("Kéo Prefab của Skill Icon vào đây")]
    public GameObject skillIconPrefab;

    // Biến này sẽ được tìm tự động, không cần kéo thả trong Inspector nữa
    public Transform skillIconContainer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    /// <summary>
    /// Hàm này sẽ được GameManager gọi mỗi khi một scene mới được tải.
    /// </summary>
    public void FindAndRefresh()
    {
        // 1. TÌM KIẾM "NƠI LÀM VIỆC"
        GameObject containerObject = GameObject.Find("SkillPanel"); // Tên phải chính xác!

        if (containerObject != null)
        {
            skillIconContainer = containerObject.transform;
            Debug.Log("<color=green>[PlayerHUD] Đã tìm thấy SkillPanel. Bắt đầu cập nhật skills.</color>");
            RefreshPlayerSkills();
        }
        else
        {
            // Nếu không tìm thấy, chỉ cần dọn dẹp và không làm gì cả
            Debug.Log("[PlayerHUD] Không tìm thấy 'SkillPanel' trong scene này. Bỏ qua việc cập nhật skills.");
            ClearSkillIcons();
        }
    }

    private void RefreshPlayerSkills()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerInstance == null)
        {
            ClearSkillIcons();
            return;
        }

        PlayerSkills playerSkills = GameManager.Instance.playerInstance.GetComponent<PlayerSkills>();
        CreatePlayerSkillIcons(playerSkills);
    }
    private void CreatePlayerSkillIcons(PlayerSkills playerSkills)
    {
        ClearSkillIcons();
        if (playerSkills == null || skillIconContainer == null) return;

        // --- LẤY PLAYER CHARACTER MỘT LẦN ---
        Character playerCharacterRef = null;
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            playerCharacterRef = GameManager.Instance.playerInstance.GetComponent<Character>();
        }
        // --- KẾT THÚC LẤY ---

        List<BaseSkillSO> allSkills = new List<BaseSkillSO>();
        allSkills.AddRange(playerSkills.passiveSkills);
        allSkills.AddRange(playerSkills.activeSkills);
        var sortedSkills = allSkills.OrderByDescending(skill => skill is PassiveSkillData); // Sửa lại tên class Passive nếu khác

        foreach (var skillSO in sortedSkills)
        {
            if (skillSO == null) continue;

            GameObject iconGO = Instantiate(skillIconPrefab, skillIconContainer);
            SkillIconUI iconUI = iconGO.GetComponent<SkillIconUI>();
            if (iconUI != null)
            {
                iconUI.Setup(skillSO); // Setup icon như cũ

                TooltipTrigger trigger = iconGO.GetComponent<TooltipTrigger>();
                if (trigger != null)
                {
                    trigger.dataToShow = skillSO; // Gán data cho trigger

                    // --- SỬA LẠI ADDLISTENER ---
                    // Xóa listener cũ (nếu có) để tránh gọi nhiều lần
                    trigger.OnSkillHoverEnter.RemoveAllListeners();
                    trigger.OnHoverExit.RemoveAllListeners(); // Xóa cả listener Exit cũ

                    // Thêm listener mới với lambda nhận 3 tham số
                    trigger.OnSkillHoverEnter.AddListener((skill, caster, rect) => {
                        // Kiểm tra xem TooltipManager có tồn tại không
                        if (TooltipManager.Instance != null)
                        {
                            // Gọi ShowSkillTooltip với 3 tham số nhận được từ Event
                            TooltipManager.Instance.ShowSkillTooltip(skill, caster, rect);
                        }
                    });
                    // --- KẾT THÚC SỬA ---

                    // Gán lại listener cho Exit
                    trigger.OnHoverExit.AddListener(() => {
                        if (TooltipManager.Instance != null) TooltipManager.Instance.HideAllTooltips();
                    });
                }
            }
        }
    }

    void ClearSkillIcons()
    {
        if (skillIconContainer == null) return;
        foreach (Transform child in skillIconContainer)
        {
            // Quan trọng: Phải Remove listener trước khi Destroy để tránh lỗi
            TooltipTrigger trigger = child.GetComponent<TooltipTrigger>();
            if (trigger != null)
            {
                trigger.OnSkillHoverEnter.RemoveAllListeners();
                trigger.OnHoverExit.RemoveAllListeners();
            }
            Destroy(child.gameObject);
        }
    }
}