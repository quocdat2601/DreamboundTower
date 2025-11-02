using UnityEngine;

public static class RunSaveService
{
    private const string RunActiveKey = "Run_Active";
    private const string RunDataKey = "Run_Data"; // Chỉ một key cho toàn bộ dữ liệu
    private const string BEST_TIME_KEY = "BestClearTime";

    public static bool HasActiveRun()
    {
        return PlayerPrefs.GetInt(RunActiveKey, 0) == 1;
    }

    public static void SaveRun(RunData data)
    {
        // ✅ THÊM KHỐI KIỂM TRA NÀY
        if (GameManager.Instance != null && GameManager.Instance.isDebugRun)
        {
            Debug.LogWarning("[RunSaveService] Đang ở chế độ Debug, đã BỎ QUA LƯU GAME.");
            return; // Không lưu gì cả
        }
        if (data == null) return;

        string json = JsonUtility.ToJson(data); // Chuyển cả "hộp" thành JSON
        PlayerPrefs.SetString(RunDataKey, json);
        PlayerPrefs.SetInt(RunActiveKey, 1); // Đánh dấu là có run đang hoạt động
        PlayerPrefs.Save();
        Debug.Log("Run data saved!");
    }

    public static RunData LoadRun()
    {
        if (!HasActiveRun()) return null;

        string json = PlayerPrefs.GetString(RunDataKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return null;

        return JsonUtility.FromJson<RunData>(json); // Chuyển JSON về lại "hộp"
    }

    public static void ClearRun()
    {
        // ✅ THÊM KHỐI KIỂM TRA NÀY
        if (GameManager.Instance != null && GameManager.Instance.isDebugRun)
        {
            Debug.LogWarning("[RunSaveService] Đang ở chế độ Debug, đã BỎ QUA XÓA LƯU.");
            return; // Không xóa gì cả
        }
        PlayerPrefs.DeleteKey(RunDataKey); // Chỉ cần xóa 1 key duy nhất
        PlayerPrefs.SetInt(RunActiveKey, 0);
        PlayerPrefs.Save();
        Debug.Log("Run data cleared!");
    }

    /// <summary>
    /// Lưu thời gian hoàn thành tốt nhất (tính bằng giây).
    /// </summary>
    public static void SaveBestTime(float timeInSeconds)
    {
        // Lưu lại nếu thời gian mới tốt hơn (nhỏ hơn) thời gian cũ
        float currentBest = LoadBestTime();
        if (timeInSeconds < currentBest)
        {
            PlayerPrefs.SetFloat(BEST_TIME_KEY, timeInSeconds);
            PlayerPrefs.Save();
            Debug.Log($"[GameStats] New Best Time Saved: {timeInSeconds}s");
        }
    }

    /// <summary>
    /// Tải thời gian hoàn thành tốt nhất (tính bằng giây).
    /// </summary>
    /// <returns>Thời gian tốt nhất, hoặc float.MaxValue nếu chưa có.</returns>
    public static float LoadBestTime()
    {
        // Trả về thời gian đã lưu, hoặc một giá trị "vô cực" nếu chưa từng lưu
        return PlayerPrefs.GetFloat(BEST_TIME_KEY, float.MaxValue);
    }
}