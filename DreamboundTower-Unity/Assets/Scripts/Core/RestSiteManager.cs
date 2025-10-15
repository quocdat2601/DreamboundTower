using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RestSiteManager : MonoBehaviour
{
    [Header("UI References")]
    public Button restAndLeaveButton;

    private Character playerCharacter;

    void Start()
    {
        if (GameManager.Instance == null || GameManager.Instance.playerInstance == null)
        {
            Debug.LogError("Không tìm thấy người chơi!");
            restAndLeaveButton.interactable = false;
            return;
        }
        playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
        restAndLeaveButton.onClick.AddListener(PerformRestAndLeave);
    }

    private void PerformRestAndLeave()
    {
        restAndLeaveButton.interactable = false;

        Debug.Log("[REST] Performing all rest actions...");
        playerCharacter.HealPercentage(0.5f);
        playerCharacter.RestoreFullMana();
        playerCharacter.RemoveAllNegativeStatusEffects();

        // KHÔNG CẦN CẬP NHẬT TEXT CỤC BỘ NỮA
        // UI bất tử của GameManager sẽ tự động cập nhật thông qua events.

        GameManager.Instance.SavePlayerStateToRunData();

        var runData = GameManager.Instance.currentRunData;
        string zoneSceneToLoad = "Zone" + runData.mapData.currentZone;
        Debug.Log($"[REST] Leaving Rest Site. Returning to {zoneSceneToLoad}.");
        SceneManager.LoadScene(zoneSceneToLoad);
    }
}