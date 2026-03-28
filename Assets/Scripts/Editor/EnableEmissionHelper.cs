#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class EnableEmissionHelper
{
    [MenuItem("AIWE/Enable Emission on Lit Materials")]
    public static void Run()
    {
        var paths = new[] {
            "Assets/Materials/Vector/M_EnemyLit.mat",
            "Assets/Materials/Vector/M_TowerLit.mat"
        };
        foreach (var p in paths)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat == null) continue;
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[EnableEmission] Done");
    }
}
#endif
