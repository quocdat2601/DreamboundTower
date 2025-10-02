using UnityEngine;
using UnityEngine.SceneManagement;
using Presets;
using UnityEngine.UI; // Cần để dùng Image

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")]
    public RacePresetSO selectedRace;
    public ClassPresetSO selectedClass;
    public GameObject playerInstance; // Lưu trữ tham chiếu đến người chơi

    [Header("Prefabs")]
    public GameObject playerPrefab; // Kéo PlayerPrefab vào đây trong Inspector

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Nếu người chơi đã được tạo và scene mới không phải là Battle Scene
        if (playerInstance != null && scene.name != "MainGame") // Thay "MainGame" bằng tên Battle Scene của bạn
        {
            // Ẩn người chơi đi
            playerInstance.SetActive(false);
        }
    }
    public void InitializePlayerCharacter()
    {
        if (playerPrefab == null || selectedRace == null || selectedClass == null) return;

        // Tạo nhân vật
        playerInstance = Instantiate(playerPrefab);
        playerInstance.name = "PlayerCharacter_DontDestroy";
        DontDestroyOnLoad(playerInstance);

        // Cấu hình chỉ số
        Character playerCharacter = playerInstance.GetComponent<Character>();
        if (playerCharacter != null)
        {
            StatBlock baseStats = selectedRace.baseStats;
            playerCharacter.baseMaxHP = baseStats.HP;
            playerCharacter.baseAttackPower = baseStats.STR;
            playerCharacter.baseDefense = baseStats.DEF;
            playerCharacter.baseMana = baseStats.MANA;        
            playerCharacter.baseIntelligence = baseStats.INT;  
            playerCharacter.baseAgility = baseStats.AGI;       
            playerCharacter.ResetToBaseStats();
        }

        // Cấu hình hình ảnh
        Image playerImage = playerInstance.GetComponent<Image>();
        if (playerImage != null)
        {
            Sprite characterSprite = null;
            switch (selectedClass.id)
            {
                case "class_cleric": characterSprite = selectedRace.clericSprite; break;
                case "class_mage": characterSprite = selectedRace.mageSprite; break;
                case "class_warrior": characterSprite = selectedRace.warriorSprite; break;
                case "class_rogue": characterSprite = selectedRace.rogueSprite; break;
            }
            playerImage.sprite = characterSprite;
        }

        // --- BƯỚC CẬP NHẬT: Cấu hình Kỹ năng ---
        PlayerSkills playerSkills = playerInstance.GetComponent<PlayerSkills>();
        if (playerSkills != null)
        {
            // Dạy cho người chơi các skill từ Race và Class đã chọn
            playerSkills.LearnSkills(selectedRace, selectedClass);
        }
    }
    public void LoadNextScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // --- PHẦN THÊM MỚI ---
    private void OnDestroy()
    {
        // Hủy đăng ký để tránh lỗi
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}