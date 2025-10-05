// File: SkillIconUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; // Rất quan trọng, cần để dùng UnityEvent

// Định nghĩa một Event mới có thể truyền dữ liệu BaseSkillSO đi
[System.Serializable]
public class SkillClickEvent : UnityEvent<BaseSkillSO> { }

public class SkillIconUI : MonoBehaviour
{
    [Header("Components")]
    public Button iconButton;
    public Image iconImage;
    public GameObject highlightBorder;

    [Header("Events")]
    public SkillClickEvent OnSkillClicked; // "Tín hiệu" sẽ được phát đi

    private BaseSkillSO currentSkillSO;

    // Hàm Setup bây giờ không cần manager nữa
    public void Setup(BaseSkillSO dataSO)
    {
        currentSkillSO = dataSO;

        if (dataSO != null && dataSO.icon != null)
        {
            iconImage.sprite = dataSO.icon;
        }

        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    private void OnClick()
    {
        // Khi được click, phát ra tín hiệu và gửi kèm dữ liệu skill
        if (currentSkillSO != null)
        {
            OnSkillClicked.Invoke(currentSkillSO);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isSelected);
        }
    }

    public string GetSkillName()
    {
        return currentSkillSO != null ? currentSkillSO.displayName : "";
    }
}