
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AIWE.Editor
{
    public static class SceneSetup
    {
        [MenuItem("AIWE/Save Current Scene As GameScene")]
        public static void SaveCurrentScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
            Debug.Log("Scene saved as Assets/Scenes/GameScene.unity");
        }

        [MenuItem("AIWE/Create Scriptable Object Assets")]
        public static void CreateSOAssets()
        {
            // Sentinelle Chassis
            CreateChassisAsset("Sentinelle", "sentinelle", 3, 10f, 360f, false, "Assets/Data/Chassis/Sentinelle.asset");

            // Player Chassis
            CreateChassisAsset("Player", "player", 2, 15f, 360f, true, "Assets/Data/Chassis/Player.asset");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("ScriptableObject assets created.");
        }

        private static void CreateChassisAsset(string displayName, string chassisId, int maxTriggers, float baseRange, float arc, bool isPlayer, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<AIWE.Towers.ChassisDefinition>(path);
            if (existing != null)
            {
                Debug.Log($"Already exists: {path}");
                return;
            }

            var asset = ScriptableObject.CreateInstance<AIWE.Towers.ChassisDefinition>();
            asset.chassisId = chassisId;
            asset.displayName = displayName;
            asset.maxTriggers = maxTriggers;
            asset.baseRange = baseRange;
            asset.rotationArc = arc;
            asset.isPlayerChassis = isPlayer;
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"Created: {path}");
        }
    }
}
#endif
