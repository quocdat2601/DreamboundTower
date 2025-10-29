using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using Presets;

// Định nghĩa các event mới có thể gửi dữ liệu đi
[System.Serializable] public class ItemTooltipEvent : UnityEvent<GearItem> { }
[System.Serializable] public class SkillTooltipEvent : UnityEvent<BaseSkillSO, Character, RectTransform> { }
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [HideInInspector] public ScriptableObject dataToShow;

    // Các sự kiện để "phát sóng" tín hiệu khi hover
    public ItemTooltipEvent OnItemHoverEnter;
    public SkillTooltipEvent OnSkillHoverEnter;
    public UnityEvent OnHoverExit;
    private Character playerCharacter;

    void Start()
    {
        // Lấy tham chiếu Player khi bắt đầu (hoặc có thể lấy khi cần)
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dataToShow == null) return;

        // Kiểm tra loại dữ liệu và phát sóng sự kiện tương ứng
        if (dataToShow is GearItem item)
        {
            if (OnItemHoverEnter != null)
            {
                OnItemHoverEnter.Invoke(item);
            }
        }
        else if (dataToShow is BaseSkillSO skill)
        {
            // Kiểm tra xem đã lấy được playerCharacter chưa
            if (playerCharacter == null)
            {
                // Thử lấy lại nếu chưa có
                if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
                {
                    playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
                }
            }

            // Chỉ gọi Invoke nếu có playerCharacter
            if (playerCharacter != null)
            {
                // Gọi Invoke với cả skill, playerCharacter và RectTransform
                OnSkillHoverEnter.Invoke(skill, playerCharacter, transform as RectTransform);
            }
            else
            {
                Debug.LogWarning("TooltipTrigger: Cannot find Player Character to show skill tooltip calculation.");
                // Có thể hiển thị tooltip tĩnh hoặc không hiển thị gì cả
                // OnSkillHoverEnter.Invoke(skill, null, transform as RectTransform); // Gọi với caster=null nếu TooltipManager xử lý được
            }
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        // Luôn phát sóng tín hiệu Exit
        if (OnHoverExit != null)
        {
            OnHoverExit.Invoke();
        }
    }
}