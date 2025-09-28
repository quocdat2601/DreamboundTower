using UnityEngine;
using UnityEngine.UI;

public class SkillIconUI : MonoBehaviour
{
    public Button iconButton;
    public Image iconImage;

    private SkillData currentSkillSO; // Đổi từ struct thành class SO
    private CharacterSelectionManager selectionManager;

    // Hàm Setup bây giờ nhận vào một SkillData ScriptableObject
    public void Setup(SkillData dataSO, CharacterSelectionManager manager)
    {
        currentSkillSO = dataSO;
        selectionManager = manager;

        if (dataSO != null && dataSO.icon != null)
        {
            iconImage.sprite = dataSO.icon;
        }

        iconButton.onClick.RemoveAllListeners();
        iconButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (selectionManager != null && currentSkillSO != null)
        {
            selectionManager.DisplaySkillDetails(currentSkillSO);
        }
    }
}