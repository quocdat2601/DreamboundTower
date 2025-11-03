using UnityEngine;
using UnityEngine.InputSystem;

public class CheckpointScript : MonoBehaviour
{
    private AudioManager audioManager;
    private bool triggered = false;

    private void Awake()
    {
        var audioObject = GameObject.FindGameObjectWithTag("Audio");
        if (audioObject) audioManager = audioObject.GetComponent<AudioManager>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        triggered = true;

        if (audioManager) audioManager.PlaySFX(audioManager.finish);

        // Tắt input của player (tuỳ chọn)
        var pi = other.GetComponent<PlayerInput>();
        if (pi) pi.enabled = false;

        // >>> GỌI GameManager để xử lý finish + bật sao <<<
        var gm = FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            Debug.Log("[Checkpoint] Reached finish -> GameManager.FinishGame()");
            gm.FinishGame();
        }
        else
        {
            Debug.LogError("[Checkpoint] GameManager not found!");
        }
    }
}
