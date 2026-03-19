
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using Unity.Netcode;

namespace AIWE.Editor
{
    public static class RegisterNetworkPrefabs
    {
        [MenuItem("AIWE/Register Network Prefabs")]
        public static void Register()
        {
            var listAsset = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            if (listAsset == null)
            {
                Debug.LogError("DefaultNetworkPrefabs.asset not found!");
                return;
            }

            var prefabPaths = new[]
            {
                "Assets/Prefabs/Network/PlayerPrefab.prefab",
                "Assets/Prefabs/Enemies/BasicEnemy.prefab",
                "Assets/Prefabs/Combat/BasicProjectile.prefab",
            };

            var so = new SerializedObject(listAsset);
            var listProp = so.FindProperty("List");

            foreach (var path in prefabPaths)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    Debug.LogWarning($"Prefab not found: {path}");
                    continue;
                }

                // Check if already registered
                bool found = false;
                for (int i = 0; i < listProp.arraySize; i++)
                {
                    var entry = listProp.GetArrayElementAtIndex(i);
                    var prefabField = entry.FindPropertyRelative("Prefab");
                    if (prefabField != null && prefabField.objectReferenceValue == prefab)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    int idx = listProp.arraySize;
                    listProp.InsertArrayElementAtIndex(idx);
                    var newEntry = listProp.GetArrayElementAtIndex(idx);
                    var prefabField = newEntry.FindPropertyRelative("Prefab");
                    if (prefabField != null)
                    {
                        prefabField.objectReferenceValue = prefab;
                    }
                    Debug.Log($"Registered network prefab: {path}");
                }
                else
                {
                    Debug.Log($"Already registered: {path}");
                }
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(listAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("Network prefabs registration complete.");
        }
    }
}
#endif
