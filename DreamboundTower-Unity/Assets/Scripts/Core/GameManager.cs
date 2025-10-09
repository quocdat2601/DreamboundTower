using Presets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Cần để dùng Image

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Data")]
    //public RacePresetSO selectedRace;
    //public ClassPresetSO selectedClass;
    public RunData currentRunData;
    public GameObject playerInstance; // Lưu trữ tham chiếu đến người chơi

    [Header("Prefabs")]
    public GameObject playerPrefab; // Kéo PlayerPrefab vào đây trong Inspector

    [Header("Asset Databases")] // <-- THÊM MỚI
    public List<RacePresetSO> allRaces;
    public List<ClassPresetSO> allClasses;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (SceneManager.GetActiveScene().name == "Initialization")
        {
            SceneManager.LoadScene("MainMenu");
        }
        if (currentRunData == null)
        {
            currentRunData = new RunData();
        }
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

    // Hàm tìm RacePresetSO dựa trên ID
    public RacePresetSO GetRaceByID(string id)
    {
        return allRaces.Find(race => race.id == id);
    }

    // Hàm tìm ClassPresetSO dựa trên ID
    public ClassPresetSO GetClassByID(string id)
    {
        return allClasses.Find(cls => cls.id == id);
    }
    public void InitializePlayerCharacter()
    {
        // Bước 1: Đọc "bản thiết kế" từ currentRunData
        if (playerPrefab == null || currentRunData == null) return;

        string raceId = currentRunData.playerData.selectedRaceId;
        string classId = currentRunData.playerData.selectedClassId;

        if (string.IsNullOrEmpty(raceId) || string.IsNullOrEmpty(classId))
        {
            Debug.LogError("Race ID hoặc Class ID trong RunData trống!");
            return;
        }

        // Bước 2: Lấy "nguyên vật liệu" (ScriptableObjects) từ kho bằng cách tra cứu ID
        RacePresetSO raceData = GetRaceByID(raceId);
        ClassPresetSO classData = GetClassByID(classId);

        if (raceData == null || classData == null)
        {
            Debug.LogError("Không tìm thấy Race/Class Preset với ID tương ứng!");
            return;
        }

        // Bước 3: Bắt đầu "xây nhà" (Instantiate và cấu hình playerInstance)
        // Nếu đã có instance cũ, hủy nó đi để tạo cái mới
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }

        playerInstance = Instantiate(playerPrefab);
        playerInstance.name = "PlayerCharacter_DontDestroy";
        DontDestroyOnLoad(playerInstance);

        // Cấu hình chỉ số
        Character playerCharacter = playerInstance.GetComponent<Character>();
        if (playerCharacter != null)
        {
            StatBlock baseStats = raceData.baseStats; // Dùng raceData
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
            switch (classData.id) // Dùng classData
            {
                case "class_cleric": characterSprite = raceData.clericSprite; break;
                case "class_mage": characterSprite = raceData.mageSprite; break;
                case "class_warrior": characterSprite = raceData.warriorSprite; break;
                case "class_rogue": characterSprite = raceData.rogueSprite; break;
            }
            playerImage.sprite = characterSprite;
        }

        // Cấu hình Kỹ năng
        PlayerSkills playerSkills = playerInstance.GetComponent<PlayerSkills>();
        if (playerSkills != null)
        {
            playerSkills.LearnSkills(raceData, classData); // Dùng raceData và classData
        }

        Debug.Log("Player Character Initialized from RunData!");
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

    // HÀM MỚI CHO NÚT "NEW GAME"
    public void StartNewGame()
    {
        // Tạo một "hộp" RunData mới hoàn toàn
        currentRunData = new RunData();

        // Hủy player instance cũ nếu có
        if (playerInstance != null)
        {
            Destroy(playerInstance);
        }

        // Chuyển đến màn hình chọn nhân vật
        SceneManager.LoadScene("Character Selection");
    }
    public void ContinueGame()
    {
        // 1. Tải "hộp" RunData từ file lưu
        currentRunData = RunSaveService.LoadRun();

        if (currentRunData == null)
        {
            Debug.LogError("Failed to load run data! Starting a new game as a fallback.");
            StartNewGame();
            return;
        }

        // 2. Xây dựng lại "ngôi nhà" (playerInstance) từ "bản thiết kế" (RunData)
        InitializePlayerCharacter();

        // 3. LOGIC QUAN TRỌNG: KIỂM TRA TRẠNG THÁI "PENDING"
        if (currentRunData.mapData.pendingNodePoint.x != -1 && currentRunData.mapData.pendingNodePoint.y != -1)
        {
            // --- TRƯỜNG HỢP 1: NGƯỜI CHƠI ĐÃ THOÁT GIỮA CHỪNG ---
            Debug.Log($"Pending node detected at {currentRunData.mapData.pendingNodePoint}. Returning player to the action...");

            string sceneToLoad = currentRunData.mapData.pendingNodeSceneName;

            // KIỂM TRA AN TOÀN: Đề phòng trường hợp tên scene bị rỗng
            if (string.IsNullOrEmpty(sceneToLoad))
            {
                Debug.LogError($"Pending node was found, but scene name is empty! Fallback to map to avoid crashing.");

                // Xóa trạng thái pending bị lỗi và lưu lại để sửa file save
                currentRunData.mapData.pendingNodePoint = new Vector2Int(-1, -1);
                RunSaveService.SaveRun(currentRunData);

                // Tải lại scene map một cách an toàn
                SceneManager.LoadScene($"Zone{currentRunData.mapData.currentZone}");
            }
            else
            {
                // Tải scene đã được lưu một cách bình thường
                SceneManager.LoadScene(sceneToLoad);
            }
        }
        else
        {
            // --- TRƯỜNG HỢP 2: NGƯỜI CHƠI AN TOÀN TRÊN MAP ---
            string sceneToLoad = $"Zone{currentRunData.mapData.currentZone}";
            Debug.Log($"No pending node. Returning to map scene: {sceneToLoad}");
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}