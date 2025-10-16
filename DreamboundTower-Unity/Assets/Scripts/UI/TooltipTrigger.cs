using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Presets;

// Định nghĩa các event mới có thể gửi dữ liệu đi
[System.Serializable] public class ItemTooltipEvent : UnityEvent<GearItem> { }
[System.Serializable] public class SkillTooltipEvent : UnityEvent<BaseSkillSO, RectTransform> { }

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public ScriptableObject dataToShow;

    // Các sự kiện để "phát sóng" tín hiệu khi hover
    public ItemTooltipEvent OnItemHoverEnter;
    public SkillTooltipEvent OnSkillHoverEnter;
    public UnityEvent OnHoverExit;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dataToShow == null) return;

        // Kiểm tra loại dữ liệu và phát sóng sự kiện tương ứng
        if (dataToShow is GearItem item)
        {
            OnItemHoverEnter.Invoke(item);
        }
        else if (dataToShow is BaseSkillSO skill)
        {
            OnSkillHoverEnter.Invoke(skill, transform as RectTransform);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Luôn phát sóng tín hiệu Exit
        OnHoverExit.Invoke();
    }
}