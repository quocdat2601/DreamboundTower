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

        // Combat payload keys
        private const string EnemyPendingKey = "EnemyPending";
        private const string EnemyKindKey = "EnemyKind"; // 0 Normal,1 Elite,2 Boss
        private const string EnemyHPKey = "EnemyHP";
        private const string EnemySTRKey = "EnemySTR";
        private const string EnemyDEFKey = "EnemyDEF";
        private const string EnemyMANAKey = "EnemyMANA";
        private const string EnemyINTKey = "EnemyINT";
        private const string EnemyAGIKey = "EnemyAGI";
        private const string EnemyAbsFloorKey = "EnemyAbsFloor";
        private const string EnemyArchetypeKey = "EnemyArchetype";

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

        // Store enemy payload for next combat via PlayerPrefs (scene-safe)
        public static void SetPendingEnemy(int enemyKind, int hp, int str, int def, int mana, int intel, int agi, int absFloor, string archetype)
        {
            PlayerPrefs.SetInt(EnemyPendingKey, 1);
            PlayerPrefs.SetInt(EnemyKindKey, enemyKind);
            PlayerPrefs.SetInt(EnemyHPKey, hp);
            PlayerPrefs.SetInt(EnemySTRKey, str);
            PlayerPrefs.SetInt(EnemyDEFKey, def);
            PlayerPrefs.SetInt(EnemyMANAKey, mana);
            PlayerPrefs.SetInt(EnemyINTKey, intel);
            PlayerPrefs.SetInt(EnemyAGIKey, agi);
            PlayerPrefs.SetInt(EnemyAbsFloorKey, absFloor);
            PlayerPrefs.SetString(EnemyArchetypeKey, archetype ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static bool TryReadAndClearPendingEnemy(out int enemyKind, out int hp, out int str, out int def, out int mana, out int intel, out int agi, out int absFloor, out string archetype)
        {
            enemyKind = hp = str = def = mana = intel = agi = absFloor = 0;
            archetype = string.Empty;
            if (PlayerPrefs.GetInt(EnemyPendingKey, 0) != 1)
                return false;

            enemyKind = PlayerPrefs.GetInt(EnemyKindKey, 0);
            hp = PlayerPrefs.GetInt(EnemyHPKey, 0);
            str = PlayerPrefs.GetInt(EnemySTRKey, 0);
            def = PlayerPrefs.GetInt(EnemyDEFKey, 0);
            mana = PlayerPrefs.GetInt(EnemyMANAKey, 0);
            intel = PlayerPrefs.GetInt(EnemyINTKey, 0);
            agi = PlayerPrefs.GetInt(EnemyAGIKey, 0);
            absFloor = PlayerPrefs.GetInt(EnemyAbsFloorKey, 0);
            archetype = PlayerPrefs.GetString(EnemyArchetypeKey, string.Empty);

            PlayerPrefs.DeleteKey(EnemyPendingKey);
            PlayerPrefs.DeleteKey(EnemyKindKey);
            PlayerPrefs.DeleteKey(EnemyHPKey);
            PlayerPrefs.DeleteKey(EnemySTRKey);
            PlayerPrefs.DeleteKey(EnemyDEFKey);
            PlayerPrefs.DeleteKey(EnemyMANAKey);
            PlayerPrefs.DeleteKey(EnemyINTKey);
            PlayerPrefs.DeleteKey(EnemyAGIKey);
            PlayerPrefs.DeleteKey(EnemyAbsFloorKey);
            PlayerPrefs.DeleteKey(EnemyArchetypeKey);
            PlayerPrefs.Save();
            return true;
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


