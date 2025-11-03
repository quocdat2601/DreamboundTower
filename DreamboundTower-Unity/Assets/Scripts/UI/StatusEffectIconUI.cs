using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StatusEffects;

/// <summary>
/// UI component for displaying a single status effect icon
/// Shows icon, duration, and automatically updates
/// </summary>
public class StatusEffectIconUI : MonoBehaviour
{
    [Header("Components")]
    public Image iconImage;
    public TextMeshProUGUI durationText;
    
    private StatusEffect currentEffect;
    private Sprite iconSprite;
    private Color iconTint = Color.white;
    
    /// <summary>
    /// Sets up the icon with status effect data
    /// </summary>
    public void Setup(StatusEffect effect, Sprite icon, Color tint)
    {
        currentEffect = effect;
        iconSprite = icon;
        iconTint = tint;
        
        if (iconImage != null)
        {
            iconImage.sprite = icon;
            // Set to white by default unless a specific tint is desired
            iconImage.color = Color.white;
        }
        
        UpdateDisplay();
    }
    
    /// <summary>
    /// Updates the duration text
    /// Call this every turn
    /// </summary>
    public void UpdateDisplay()
    {
        if (currentEffect != null && durationText != null)
        {
            if (currentEffect.duration > 0)
            {
                durationText.text = currentEffect.duration.ToString();
                durationText.gameObject.SetActive(true);
            }
            else
            {
                durationText.gameObject.SetActive(false);
            }
        }
    }
    
    /// <summary>
    /// Gets the effect name (for identification)
    /// </summary>
    public string GetEffectName()
    {
        return currentEffect != null ? currentEffect.effectName : "";
    }
}

