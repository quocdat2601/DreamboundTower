using UnityEngine;

/// <summary>
/// Makes a VFX GameObject follow a target character's UI position
/// </summary>
public class VFXTracker : MonoBehaviour
{
    [Tooltip("The character to track")]
    public Character targetCharacter;
    [Tooltip("Offset from the character's position")]
    public Vector3 offset = Vector3.zero;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning($"[VFXTracker] RectTransform not found on {gameObject.name}. VFX might not track correctly.");
        }
    }

    void LateUpdate()
    {
        if (targetCharacter == null)
        {
            Destroy(gameObject); // Destroy VFX if target is gone
            return;
        }

        if (rectTransform != null && targetCharacter.characterImage != null)
        {
            // Update position to follow the target character's UI image
            rectTransform.position = targetCharacter.characterImage.transform.position + offset;
        }
        else if (rectTransform != null && targetCharacter.transform != null)
        {
            // Fallback to character's transform position if characterImage is null
            rectTransform.position = targetCharacter.transform.position + offset;
        }
    }
}

