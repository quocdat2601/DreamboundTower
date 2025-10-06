using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunSaveService
{
	private const string RunActiveKey = "Run_Active";
	private const string RunLastSceneKey = "Run_LastScene";
	private const string CurrentZoneKey = "CurrentZone";

	public static bool HasActiveRun()
	{
		return PlayerPrefs.GetInt(RunActiveKey, 0) == 1;
	}

	public static void StartNewRun(string startingSceneName, int startingZone = 1)
	{
		PlayerPrefs.SetInt(RunActiveKey, 1);
		PlayerPrefs.SetString(RunLastSceneKey, startingSceneName);
		PlayerPrefs.SetInt(CurrentZoneKey, startingZone);
		PlayerPrefs.Save();
		SceneManager.LoadScene(startingSceneName, LoadSceneMode.Single);
	}

	public static void ContinueRunOrFallback(string fallbackSceneName)
	{
		if (!HasActiveRun())
		{
			SceneManager.LoadScene(fallbackSceneName, LoadSceneMode.Single);
			return;
		}

		string lastScene = PlayerPrefs.GetString(RunLastSceneKey, string.Empty);
		if (!string.IsNullOrEmpty(lastScene))
		{
			SceneManager.LoadScene(lastScene, LoadSceneMode.Single);
			return;
		}

		// Fallback to zone scene if available
		int currentZone = PlayerPrefs.GetInt(CurrentZoneKey, 1);
		string zoneScene = currentZone >= 1 && currentZone <= 10 ? $"Zone{currentZone}" : fallbackSceneName;
		SceneManager.LoadScene(zoneScene, LoadSceneMode.Single);
	}

	public static void UpdateLastScene(string sceneName)
	{
		PlayerPrefs.SetString(RunLastSceneKey, sceneName);
		PlayerPrefs.SetInt(RunActiveKey, 1);
		PlayerPrefs.Save();
	}

	public static void ClearRun()
	{
		// Clear known run-related keys without touching user settings
		for (int zone = 1; zone <= 10; zone++)
		{
			PlayerPrefs.DeleteKey($"Zone{zone}_Map");
			PlayerPrefs.DeleteKey($"Zone{zone}_Floor");
			PlayerPrefs.DeleteKey($"Zone{zone}_SteadfastHeart");
		}
		PlayerPrefs.DeleteKey(CurrentZoneKey);
		PlayerPrefs.DeleteKey(RunLastSceneKey);
		PlayerPrefs.SetInt(RunActiveKey, 0);
		PlayerPrefs.Save();
	}
}


