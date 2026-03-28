#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public static class VectorStyleApplier
{
    [MenuItem("Tools/Apply Vector Style Materials")]
    public static void ApplyVectorStyle()
    {
        var env3d = GameObject.Find("testLevel/World/Foundry/Environment_3D");
        if (env3d == null)
        {
            Debug.LogError("Environment_3D not found");
            return;
        }

        var wallMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Vector/M_WallOutline.mat");
        var floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Vector/M_FloorDark.mat");
        var highMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Vector/M_VectorCyan.mat");

        int matCount = 0;
        int flatCount = 0;
        foreach (var t in env3d.GetComponentsInChildren<Transform>(true))
        {
            if (t == env3d.transform) continue;
            string objName = t.gameObject.name;

            var r = t.GetComponent<MeshRenderer>();
            if (r != null)
            {
                Material newMat = floorMat;
                if (objName.StartsWith("Wall")) newMat = wallMat;
                else if (objName.StartsWith("HighGround")) newMat = highMat;
                r.sharedMaterial = newMat;
                matCount++;
            }

            // Flatten to 2D: walls become thin tiles at y=0.05, floors at y=0
            var pos = t.localPosition;
            var scale = t.localScale;
            if (objName.StartsWith("Wall"))
            {
                pos.y = 0.05f;
                scale.y = 0.1f;
            }
            else if (objName.StartsWith("Floor"))
            {
                pos.y = 0f;
                scale.y = 0.01f;
            }
            else if (objName.StartsWith("HighGround"))
            {
                pos.y = 0.03f;
                scale.y = 0.06f;
            }
            else if (objName.StartsWith("Ramp"))
            {
                pos.y = 0.01f;
                scale.y = 0.02f;
            }
            else
            {
                pos.y = 0f;
                scale.y = 0.01f;
            }
            t.localPosition = pos;
            t.localScale = scale;
            flatCount++;
            EditorUtility.SetDirty(t.gameObject);
        }
        Debug.Log($"VectorStyleApplier: Materials={matCount}, Flattened={flatCount}");
    }
}
#endif