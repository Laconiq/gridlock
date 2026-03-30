#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Gridlock.Editor
{
    public static class FixPickupPrefab
    {
        [MenuItem("Gridlock/Fix Pickup Prefab")]
        public static void Fix()
        {
            var prefabPath = "Assets/Prefabs/Loot/ModulePickup.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError("[FixPickupPrefab] Prefab not found at " + prefabPath);
                return;
            }

            var contents = PrefabUtility.LoadPrefabContents(prefabPath);

            var front = contents.transform.Find("FrontFace");
            if (front != null) Object.DestroyImmediate(front.gameObject);

            var back = contents.transform.Find("BackFace");
            if (back != null) Object.DestroyImmediate(back.gameObject);

            var existing = contents.transform.Find("Model");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            var model = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            model.name = "Model";
            model.transform.SetParent(contents.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale = Vector3.one;

            var collider = model.GetComponent<SphereCollider>();
            if (collider != null) Object.DestroyImmediate(collider);

            var shader = Shader.Find("Custom/VectorGlow");
            if (shader != null)
            {
                var mat = new Material(shader);
                mat.SetColor("_Color", Color.white);
                mat.SetColor("_EmissionColor", Color.white);
                mat.SetFloat("_EmissionIntensity", 5f);
                model.GetComponent<MeshRenderer>().sharedMaterial = mat;
            }

            contents.transform.localScale = Vector3.one * 0.3f;

            PrefabUtility.SaveAsPrefabAsset(contents, prefabPath);
            PrefabUtility.UnloadPrefabContents(contents);

            Debug.Log("[FixPickupPrefab] ModulePickup prefab updated: billboard → sphere, scale 0.3");
        }
    }
}
#endif
