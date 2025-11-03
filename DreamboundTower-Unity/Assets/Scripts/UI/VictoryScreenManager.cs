using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI; // Để dùng TimeSpan

public class VictoryScreenManager : MonoBehaviour
{
    public TextMeshProUGUI yourTimeText;
    public TextMeshProUGUI bestTimeText;
    public Button mainMenuButton;

    void Start()
    {
        // 1. Lấy thời gian CỦA BẠN (vừa chơi xong)
        float yourTime = 0f;
        if (GameManager.Instance != null)
        {
            yourTime = GameManager.Instance.lastRunTime;
        }
        yourTimeText.text = $"TIME CLEAR: {FormatTime(yourTime)}";

        // 2. Lấy thời gian KỶ LỤC (đã lưu)
        float bestTime = RunSaveService.LoadBestTime();
        if (bestTime == float.MaxValue)
        {
            // Đây là lần đầu tiên thắng game
            bestTimeText.text = "NEW RECORD!";
        }
        else
        {
            bestTimeText.text = $"BEST TIME CLEAR: {FormatTime(bestTime)}";
        }

        // Gắn sự kiện cho nút
        mainMenuButton.onClick.AddListener(OnMainMenuButton);

        // Hiển thị con trỏ chuột
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Hàm này sẽ được gọi khi bấm nút
    public void OnMainMenuButton()
    {
        // Không cần ClearRun ở đây nữa, BattleManager đã làm rồi
        SceneManager.LoadScene("MainMenu");
    }

    // Hàm tiện ích để đổi từ giây sang định dạng MM:SS
    private string FormatTime(float timeInSeconds)
    {
        TimeSpan time = TimeSpan.FromSeconds(timeInSeconds);
        return time.ToString(@"mm\:ss"); // Ví dụ: 05:30
    }
}