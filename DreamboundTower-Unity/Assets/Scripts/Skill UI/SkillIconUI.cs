using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Presets;
using TMPro;

[System.Serializable]
public class SkillClickEvent : UnityEvent<BaseSkillSO> { }

public class SkillIconUI : MonoBehaviour
{
    [Header("Components")]
    public Button iconButton;
    public Image iconImage;
    public GameObject highlightBorder;
    
    [Header("Cooldown Display")]
    public Image cooldownOverlay; // Dark overlay when on cooldown
    public TextMeshProUGUI cooldownText; // Shows remaining cooldown (e.g., "3")

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
        
        // Hide cooldown elements by default (will be shown if needed in battle)
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(false);
        }
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(false);
        }
        if (iconButton != null)
        {
            iconButton.interactable = true;
        }
        if (iconImage != null)
        {
            iconImage.color = Color.white;
        }
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
    
    /// <summary>
    /// Updates the cooldown display on the skill icon
    /// </summary>
    /// <param name="remainingCooldown">Turns remaining on cooldown (0 if ready)</param>
    /// <param name="isUsable">Whether the skill can be used (has mana and not on cooldown)</param>
    public void UpdateCooldownDisplay(int remainingCooldown, bool isUsable)
    {
        bool onCooldown = remainingCooldown > 0;
        
        // Show/hide cooldown overlay
        if (cooldownOverlay != null)
        {
            cooldownOverlay.gameObject.SetActive(onCooldown);
            
            // Optional: Adjust opacity based on cooldown
            if (onCooldown)
            {
                Color overlayColor = cooldownOverlay.color;
                overlayColor.a = 0.7f; // 70% opacity
                cooldownOverlay.color = overlayColor;
            }
        }
        
        // Show cooldown number
        if (cooldownText != null)
        {
            if (onCooldown)
            {
                cooldownText.text = remainingCooldown.ToString();
                cooldownText.gameObject.SetActive(true);
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
        
        // Disable button if on cooldown or not usable
        if (iconButton != null)
        {
            iconButton.interactable = isUsable;
        }
        
        // Optional: Change icon brightness when on cooldown
        if (iconImage != null)
        {
            if (onCooldown)
            {
                iconImage.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Gray out
            }
            else if (!isUsable)
            {
                iconImage.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Slightly gray
            }
            else
            {
                iconImage.color = Color.white; // Normal
            }
        }
    }
    
    /// <summary>
    /// Refreshes the cooldown display by querying SkillManager
    /// </summary>
    public void RefreshCooldownDisplay()
    {
        if (currentSkillSO is SkillData activeSkill)
        {
            // Find SkillManager in the scene
            SkillManager skillManager = FindFirstObjectByType<SkillManager>();
            if (skillManager != null)
            {
                // Only in battle: update cooldown display
                int remainingCooldown = skillManager.GetSkillCooldown(activeSkill);
                bool isUsable = skillManager.CanUseSkill(activeSkill);
                UpdateCooldownDisplay(remainingCooldown, isUsable);
            }
            else
            {
                // Not in battle (e.g., character selection): hide cooldown elements
                if (cooldownOverlay != null)
                {
                    cooldownOverlay.gameObject.SetActive(false);
                }
                if (cooldownText != null)
                {
                    cooldownText.gameObject.SetActive(false);
                }
                if (iconButton != null)
                {
                    iconButton.interactable = true; // Always clickable outside battle
                }
                if (iconImage != null)
                {
                    iconImage.color = Color.white; // Normal color outside battle
                }
            }
        }
        else
        {
            // Passive skill or no skill: hide cooldown elements
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(false);
            }
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
}