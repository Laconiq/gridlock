#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FixTowerPrefab
{
    [MenuItem("AIWE/Fix Tower Prefab References")]
    public static void Run()
    {
        string path = "Assets/Prefabs/Towers/TowerPrefab.prefab";
        var contents = PrefabUtility.LoadPrefabContents(path);

        var chassis = contents.GetComponent<AIWE.Towers.TowerChassis>();
        if (chassis != null)
        {
            var so = new SerializedObject(chassis);

            var defProp = so.FindProperty("definition");
            var defAsset = AssetDatabase.LoadAssetAtPath<AIWE.Towers.ChassisDefinition>("Assets/Data/Chassis/Sentinelle.asset");
            if (defAsset != null)
                defProp.objectReferenceValue = defAsset;

            var fpProp = so.FindProperty("firePoint");
            var fp = contents.transform.Find("FirePoint");
            if (fp != null)
                fpProp.objectReferenceValue = fp;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var executor = contents.GetComponent<AIWE.Towers.TowerExecutor>();
        if (executor != null)
        {
            var so = new SerializedObject(executor);
            var regProp = so.FindProperty("moduleRegistry");
            var registry = AssetDatabase.LoadAssetAtPath<AIWE.Modules.ModuleRegistry>("Assets/Data/ModuleRegistry.asset");
            if (registry != null)
                regProp.objectReferenceValue = registry;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        PrefabUtility.SaveAsPrefabAsset(contents, path);
        PrefabUtility.UnloadPrefabContents(contents);
        Debug.Log("[FixTowerPrefab] Assigned ChassisDefinition, FirePoint, ModuleRegistry");
    }
}
#endif
