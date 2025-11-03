using UnityEngine;
using System.Collections.Generic;
using Presets;

[RequireComponent(typeof(Character))]
public class PillarBehavior : MonoBehaviour
{
    private Character pillarCharacter;
    private BattleManager battleManager;
    private Character mainBoss; // The Boss this Pillar must unfreeze

    private int summonCooldown = 5; // Cooldown between summons
    private int currentCooldown = 0; // Starts at 0 to summon immediately

    void Awake()
    {
        pillarCharacter = GetComponent<Character>();
        battleManager = FindAnyObjectByType<BattleManager>();

        if (pillarCharacter != null)
        {
            // Listen for its own death
            pillarCharacter.OnDeath += HandleDeath;
        }
    }

    void OnDestroy()
    {
        if (pillarCharacter != null)
        {
            pillarCharacter.OnDeath -= HandleDeath;
        }
    }

    /// <summary>
    /// Called by BattleManager when this Pillar is first spawned.
    /// </summary>
    public void Initialize(Character bossToUnfreeze)
    {
        this.mainBoss = bossToUnfreeze;
    }

    /// <summary>
    /// Called by BattleManager (from EnemyTurnRoutine) when it's this Pillar's turn.
    /// </summary>
    public void HandleTurn()
    {
        if (battleManager == null) return;

        // Check if cooldown is over
        if (currentCooldown <= 0)
        {
            Debug.Log($"<color=cyan>[{pillarCharacter.name}] Attempting to summon Sub-Boss...</color>");
            // Try to spawn the F80 Sub-Boss
            bool success = battleManager.AttemptSpawnSubBoss(80); // 80 = stats floor

            if (success)
            {
                Debug.Log($"<color=cyan>[{pillarCharacter.name}] Summon successful! Cooldown set to {summonCooldown}.</color>");
                currentCooldown = summonCooldown; // Reset cooldown
            }
            else
            {
                Debug.LogWarning($"<color=orange>[{pillarCharacter.name}] Summon failed (no slots?). Retrying next turn.</color>");
                currentCooldown = 0; // Stay at 0 to retry next turn
            }
        }
        else
        {
            // Not time to summon, just tick down the cooldown
            Debug.Log($"<color=cyan>[{pillarCharacter.name}] Summon cooldown: {currentCooldown}</color>");
            currentCooldown--;
        }
    }

    /// <summary>
    /// Called when this Pillar is destroyed.
    /// </summary>
    private void HandleDeath(Character self)
    {
        Debug.Log($"<color=red>[{pillarCharacter.name}] Pillar destroyed! Unfreezing main boss...</color>");

        // Check if the main boss is still alive
        if (mainBoss != null && mainBoss.currentHP > 0)
        {
            HordeSummonerBehavior bossGimmick = mainBoss.GetComponent<HordeSummonerBehavior>();
            if (bossGimmick != null)
            {
                // Tell the boss to break free
                bossGimmick.BreakFreeze();
            }
        }
    }
}