using UnityEngine;
using DG.Tweening; // Make sure you have DOTween imported
using StatusEffects; // Make sure you have this namespace for StunEffect

[RequireComponent(typeof(Character))]
public class HordeSummonerBehavior : MonoBehaviour
{
    private Character bossCharacter;
    private BattleManager battleManager;
    private bool hasTriggered = false; // Flag to ensure it only happens once

    void Awake()
    {
        bossCharacter = GetComponent<Character>();
        if (bossCharacter != null)
        {
            // Listen for when this character takes damage
            bossCharacter.OnHealthChanged += HandleHealthChanged;
        }
    }

    void Start()
    {
        // Find the BattleManager
        battleManager = FindAnyObjectByType<BattleManager>();
        if (battleManager == null)
        {
            Debug.LogError("HordeSummonerBehavior: Cannot find BattleManager!");
        }
    }

    void OnDestroy()
    {
        // Always unsubscribe from events
        if (bossCharacter != null)
        {
            bossCharacter.OnHealthChanged -= HandleHealthChanged;
        }
    }

    /// <summary>
    /// Called every time the Boss's health changes.
    /// </summary>
    private void HandleHealthChanged(int currentHP, int maxHP)
    {
        // Check if HP just dropped below 50% AND this hasn't triggered yet
        if (!hasTriggered && (float)currentHP / maxHP <= 0.5f)
        {
            TriggerHordeSummon();
        }
    }

    /// <summary>
    /// Freezes the boss and starts the Pillar spawning process.
    /// </summary>
    private void TriggerHordeSummon()
    {
        if (hasTriggered) return; // Safety check
        hasTriggered = true;

        Debug.Log($"<color=purple>[{bossCharacter.name}] HP below 50%. Activating HordeSummoner! Freezing...</color>");

        // 1. Freeze the Boss
        bossCharacter.isInvincible = true;
        bossCharacter.isUntargetable = true;
        if (bossCharacter.characterImage != null)
        {
            bossCharacter.characterImage.DOColor(new Color(0.5f, 0.7f, 1f), 0.5f); // Change to blue-ish tint
        }

        // 2. Tell BattleManager to spawn the Pillar
        if (battleManager != null)
        {
            battleManager.ClearTargetSelection();
            // We pass this boss's character component so the Pillar knows who to unfreeze
            battleManager.AttemptSpawnPillar(bossCharacter);
        }
    }

    /// <summary>
    /// This public function is called by the Pillar when it dies.
    /// </summary>
    public void BreakFreeze()
    {
        Debug.Log($"<color=purple>[{bossCharacter.name}] Pillar destroyed! Breaking freeze...</color>");
        bossCharacter.isInvincible = false;
        bossCharacter.isUntargetable = false;
        if (bossCharacter.characterImage != null)
        {
            bossCharacter.characterImage.DOColor(Color.white, 0.5f); // Return to normal color
        }
    }
}