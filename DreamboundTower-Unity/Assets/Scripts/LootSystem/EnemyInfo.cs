using UnityEngine;
using Presets;

/// <summary>
/// Simple component to store enemy information (kind and template).
/// Attached to enemy GameObjects during spawn by BattleManager.
/// </summary>
public class EnemyInfo : MonoBehaviour
{
    [Header("Enemy Information")]
    [Tooltip("The type of enemy (Normal, Elite, Boss)")]
    public EnemyKind enemyKind = EnemyKind.Normal;
    
    [Tooltip("Reference to the enemy template (optional)")]
    public EnemyTemplateSO enemyTemplate;
    
    void Reset()
    {
        // Default to Normal if not set
        enemyKind = EnemyKind.Normal;
    }
}


