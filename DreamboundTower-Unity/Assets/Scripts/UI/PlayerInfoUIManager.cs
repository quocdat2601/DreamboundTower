using UnityEngine;
using UnityEngine.UI; // Cần cho Button (hoặc ToggleSwitch nếu bạn dùng)
using TMPro; // Cần cho TextMeshPro
using Presets; // Có thể cần nếu bạn muốn lấy tên Class/Race sau này
// using StatusEffects; // Sẽ cần sau này cho Status

public class PlayerInfoUIManager : MonoBehaviour
{
    [Header("Panel References")]
    [Tooltip("Kéo GameObject BagPanel vào đây")]
    public GameObject bagPanel;
    [Tooltip("Kéo GameObject StatsPanel vào đây")]
    public GameObject statsPanel;

    [Header("Toggle Control")]
    [Tooltip("Kéo GameObject chứa script ToggleSwitch (InfoToggleSlider) vào đây")]
    public ToggleSwitch infoToggleSwitch; // Sử dụng kiểu ToggleSwitch bạn đã tạo

    [Header("Stats Panel Texts")]
    [Tooltip("Text hiển thị HP Tối đa")]
    public TextMeshProUGUI hpStatText;
    [Tooltip("Text hiển thị STR")]
    public TextMeshProUGUI strStatText;
    [Tooltip("Text hiển thị DEF")]
    public TextMeshProUGUI defStatText;
    [Tooltip("Text hiển thị INT")]
    public TextMeshProUGUI intStatText;
    [Tooltip("Text hiển thị MANA Tối đa")]
    public TextMeshProUGUI manaStatText;
    [Tooltip("Text hiển thị AGI")]
    public TextMeshProUGUI agiStatText;
    [Tooltip("Text hiển thị Crit")]
    public TextMeshProUGUI critStatText;
    // (Thêm các Text khác nếu bạn muốn hiển thị thêm chỉ số)

    // Tham chiếu đến Character của người chơi (sẽ lấy trong Start)
    private Character playerCharacter;

    void Start()
    {
        // 1. Lấy tham chiếu Player Character
        FindPlayerCharacter();

        // 2. Thiết lập trạng thái ban đầu (hiện Bag, ẩn Stats)
        // (Chúng ta sẽ làm điều này bằng cách gọi hàm ShowBagPanel)
        ShowBagPanel(); // Gọi hàm này để đảm bảo trạng thái UI đúng lúc bắt đầu

        // 3. Kết nối sự kiện Toggle (Sẽ làm ở Bước sau)
        // SetupToggleEvents(); // Tạm thời comment lại
    }

    // Hàm tìm Player Character
    private void FindPlayerCharacter()
    {
        if (GameManager.Instance != null && GameManager.Instance.playerInstance != null)
        {
            playerCharacter = GameManager.Instance.playerInstance.GetComponent<Character>();
            if (playerCharacter == null)
            {
                Debug.LogError("PlayerInfoUIManager: Không tìm thấy component Character trên playerInstance!");
            }
        }
        else
        {
            // Lỗi này có thể xảy ra nếu script chạy trước GameManager, cân nhắc dùng coroutine đợi
            Debug.LogError("PlayerInfoUIManager: Không tìm thấy GameManager Instance hoặc playerInstance!");
        }
    }

    // Hàm được gọi khi Toggle Bật (Hiện Stats) (Sẽ kết nối ở Bước 3)
    public void ShowStatsPanel()
    {
        if (statsPanel != null) statsPanel.SetActive(true);
        if (bagPanel != null) bagPanel.SetActive(false);
        UpdateStatsDisplay(); // Gọi cập nhật khi bật panel này
        Debug.Log("Showing Stats Panel");
    }

    // Hàm được gọi khi Toggle Tắt (Hiện Bag) (Sẽ kết nối ở Bước 3)
    public void ShowBagPanel()
    {
        if (statsPanel != null) statsPanel.SetActive(false);
        if (bagPanel != null) bagPanel.SetActive(true);
        Debug.Log("Showing Bag Panel");
    }
    private const int HP_UNIT = 10;
    private const int MANA_UNIT = 5;
    public void UpdateStatsDisplay()
    {
        // Kiểm tra xem đã tìm thấy playerCharacter chưa
        if (playerCharacter == null)
        {
            Debug.LogWarning("PlayerInfoUIManager: Chưa có tham chiếu đến playerCharacter để cập nhật Stats!");
            // (Tùy chọn: Hiển thị "N/A" hoặc để trống các ô Text)
            hpStatText.text = "HP: --";
            strStatText.text = "STR: --";
            defStatText.text = "DEF: --";
            intStatText.text = "INT: --";
            manaStatText.text = "MANA: --";
            agiStatText.text = "AGI: --";
            critStatText.text = "CR: --";
            return;
        }

        // Đọc chỉ số THỰC TẾ từ playerCharacter và gán vào Text
        // (Đảm bảo các biến tham chiếu Text đã được kéo vào Inspector)
        if (hpStatText != null)
        {
            // Lấy baseMaxHP và chia cho HP_UNIT để ra chỉ số gốc
            int baseHPStat = Mathf.RoundToInt((float)playerCharacter.baseMaxHP / HP_UNIT);
            hpStatText.text = $"HP: {baseHPStat}"; // Hiển thị chỉ số HP gốc
        }
        if (strStatText != null)
        {
            int baseStr = playerCharacter.baseAttackPower;
            int deltaStr = playerCharacter.attackPower - baseStr;
            strStatText.text = deltaStr != 0 ? $"STR: {baseStr} {(deltaStr > 0 ? "+" : "")} {deltaStr}".Replace("  ", " ") : $"STR: {baseStr}";
        }
        if (defStatText != null)
        {
            int baseDef = playerCharacter.baseDefense;
            int deltaDef = playerCharacter.defense - baseDef;
            defStatText.text = deltaDef != 0 ? $"DEF: {baseDef} {(deltaDef > 0 ? "+" : "")} {deltaDef}".Replace("  ", " ") : $"DEF: {baseDef}";
        }
        if (intStatText != null)
        {
            int baseInt = playerCharacter.baseIntelligence;
            int deltaInt = playerCharacter.intelligence - baseInt;
            intStatText.text = deltaInt != 0 ? $"INT: {baseInt} {(deltaInt > 0 ? "+" : "")} {deltaInt}".Replace("  ", " ") : $"INT: {baseInt}";
        }
        if (manaStatText != null)
        {
            // Lấy baseMana và chia cho MANA_UNIT để ra chỉ số gốc
            int baseManaStat = Mathf.RoundToInt((float)playerCharacter.baseMana / MANA_UNIT);
            manaStatText.text = $"MANA: {baseManaStat}"; // Hiển thị chỉ số MANA gốc
        }
        if (agiStatText != null) agiStatText.text = $"AGI: {playerCharacter.agility}";
        if (critStatText != null) critStatText.text = $"CR: {playerCharacter.criticalChance}";

        // (Nếu bạn có thêm Text cho các chỉ số khác, cập nhật chúng ở đây)
        Debug.Log("Stats Panel Updated!"); // Thêm log để biết hàm đã chạy
    }

}