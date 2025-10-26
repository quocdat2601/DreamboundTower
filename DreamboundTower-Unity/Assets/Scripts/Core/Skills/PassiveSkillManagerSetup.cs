// PassiveSkillManagerSetup.cs
using UnityEngine;

/// <summary>
/// Automatically creates a PassiveSkillManager GameObject if one doesn't exist
/// </summary>
public class PassiveSkillManagerSetup : MonoBehaviour
{
    void Awake()
    {
        // Check if PassiveSkillManager already exists
        if (FindFirstObjectByType<PassiveSkillManager>() == null)
        {
            // Create a new GameObject with PassiveSkillManager
            GameObject passiveSkillManagerGO = new GameObject("PassiveSkillManager");
            passiveSkillManagerGO.AddComponent<PassiveSkillManager>();
            
            // Make it persistent across scenes
            DontDestroyOnLoad(passiveSkillManagerGO);
            
            Debug.Log("[PASSIVE] Created PassiveSkillManager GameObject");
        }
    }
}
