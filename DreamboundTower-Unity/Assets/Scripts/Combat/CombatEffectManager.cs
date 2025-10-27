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
    // Hit effects are now handled by sprite animations in Character.cs
    // (PlayHitAnimation, PlayCriticalHitAnimation, PlayMissAnimation)
    #endregion
    
    #region Being Hit Effects
    // Being hit effects are now handled by sprite animations in Character.cs
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
    // Hit effects are now handled by sprite animations in Character.cs
    // This method is kept for backwards compatibility but does nothing
    public void PlayHitEffect(Vector3 position, bool isCritical = false, bool isMiss = false)
    {
        // Legacy method - effects are now handled by Character sprite animations
        // Kept for backwards compatibility
    }
    #endregion

    #region Being Hit Effects
    /// <summary>
    /// Plays being hit effects on a character
    /// </summary>
    /// <param name="target">Character that was hit</param>
    /// <param name="damage">Amount of damage taken</param>
    /// <param name="isCritical">Whether this was a critical hit</param>
    /// <param name="isMagical">Whether this is magical damage</param>
    public void PlayBeingHitEffect(Character target, int damage, bool isCritical = false, bool isMagical = false, bool isMiss = false)
    {
        if (target == null) return;
        
        // Play character hit animation (miss uses different animation)
        if (isMiss)
        {
            target.PlayMissAnimation();
        }
        else
        {
            target.PlayHitAnimation();
        }
        
        // Get the UI position of the character
        Vector3 uiPosition = GetCharacterUIPosition(target);
        
        // Add a small delay to prevent overlapping damage numbers
        StartCoroutine(ShowDamageNumberDelayed(uiPosition, damage, isCritical, isMagical, isMiss, 0.1f));
    }
    #endregion
    
    #region Damage Numbers
    /// <summary>
    /// Shows damage number with a small delay
    /// </summary>
    private System.Collections.IEnumerator ShowDamageNumberDelayed(Vector3 position, int damage, bool isCritical, bool isMagical, bool isMiss, float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowDamageNumber(position, damage, isCritical, isMagical, isMiss);
    }
    
    /// <summary>
    /// Gets the UI position of a character for damage number display
    /// </summary>
    /// <param name="character">Character to get position for</param>
    /// <returns>UI position for damage number</returns>
    public Vector3 GetCharacterUIPosition(Character character)
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
    /// <param name="isMagical">Whether this is magical damage</param>
    /// <param name="isMiss">Whether this attack missed</param>
    public void ShowDamageNumber(Vector3 position, int damage, bool isCritical = false, bool isMagical = false, bool isMiss = false)
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
            damageNumberScript.ShowDamage(damage, isCritical, false, isMagical, isMiss);
        }
    }
    
    /// <summary>
    /// Shows damage number with custom color
    /// </summary>
    /// <param name="position">UI position to show damage number</param>
    /// <param name="damage">Damage amount to display</param>
    /// <param name="color">Custom color for the damage number</param>
    public void ShowDamageNumberWithColor(Vector3 position, int damage, Color color)
    {
        ShowDamageNumberAtPosition(position, damage.ToString(), color);
    }
    
    /// <summary>
    /// Shows damage number for status effects (burn, poison, etc.) with custom color
    /// </summary>
    /// <param name="target">Character taking damage</param>
    /// <param name="damage">Damage amount to display</param>
    /// <param name="color">Color for the damage number</param>
    public void ShowStatusEffectDamage(Character target, int damage, Color color)
    {
        if (target == null) return;
        
        // Get the UI position of the character
        Vector3 position = GetCharacterUIPosition(target);
        ShowDamageNumberAtPosition(position, damage.ToString(), color);
    }
    /// <summary>
    /// Hiển thị số sát thương nổi lên tại vị trí của mục tiêu với màu sắc tùy chỉnh.
    /// </summary>
    /// <param name="target">Nhân vật nhận sát thương</param>
    /// <param name="amount">Lượng sát thương</param>
    /// <param name="color">Màu sắc cho số</param>
    public void ShowDamageNumber(Character target, int amount, Color color)
    {
        // Lấy vị trí từ target để gọi hàm helper
        if (target != null)
        {
            // Có thể thêm offset Y nhỏ để số hiện trên đầu nhân vật
            Vector3 spawnPosition = target.transform.position + Vector3.up * 30f; // Ví dụ offset 30 unit Y
            ShowDamageNumberAtPosition(spawnPosition, amount.ToString(), color);
        }
    }
    /// <summary>
    /// Helper method to show damage number at position with custom color
    /// </summary>
    private void ShowDamageNumberAtPosition(Vector3 position, string text, Color color)
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
        
        // Configure damage number with custom color
        DamageNumber damageNumberScript = damageNumber.GetComponent<DamageNumber>();
        if (damageNumberScript != null)
        {
            damageNumberScript.ShowMessage(text, color);
        }
    }
    #endregion

    #region Death Effects
    // Death effects are now handled by Character.PlayDeathAnimation() which fades out the sprite
    // This method is kept for backwards compatibility but does nothing
    public void PlayDeathEffect(Character character)
    {
        // Legacy method - death effects are now handled by Character sprite animations
        // Kept for backwards compatibility
    }
    #endregion
}
