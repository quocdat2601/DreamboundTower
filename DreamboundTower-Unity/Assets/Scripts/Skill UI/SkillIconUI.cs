using UnityEngine;
using UnityEngine.UI;

public class SkillIconUI : MonoBehaviour
{
    public Button iconButton;
    public Image iconImage;

    // Thay vì lưu một loại cụ thể, chúng ta lưu các thông tin chung
    private string skillName;
    private string skillDescription;
    private int skillManaCost;
    private int skillCooldown;
    private bool isPassive;

    private CharacterSelectionManager selectionManager;

    // Hàm Setup cho Active Skill (SkillData)
    public void Setup(SkillData dataSO, CharacterSelectionManager manager)
    {
        this.selectionManager = manager;

        // Lưu trữ thông tin từ SO
        this.skillName = dataSO.displayName;
        this.skillDescription = dataSO.description;
        this.skillManaCost = dataSO.cost;
        this.skillCooldown = dataSO.cooldown;
        this.isPassive = false; // Đánh dấu đây là Active skill

        if (dataSO.icon != null)
        {
            iconImage.sprite = dataSO.icon;
        }

        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(OnClick);
    }

    // Hàm Setup cho Passive Skill (PassiveSkillData)
    public void Setup(PassiveSkillData dataSO, CharacterSelectionManager manager)
    {
        this.selectionManager = manager;

        // Lưu trữ thông tin từ SO
        this.skillName = dataSO.displayName;
        this.skillDescription = dataSO.description;
        this.isPassive = true; // Đánh dấu đây là Passive skill

        // Các giá trị không áp dụng cho Passive
        this.skillManaCost = 0;
        this.skillCooldown = 0;

        if (dataSO.icon != null)
        {
            iconImage.sprite = dataSO.icon;
        }

        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (selectionManager != null)
        {
            // Gửi lại toàn bộ thông tin đã lưu cho Manager
            selectionManager.DisplaySkillDetails(skillName, skillDescription, skillManaCost, skillCooldown, isPassive);
        }
    }
}