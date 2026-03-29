#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CleanMissingScripts
{
    [MenuItem("Gridlock/Clean Missing Scripts from Prefabs")]
    public static void Run()
    {
        string[] prefabPaths =
        {
            "Assets/Prefabs/Combat/BasicProjectile.prefab",
            "Assets/Prefabs/Towers/TowerPrefab.prefab",
        };

        int totalRemoved = 0;

        foreach (var path in prefabPaths)
        {
            var contents = PrefabUtility.LoadPrefabContents(path);
            int removed = RemoveMissingScriptsRecursive(contents);
            if (removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                totalRemoved += removed;
                Debug.Log($"[CleanMissingScripts] Removed {removed} missing script(s) from {path}");
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        // Also clean the Player prefab
        var playerGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Player" });
        foreach (var guid in playerGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var contents = PrefabUtility.LoadPrefabContents(path);
            int removed = RemoveMissingScriptsRecursive(contents);
            if (removed > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(contents, path);
                totalRemoved += removed;
                Debug.Log($"[CleanMissingScripts] Removed {removed} missing script(s) from {path}");
            }
            PrefabUtility.UnloadPrefabContents(contents);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[CleanMissingScripts] Done. Total removed: {totalRemoved}");
    }

    static int RemoveMissingScriptsRecursive(GameObject go)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        foreach (Transform child in go.transform)
            count += RemoveMissingScriptsRecursive(child.gameObject);
        return count;
    }
}
#endif
