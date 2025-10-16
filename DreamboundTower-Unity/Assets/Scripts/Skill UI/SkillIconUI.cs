// File: SkillIconUI.cs
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

// Định nghĩa một Event mới có thể truyền dữ liệu BaseSkillSO đi
// Đặt nó ở đây để CharacterSelectionManager cũng có thể "thấy"
[System.Serializable]
public class SkillClickEvent : UnityEvent<BaseSkillSO> { }

public class SkillIconUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Components")]
    public Button iconButton;
    public Image iconImage;
    public GameObject highlightBorder;

    [Header("Events")]
    public SkillClickEvent OnSkillClicked;

    // Biến để lưu trữ tham chiếu đến manager của scene hiện tại
    private object currentManager;
    private BaseSkillSO currentSkillSO;

    // ----- CÁC HÀM SETUP -----
    public void Setup(BaseSkillSO dataSO, CharacterSelectionManager manager)
    {
        currentSkillSO = dataSO;
        currentManager = manager;
        InternalSetup();
    }
    public void Setup(BaseSkillSO dataSO, BattleUIManager manager)
    {
        currentSkillSO = dataSO;
        currentManager = manager;
        InternalSetup();
    }
    // -------------------------

    private void InternalSetup()
    {
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
        // Khi được click, phát ra tín hiệu và gửi kèm dữ liệu skill
        if (currentSkillSO != null)
        {
            OnSkillClicked.Invoke(currentSkillSO);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentManager is BattleUIManager battleUIManager)
        {
            battleUIManager.ShowTooltip(currentSkillSO, transform as RectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentManager is BattleUIManager battleUIManager)
        {
            battleUIManager.HideTooltip();
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