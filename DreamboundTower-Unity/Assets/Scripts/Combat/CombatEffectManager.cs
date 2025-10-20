using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Manages visual effects for combat including hit effects, damage numbers, and screen effects
/// </summary>
public class CombatEffectManager : MonoBehaviour
{
    #region Singleton
    public static CombatEffectManager Instance;
    #endregion

    #region Hit Effects
    [Header("Hit Effects")]
    [Tooltip("Effect prefab spawned when hitting an enemy")]
    public GameObject hitEffectPrefab;
    
    [Tooltip("Effect prefab for critical hits")]
    public GameObject criticalHitEffectPrefab;
    
    [Tooltip("Effect prefab for missed attacks")]
    public GameObject missEffectPrefab;
    #endregion
    
    #region Being Hit Effects
    [Header("Being Hit Effects")]
    [Tooltip("Effect prefab when character takes damage")]
    public GameObject damageEffectPrefab;
    
    [Tooltip("Effect prefab when character dies")]
    public GameObject deathEffectPrefab;
    #endregion
    
    #region Damage Numbers
    [Header("Damage Numbers")]
    [Tooltip("Prefab for floating damage numbers")]
    public GameObject damageNumberPrefab;
    
    [Tooltip("Canvas for damage numbers (should be Screen Space - Overlay)")]
    public Canvas damageNumberCanvas;
    #endregion
    
    #region Unity Lifecycle
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        // Find damage number canvas if not assigned
        RefreshCanvasReference();
    }
    
    /// <summary>
    /// Refreshes the canvas reference - call this when entering a new scene
    /// </summary>
    public void RefreshCanvasReference()
    {
        // Always refresh the canvas reference when entering a new scene
        // because the previous canvas might have been destroyed
        damageNumberCanvas = FindFirstObjectByType<Canvas>();
    }
    #endregion
    
    #region Hit Effects
    /// <summary>
    /// Plays hit effect at the specified position
    /// </summary>
    /// <param name="position">UI position where hit occurred</param>
    /// <param name="isCritical">Whether this is a critical hit</param>
    /// <param name="isMiss">Whether this attack missed</param>
    public void PlayHitEffect(Vector3 position, bool isCritical = false, bool isMiss = false)
    {
        GameObject effectPrefab = null;
        
        if (isMiss)
        {
            effectPrefab = missEffectPrefab;
        }
        else if (isCritical)
        {
            effectPrefab = criticalHitEffectPrefab;
        }
        else
        {
            effectPrefab = hitEffectPrefab;
        }
        
        if (effectPrefab != null)
        {
            // Spawn effect at position
            GameObject effect = Instantiate(effectPrefab, position, Quaternion.identity);
            
            // Auto-destroy after 2 seconds
            Destroy(effect, 2f);
        }
    }
    #endregion

    #region Being Hit Effects
    /// <summary>
    /// Plays being hit effects on a character
    /// </summary>
    /// <param name="target">Character that was hit</param>
    /// <param name="damage">Amount of damage taken</param>
    /// <param name="isCritical">Whether this was a critical hit</param>
    public void PlayBeingHitEffect(Character target, int damage, bool isCritical = false)
    {
        if (target == null) return;
        
        // Play character hit animation
        target.PlayHitAnimation();
        
        // Get the UI position of the character
        Vector3 uiPosition = GetCharacterUIPosition(target);
        
        // Add a small delay to prevent overlapping damage numbers
        StartCoroutine(ShowDamageNumberDelayed(uiPosition, damage, isCritical, 0.1f));
    }
    #endregion
    
    #region Damage Numbers
    /// <summary>
    /// Shows damage number with a small delay
    /// </summary>
    private System.Collections.IEnumerator ShowDamageNumberDelayed(Vector3 position, int damage, bool isCritical, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowDamageNumber(position, damage, isCritical);
    }
    
    /// <summary>
    /// Gets the UI position of a character for damage number display
    /// </summary>
    /// <param name="character">Character to get position for</param>
    /// <returns>UI position for damage number</returns>
    private Vector3 GetCharacterUIPosition(Character character)
    {
        Vector3 position;
        
        // Try to get the character's UI Image position
        if (character.characterImage != null)
        {
            position = character.characterImage.transform.position;
        }
        else
        {
            // For enemies without UI Image, use transform position with offset based on character name
            position = character.transform.position;
            
            // Add a unique offset based on character name hash to separate enemies
            int nameHash = character.name.GetHashCode();
            float offsetX = (nameHash % 100) - 50; // -50 to 50 range
            float offsetY = ((nameHash / 100) % 50) - 25; // -25 to 25 range
            
            position += new Vector3(offsetX, offsetY, 0);
        }
        
        return position;
    }
    
    /// <summary>
    /// Shows floating damage number
    /// </summary>
    /// <param name="position">UI position to show damage number (use character's UI position)</param>
    /// <param name="damage">Damage amount to display</param>
    /// <param name="isCritical">Whether this is a critical hit</param>
    public void ShowDamageNumber(Vector3 position, int damage, bool isCritical = false)
    {
        if (damageNumberPrefab == null || damageNumberCanvas == null) return;
        
        // Spawn damage number
        GameObject damageNumber = Instantiate(damageNumberPrefab, damageNumberCanvas.transform);
        
        // Set position relative to the canvas
        damageNumber.transform.position = position;
        
        // Add some random offset to prevent overlapping
        Vector3 randomOffset = new Vector3(
            Random.Range(-80f, 80f),
            Random.Range(-30f, 30f),
            0f
        );
        damageNumber.transform.position += randomOffset;
        
        // Configure damage number
        DamageNumber damageNumberScript = damageNumber.GetComponent<DamageNumber>();
        if (damageNumberScript != null)
        {
            damageNumberScript.ShowDamage(damage, isCritical);
        }
    }
    #endregion

    #region Death Effects
    /// <summary>
    /// Plays death effect for a character
    /// </summary>
    /// <param name="character">Character that died</param>
    public void PlayDeathEffect(Character character)
    {
        if (character == null || deathEffectPrefab == null) return;
        
        // Spawn death effect
        GameObject effect = Instantiate(deathEffectPrefab, character.transform.position, Quaternion.identity);
        Destroy(effect, 3f);
    }
    #endregion
}
