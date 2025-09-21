using UnityEngine;
using UnityEngine.SceneManagement;

namespace Map
{
    public static class MapTravel
    {
        private const string PendingNodeXKey = "PendingNodeX";
        private const string PendingNodeYKey = "PendingNodeY";
        private const string PendingReturnSceneKey = "PendingReturnScene";
        private const string PendingCompletedKey = "PendingCompleted";
        private const string PendingTypeKey = "PendingNodeType";

        public static void BeginNodeBattle(Vector2Int nodePoint, string returnSceneName, string nodeType)
        {
            PlayerPrefs.SetInt(PendingNodeXKey, nodePoint.x);
            PlayerPrefs.SetInt(PendingNodeYKey, nodePoint.y);
            PlayerPrefs.SetString(PendingReturnSceneKey, returnSceneName);
            PlayerPrefs.SetString(PendingTypeKey, nodeType);
            PlayerPrefs.SetInt(PendingCompletedKey, 0);
            PlayerPrefs.Save();
        }

        public static void MarkBattleCompleted()
        {
            PlayerPrefs.SetInt(PendingCompletedKey, 1);
            PlayerPrefs.Save();
        }

        public static void ReturnToMap()
        {
            string sceneName = PlayerPrefs.GetString(PendingReturnSceneKey, string.Empty);
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }

        // Call this in Map scene after map is shown
        public static bool TryApplyPendingCompletion(MapManager mapManager)
        {
            string currentScene = SceneManager.GetActiveScene().name;
            string returnScene = PlayerPrefs.GetString(PendingReturnSceneKey, string.Empty);
            int completed = PlayerPrefs.GetInt(PendingCompletedKey, 0);
            if (completed != 1 || string.IsNullOrEmpty(returnScene) || returnScene != currentScene)
                return false;

            Vector2Int point = new Vector2Int(PlayerPrefs.GetInt(PendingNodeXKey, int.MinValue),
                PlayerPrefs.GetInt(PendingNodeYKey, int.MinValue));
            if (point.x == int.MinValue || point.y == int.MinValue)
                return false;

            if (!mapManager.CurrentMap.path.Exists(p => p.Equals(point)))
            {
                mapManager.CurrentMap.path.Add(point);
                mapManager.SaveMap();
                
                // Check if this is a boss node completion - advance floor if so
                Node completedNode = mapManager.CurrentMap.GetNode(point);
                if (completedNode != null && completedNode.nodeType == NodeType.Boss)
                {
                    Debug.Log("Boss defeated from battle scene! Advancing floor...");
                    mapManager.AdvanceFloor();
                }
            }

            if (mapManager.view != null)
            {
                mapManager.view.SetAttainableNodes();
                mapManager.view.SetLineColors();
                mapManager.view.UpdateFloorDisplay();
            }

            // clear pending
            PlayerPrefs.DeleteKey(PendingNodeXKey);
            PlayerPrefs.DeleteKey(PendingNodeYKey);
            PlayerPrefs.DeleteKey(PendingReturnSceneKey);
            PlayerPrefs.DeleteKey(PendingTypeKey);
            PlayerPrefs.DeleteKey(PendingCompletedKey);
            PlayerPrefs.Save();
            return true;
        }
    }
}


