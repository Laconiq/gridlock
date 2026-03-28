#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class AssignLitMaterials
{
    [MenuItem("AIWE/Assign Default Material to Prefabs")]
    public static void Run()
    {
        // Use the same default material Unity assigns to primitives
        var tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var defaultMat = tempCube.GetComponent<MeshRenderer>().sharedMaterial;
        Object.DestroyImmediate(tempCube);

        AssignToPrefab("Assets/Prefabs/Enemies/BasicEnemy.prefab", defaultMat);
        AssignToPrefab("Assets/Prefabs/Towers/TowerPrefab.prefab", defaultMat);
        AssignToPrefab("Assets/Prefabs/Combat/BasicProjectile.prefab", defaultMat);

        AssetDatabase.SaveAssets();
        Debug.Log($"[AssignLitMaterials] Assigned '{defaultMat.name}' ({defaultMat.shader.name}) to all prefabs");
    }

    static void AssignToPrefab(string path, Material mat)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null || mat == null) return;

        var contents = PrefabUtility.LoadPrefabContents(path);
        foreach (var r in contents.GetComponentsInChildren<MeshRenderer>(true))
            r.sharedMaterial = mat;
        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
    }
}
#endif
