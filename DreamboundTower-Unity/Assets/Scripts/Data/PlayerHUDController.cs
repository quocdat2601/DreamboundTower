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

        List<BaseSkillSO> allSkills = new List<BaseSkillSO>();
        allSkills.AddRange(playerSkills.passiveSkills);
        allSkills.AddRange(playerSkills.activeSkills);

        var sortedSkills = allSkills.OrderByDescending(skill => skill is PassiveSkillData);

        foreach (var skillSO in sortedSkills)
        {
            if (skillSO == null) continue;

            GameObject iconGO = Instantiate(skillIconPrefab, skillIconContainer);
            SkillIconUI iconUI = iconGO.GetComponent<SkillIconUI>();
            if (iconUI != null)
            {
                iconUI.Setup(skillSO);

                // ✅ GÁN DỮ LIỆU CHO TOOLTIP TRIGGER
                // Chỉ cần gán data, không cần lo về listener ở đây
                TooltipTrigger trigger = iconGO.GetComponent<TooltipTrigger>();
                if (trigger != null)
                {
                    trigger.dataToShow = skillSO;
                    trigger.OnSkillHoverEnter.AddListener((skill, rect) => {

                        StatBlock statsToDisplay = new StatBlock(); // Tạo một StatBlock rỗng làm phương án dự phòng

                        // Cố gắng lấy chỉ số "live" từ người chơi hiện tại
                        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
                        {
                            Character playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
                            if (playerCharacter != null)
                            {
                                // Điền dữ liệu thực tế vào StatBlock
                                statsToDisplay = new StatBlock
                                {
                                    HP = playerCharacter.maxHP,
                                    MANA = playerCharacter.mana,
                                    STR = playerCharacter.attackPower,
                                    DEF = playerCharacter.defense,
                                    INT = playerCharacter.intelligence,
                                    AGI = playerCharacter.agility
                                };
                            }
                        }
                        // Gọi TooltipManager với dữ liệu chính xác (hoặc dữ liệu rỗng nếu không tìm thấy người chơi)
                        TooltipManager.Instance.ShowSkillTooltip(skill, statsToDisplay);
                    });

                    trigger.OnHoverExit.AddListener(TooltipManager.Instance.HideAllTooltips);
                }
            }
        }
    }

    private void ClearSkillIcons()
    {
        if (skillIconContainer == null) return;
        foreach (Transform child in skillIconContainer)
        {
            Destroy(child.gameObject);
        }
    }
}