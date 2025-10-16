using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Presets;

[System.Serializable]
public class SkillClickEvent : UnityEvent<BaseSkillSO> { }

public class SkillIconUI : MonoBehaviour
{
    [Header("Components")]
    public Button iconButton;
    public Image iconImage;
    public GameObject highlightBorder;

    [Header("Events")]
    public SkillClickEvent OnSkillClicked;

    private BaseSkillSO currentSkillSO;

    // Hàm Setup giờ chỉ cần nhận dữ liệu
    public void Setup(BaseSkillSO dataSO)
    {
        currentSkillSO = dataSO;
        if (currentSkillSO != null && currentSkillSO.icon != null)
        {
            iconImage.sprite = currentSkillSO.icon;
        }

        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(OnClick);

        SetSelected(false);
    }

    private void OnClick()
    {
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