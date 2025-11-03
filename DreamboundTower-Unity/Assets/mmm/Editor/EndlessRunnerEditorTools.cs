using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class EndlessRunnerEditorTools
{
	[MenuItem("Tools/Endless Runner/Create Chunk From Selection", priority = 0)]
	public static void CreateChunkFromSelection()
	{
		GameObject[] selected = Selection.gameObjects;
		if (selected == null || selected.Length == 0)
		{
			EditorUtility.DisplayDialog("Create Chunk", "Hãy chọn các platform/obstacle trong Hierarchy trước.", "OK");
			return;
		}

		Bounds b = CalculateSelectionBounds(selected);
		GameObject parent = new GameObject("Chunk_A");
		Undo.RegisterCreatedObjectUndo(parent, "Create Chunk_A");
		parent.transform.position = new Vector3(b.center.x, b.center.y, 0f);

		foreach (var go in selected)
		{
			Undo.SetTransformParent(go.transform, parent.transform, "Parent To Chunk");
		}

		// Add LevelChunk and entry/exit markers
		var chunk = parent.AddComponent<LevelChunk>();
		GameObject entry = new GameObject("EntryPoint");
		entry.transform.SetParent(parent.transform, true);
		entry.transform.position = new Vector3(b.min.x, b.center.y, 0f);
		GameObject exit = new GameObject("ExitPoint");
		exit.transform.SetParent(parent.transform, true);
		exit.transform.position = new Vector3(b.max.x, b.center.y, 0f);

		// Gán reference qua SerializedObject để tránh cần field public
		SerializedObject so = new SerializedObject(chunk);
		so.FindProperty("entryPoint").objectReferenceValue = entry.transform;
		so.FindProperty("exitPoint").objectReferenceValue = exit.transform;
		so.ApplyModifiedPropertiesWithoutUndo();

		// Đảm bảo thư mục tồn tại
		const string prefabDir = "Assets/prefabs";
		if (!AssetDatabase.IsValidFolder(prefabDir))
		{
			AssetDatabase.CreateFolder("Assets", "prefabs");
		}

		string path = AssetDatabase.GenerateUniqueAssetPath(prefabDir + "/Chunk_A.prefab");
		PrefabUtility.SaveAsPrefabAssetAndConnect(parent, path, InteractionMode.UserAction);
		AssetDatabase.SaveAssets();
		EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));

		EditorUtility.DisplayDialog("Create Chunk", "Đã tạo prefab: " + path, "OK");
	}

	[MenuItem("Tools/Endless Runner/Setup Level Generator", priority = 1)]
	public static void SetupLevelGenerator()
	{
		// Tìm tất cả prefab chunk trong Assets/prefabs
		string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/prefabs" });
		List<LevelChunk> chunks = new List<LevelChunk>();
		foreach (var guid in guids)
		{
			string path = AssetDatabase.GUIDToAssetPath(guid);
			GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			if (prefab != null)
			{
				LevelChunk lc = prefab.GetComponent<LevelChunk>();
				if (lc != null) chunks.Add(lc);
			}
		}

		if (chunks.Count == 0)
		{
			EditorUtility.DisplayDialog("Setup Generator", "Không tìm thấy prefab chunk trong Assets/prefabs. Hãy tạo trước bằng menu Create Chunk.", "OK");
			return;
		}

		GameObject genGo = GameObject.Find("LevelGenerator");
		if (genGo == null)
		{
			genGo = new GameObject("LevelGenerator");
			Undo.RegisterCreatedObjectUndo(genGo, "Create LevelGenerator");
		}

		var gen = genGo.GetComponent<EndlessRunnerGenerator>();
		if (gen == null) gen = genGo.AddComponent<EndlessRunnerGenerator>();

		SerializedObject so = new SerializedObject(gen);
		SerializedProperty listProp = so.FindProperty("chunkPrefabs");
		listProp.ClearArray();
		for (int i = 0; i < chunks.Count; i++)
		{
			listProp.InsertArrayElementAtIndex(i);
			listProp.GetArrayElementAtIndex(i).objectReferenceValue = chunks[i];
		}
		so.FindProperty("initialChunks").intValue = 3;
		so.FindProperty("followCamera").boolValue = true;
		so.FindProperty("aheadDistance").floatValue = 30f;
		so.FindProperty("behindCullDistance").floatValue = 20f;
		so.FindProperty("poolPrewarmPerPrefab").intValue = 1;
		so.ApplyModifiedPropertiesWithoutUndo();

		EditorGUIUtility.PingObject(genGo);
		EditorUtility.DisplayDialog("Setup Generator", "Đã cấu hình xong LevelGenerator với các chunk tìm thấy.", "OK");
	}

	[MenuItem("Tools/Endless Runner/Convert Grid Children To Selected Chunk", priority = 2)]
	public static void ConvertGridChildrenToSelectedChunk()
	{
		GameObject selected = Selection.activeGameObject;
		if (selected == null || selected.GetComponent<LevelChunk>() == null)
		{
			EditorUtility.DisplayDialog("Convert Grid → Chunk", "Hãy chọn một GameObject có component LevelChunk (ví dụ Chunk_A).", "OK");
			return;
		}

		GameObject grid = GameObject.Find("Grid");
		if (grid == null)
		{
			EditorUtility.DisplayDialog("Convert Grid → Chunk", "Không tìm thấy GameObject tên 'Grid' trong scene.", "OK");
			return;
		}

		// Tạo parent chứa tilemap trong chunk để gọn gàng
		GameObject container = new GameObject("Tilemaps");
		Undo.RegisterCreatedObjectUndo(container, "Create Tilemaps Container");
		container.transform.SetParent(selected.transform, true);
		container.transform.localPosition = Vector3.zero;

		int moved = 0;
		List<Transform> children = new List<Transform>();
		for (int i = 0; i < grid.transform.childCount; i++)
		{
			children.Add(grid.transform.GetChild(i));
		}

		foreach (var child in children)
		{
			// Bỏ qua nếu là gizmo/empty đặc biệt
			if (child == selected.transform) continue;
			Undo.SetTransformParent(child, container.transform, "Move Grid Child To Chunk");
			moved++;
		}

		EditorGUIUtility.PingObject(selected);
		EditorUtility.DisplayDialog("Convert Grid → Chunk", $"Đã chuyển {moved} đối tượng từ Grid vào '{selected.name}/Tilemaps'.", "OK");
	}

	private static Bounds CalculateSelectionBounds(GameObject[] objects)
	{
		bool hasAny = false;
		Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
		foreach (var go in objects)
		{
			Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
			Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>(true);
			foreach (var r in renderers)
			{
				if (!hasAny) { bounds = r.bounds; hasAny = true; }
				else bounds.Encapsulate(r.bounds);
			}
			foreach (var c in colliders)
			{
				if (!hasAny) { bounds = c.bounds; hasAny = true; }
				else bounds.Encapsulate(c.bounds);
			}
		}
		if (!hasAny)
		{
			bounds = new Bounds(Vector3.zero, new Vector3(10f, 5f, 1f));
		}
		return bounds;
	}

	[MenuItem("Tools/Endless Runner/Duplicate Selected Chunk As Randomized", priority = 3)]
	public static void DuplicateSelectedChunkAsRandomized()
	{
		GameObject selected = Selection.activeGameObject;
		if (selected == null)
		{
			EditorUtility.DisplayDialog("Duplicate Chunk", "Hãy chọn một chunk trong Hierarchy trước.", "OK");
			return;
		}
		LevelChunk src = selected.GetComponent<LevelChunk>();
		if (src == null)
		{
			EditorUtility.DisplayDialog("Duplicate Chunk", "Đối tượng được chọn không có LevelChunk.", "OK");
			return;
		}

		// Nhân bản trong scene
		GameObject clone = Object.Instantiate(selected, selected.transform.parent);
		Undo.RegisterCreatedObjectUndo(clone, "Duplicate Chunk");
		clone.name = selected.name.Replace("Chunk_A", "Chunk_B");

		// Ngẫu nhiên hoá vị trí các platform/obstacle con (tránh Entry/Exit và Tilemaps)
		RandomizeChildren(clone.transform);

		// Lưu thành prefab mới
		const string prefabDir = "Assets/prefabs";
		if (!AssetDatabase.IsValidFolder(prefabDir))
		{
			AssetDatabase.CreateFolder("Assets", "prefabs");
		}
		string path = AssetDatabase.GenerateUniqueAssetPath(prefabDir + "/Chunk_B.prefab");
		PrefabUtility.SaveAsPrefabAssetAndConnect(clone, path, InteractionMode.UserAction);

		// Cập nhật LevelGenerator
		GameObject genGo = GameObject.Find("LevelGenerator");
		if (genGo != null)
		{
			var gen = genGo.GetComponent<EndlessRunnerGenerator>();
			if (gen != null)
			{
				LevelChunk newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LevelChunk>();
				SerializedObject so = new SerializedObject(gen);
				SerializedProperty listProp = so.FindProperty("chunkPrefabs");
				int newIndex = listProp.arraySize;
				listProp.InsertArrayElementAtIndex(newIndex);
				listProp.GetArrayElementAtIndex(newIndex).objectReferenceValue = newPrefab;
				so.ApplyModifiedPropertiesWithoutUndo();
			}
		}

		AssetDatabase.SaveAssets();
		EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(path));
		EditorUtility.DisplayDialog("Duplicate Chunk", "Đã tạo và thêm vào generator: " + path, "OK");
	}

	private static void RandomizeChildren(Transform root)
	{
		// Biên độ ngẫu nhiên có thể chỉnh: dịch X nhỏ, Y nhỏ
		const float dx = 1.0f;
		const float dy = 1.0f;

		foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
		{
			if (t == root) continue;
			string n = t.name;
			if (n.Contains("EntryPoint") || n.Contains("ExitPoint")) continue;
			if (t.GetComponent<LevelChunk>() != null) continue;
			if (t.GetComponent<Tilemap>() != null) continue; // không xáo trộn tilemap để tránh vỡ bố cục

			bool isRenderable = t.GetComponent<Renderer>() != null || t.GetComponent<Collider2D>() != null;
			if (!isRenderable) continue;

			Vector3 p = t.position;
			p.x += Random.Range(-dx, dx);
			p.y += Random.Range(-dy, dy);
			Undo.RecordObject(t, "Randomize Child Position");
			t.position = p;
		}
	}
}


