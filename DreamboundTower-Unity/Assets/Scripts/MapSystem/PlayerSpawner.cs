// File: PlayerSpawner.cs
using UnityEngine;
using UnityEngine.UI; // Cần cho Image
using Presets;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab; // Kéo PlayerPrefab của bạn vào đây

    [Header("Spawn Location")]
    public Transform playerSpawnPoint; // Kéo một vị trí bất kỳ trong scene vào đây

    void Start()
    {
        SpawnPlayer();
    }

    void SpawnPlayer()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager not found. Cannot spawn player with selected data.");
            return;
        }

        RacePresetSO race = GameManager.Instance.selectedRace;
        ClassPresetSO charClass = GameManager.Instance.selectedClass;

        if (race == null || charClass == null)
        {
            Debug.LogError("No character data found in GameManager. Returning to selection scene.");
            // UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelection");
            return;
        }

        // Tạo nhân vật từ Prefab
        GameObject playerInstance = Instantiate(playerPrefab, playerSpawnPoint.position, Quaternion.identity);
        playerInstance.name = "Player";

        // Lấy các component cần thiết từ Prefab
        Character playerCharacter = playerInstance.GetComponent<Character>();
        Image playerImage = playerInstance.GetComponent<Image>(); // Dựa theo prefab của bạn

        if (playerCharacter != null)
        {
            // --- Gán chỉ số gốc từ Race ---
            StatBlock baseStats = race.baseStats;
            playerCharacter.baseMaxHP = baseStats.HP;
            playerCharacter.baseAttackPower = baseStats.STR;
            playerCharacter.baseDefense = baseStats.DEF;
            // (Bạn cần thêm baseMana, baseInt, baseAgi vào script Character.cs nếu muốn)

            // Reset lại chỉ số để áp dụng giá trị mới
            playerCharacter.ResetToBaseStats();
        }

        // --- Gán hình ảnh nhân vật ---
        if (playerImage != null)
        {
            Sprite characterSprite = null;
            switch (charClass.id)
            {
                case "class_cleric": characterSprite = race.clericSprite; break;
                case "class_mage": characterSprite = race.mageSprite; break;
                case "class_rogue": characterSprite = race.rogueSprite; break;
                case "class_warrior": characterSprite = race.warriorSprite; break;
            }
            playerImage.sprite = characterSprite;
        }
    }
}