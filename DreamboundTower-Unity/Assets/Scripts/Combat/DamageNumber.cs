using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// Handles floating damage number display
/// </summary>
public class DamageNumber : MonoBehaviour
{
    #region Components
    [Header("Components")]
    public TextMeshProUGUI damageText;
    #endregion
    
    #region Animation Settings
    [Header("Animation Settings")]
    [Tooltip("How long the number stays visible")]
    public float lifetime = 1.5f;
    
    [Tooltip("Distance the number floats upward")]
    public float floatDistance = 30f;
    #endregion
    
    #region Colors
    [Header("Colors")]
    [Tooltip("Color for normal damage")]
    public Color normalDamageColor = Color.white;
    
    [Tooltip("Color for critical damage")]
    public Color criticalDamageColor = Color.yellow;
    
    [Tooltip("Color for healing")]
    public Color healingColor = Color.green;
    #endregion

    #region Private Variables
    private Vector3 startPosition;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Get text component if not assigned
        if (damageText == null)
        {
            damageText = GetComponent<TextMeshProUGUI>();
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Shows damage number with animation
    /// </summary>
    /// <param name="damage">Amount of damage to display</param>
    /// <param name="isCritical">Whether this is a critical hit</param>
    /// <param name="isHealing">Whether this is healing (negative damage)</param>
    public void ShowDamage(int damage, bool isCritical = false, bool isHealing = false)
    {
        // Set text
        string displayText = damage.ToString();
        if (isCritical)
        {
            displayText = "CRIT! " + displayText;
        }
        else if (isHealing)
        {
            displayText = "+" + displayText;
        }
        
        damageText.text = displayText;
        
        // Set color
        if (isHealing)
        {
            damageText.color = healingColor;
        }
        else if (isCritical)
        {
            damageText.color = criticalDamageColor;
        }
        else
        {
            damageText.color = normalDamageColor;
        }
        
        // Start animation
        StartCoroutine(AnimateDamageNumber());
    }
    
    /// <summary>
    /// Shows a custom message (for status effects, etc.)
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="color">Color of the message</param>
    public void ShowMessage(string message, Color color)
    {
        damageText.text = message;
        damageText.color = color;
        
        StartCoroutine(AnimateDamageNumber());
    }
    #endregion
    
    #region Private Methods
    private System.Collections.IEnumerator AnimateDamageNumber()
    {
        // Capture the start position AFTER the damage number is positioned
        startPosition = transform.position;
        
        // Scale up quickly
        transform.localScale = Vector3.zero;
        transform.DOScale(1.2f, 0.1f).SetEase(Ease.OutBack);
        
        yield return new WaitForSeconds(0.1f);
        
        // Scale back to normal
        transform.DOScale(1f, 0.1f).SetEase(Ease.InBack);
        
        yield return new WaitForSeconds(0.1f);
        
        // Float upward and fade out simultaneously
        Vector3 endPosition = startPosition + Vector3.up * floatDistance;
        
        Sequence floatSequence = DOTween.Sequence();
        floatSequence.Append(transform.DOMove(endPosition, lifetime).SetEase(Ease.OutQuad));
        floatSequence.Join(damageText.DOFade(0f, lifetime).SetEase(Ease.InQuad));
        
        // Wait for animation to complete
        yield return new WaitForSeconds(lifetime);
        
        // Clean up
        Destroy(gameObject);
    }
    #endregion
}
