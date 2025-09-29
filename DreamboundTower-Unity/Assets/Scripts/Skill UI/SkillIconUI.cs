// File: SkillIconUI.cs
using UnityEngine;
using UnityEngine.UI;

public class SkillIconUI : MonoBehaviour
{
    public Button iconButton;
    public Image iconImage;
    public GameObject highlightBorder; // Thêm tham chiếu đến viền vàng

    private BaseSkillSO currentSkillSO;
    private CharacterSelectionManager selectionManager;

    public void Setup(BaseSkillSO dataSO, CharacterSelectionManager manager)
    {
        currentSkillSO = dataSO;
        selectionManager = manager;

        if (dataSO != null && dataSO.icon != null)
        {
            iconImage.sprite = dataSO.icon;
        }

        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(OnClick);

        SetSelected(false); // Mặc định tắt highlight
    }

    private void OnClick()
    {
        if (selectionManager != null && currentSkillSO != null)
        {
            selectionManager.DisplaySkillDetails(currentSkillSO);
        }
    }

    // Hàm để bật/tắt viền vàng
    public void SetSelected(bool isSelected)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(isSelected);
        }
    }

    // Hàm để manager có thể lấy tên skill để so sánh
    public string GetSkillName()
    {
        return currentSkillSO != null ? currentSkillSO.displayName : "";
    }
}