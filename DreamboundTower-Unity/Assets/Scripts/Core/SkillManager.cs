// SkillManager.cs
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Data;

/// <summary>
/// Manages skill cooldowns, validation, and usage for the battle system
/// </summary>
public class SkillManager : MonoBehaviour
{
    #region Variables
    
    [Header("Skill System")]
    [Tooltip("Reference to the player character for mana and stat checks")]
    public Character playerCharacter;
    
    [Tooltip("Reference to the battle manager for turn state")]
    public BattleManager battleManager;
    
    // skill cooldowns tracking
    private Dictionary<SkillData, int> skillCooldowns = new Dictionary<SkillData, int>();
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        // Initialize cooldown dictionary
        skillCooldowns = new Dictionary<SkillData, int>();
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Checks if a skill can be used (mana, cooldown, turn state)
    /// </summary>
    public bool CanUseSkill(SkillData skill)
    {
        
        if (skill == null || battleManager == null) 
        {
            Debug.LogWarning($"[SKILL] Skill or BattleManager is null. Skill: {skill != null}, BattleManager: {battleManager != null}");
            return false;
        }
        
        // Auto-assign player character if not set
        if (playerCharacter == null)
        {
            AutoAssignPlayerCharacter();
            if (playerCharacter == null) 
            {
                Debug.LogError("[SKILL] Failed to auto-assign player character!");
                return false;
            }
        }
        
        
        // Check mana cost
        if (playerCharacter.currentMana < skill.cost) 
        {
            Debug.LogWarning($"[SKILL] Insufficient mana! Need {skill.cost}, have {playerCharacter.currentMana}");
            return false;
        }
        
        // Check cooldown
        if (IsSkillOnCooldown(skill)) 
        {
            Debug.LogWarning($"[SKILL] Skill is on cooldown! Turns remaining: {GetSkillCooldown(skill)}");
            return false;
        }
        
        // Check if it's player turn
        if (!battleManager.PlayerTurn) 
        {
            Debug.LogWarning($"[SKILL] Not player turn! PlayerTurn: {battleManager.PlayerTurn}");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Checks if a skill is currently on cooldown
    /// </summary>
    public bool IsSkillOnCooldown(SkillData skill)
    {
        if (skill == null) return false;
        
        if (skillCooldowns.ContainsKey(skill))
        {
            return skillCooldowns[skill] > 0;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets the remaining cooldown turns for a skill
    /// </summary>
    public int GetSkillCooldown(SkillData skill)
    {
        if (skill == null) return 0;
        
        if (skillCooldowns.ContainsKey(skill))
        {
            return skillCooldowns[skill];
        }
        
        return 0;
    }
    
    /// <summary>
    /// Calculates skill damage with stat scaling
    /// </summary>
    public int CalculateSkillDamage(SkillData skill)
    {
        if (skill == null) return 0;
        
        // Auto-assign player character if not set
        if (playerCharacter == null)
        {
            AutoAssignPlayerCharacter();
            if (playerCharacter == null) return 0;
        }
        
        int baseDamage = skill.baseDamage;
        int scalingValue = 0;

        // Get scaling stat value
        switch (skill.scalingStat)
        {
            case StatType.STR:
                scalingValue = playerCharacter.attackPower;
                break;
            case StatType.INT:
                scalingValue = playerCharacter.intelligence;
                break;
            case StatType.AGI:
                scalingValue = playerCharacter.agility;
                break;
            default:
                scalingValue = playerCharacter.attackPower; // Default to STR
                break;
        }

        // Calculate final damage
        int scaledDamage = Mathf.RoundToInt(scalingValue * skill.scalingPercent);
        int totalDamage = baseDamage + scaledDamage;

        return Mathf.Max(1, totalDamage); // Ensure minimum 1 damage
    }
    
    /// <summary>
    /// Consumes mana and sets skill on cooldown
    /// </summary>
    public void UseSkill(SkillData skill)
    {
        if (skill == null) return;
        
        // Auto-assign player character if not set
        if (playerCharacter == null)
        {
            AutoAssignPlayerCharacter();
            if (playerCharacter == null) return;
        }
        
        // Consume mana
        playerCharacter.currentMana -= skill.cost;
        playerCharacter.UpdateManaUI();
        
        // Set skill on cooldown
        SetSkillCooldown(skill);
        
    }
    
    /// <summary>
    /// Reduces all skill cooldowns by 1 turn (call this at the end of each turn)
    /// </summary>
    public void ReduceSkillCooldowns()
    {
        var skillsToUpdate = new List<SkillData>();
        
        foreach (var kvp in skillCooldowns)
        {
            if (kvp.Value > 0)
            {
                skillsToUpdate.Add(kvp.Key);
            }
        }
        
        foreach (var skill in skillsToUpdate)
        {
            skillCooldowns[skill]--;
        }
    }
    
    /// <summary>
    /// Resets all skill cooldowns (useful for battle start/end)
    /// </summary>
    public void ResetAllCooldowns()
    {
        skillCooldowns.Clear();
    }
    
    /// <summary>
    /// Manually assigns the player character (called by BattleManager)
    /// </summary>
    public void AssignPlayerCharacter(Character character)
    {
        playerCharacter = character;
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Automatically finds and assigns the player character
    /// </summary>
    private void AutoAssignPlayerCharacter()
    {
        
        // Try to get player from GameManager first
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerCharacter != null)
            {
                return;
            }
            else
            {
                Debug.LogWarning("[SKILL] GameManager.playerInstance found but no Character component!");
            }
        }
        else
        {
            Debug.LogWarning("[SKILL] GameManager or playerInstance not found!");
        }
        
        // Fallback: Find player character in scene
        Character[] characters = FindObjectsByType<Character>(FindObjectsSortMode.None);
        foreach (Character character in characters)
        {
            if (character.isPlayer)
            {
                playerCharacter = character;
                return;
            }
        }
        
        // Last resort: Find any character with "Player" in the name
        foreach (Character character in characters)
        {
            if (character.name.ToLower().Contains("player"))
            {
                playerCharacter = character;
                return;
            }
        }
        
        Debug.LogWarning("[SKILL] Could not auto-assign player character!");
    }
    
    /// <summary>
    /// Sets a skill on cooldown after use
    /// </summary>
    private void SetSkillCooldown(SkillData skill)
    {
        if (skill == null || skill.cooldown <= 0) return;
        
        skillCooldowns[skill] = skill.cooldown;
    }
    
    #endregion
}
