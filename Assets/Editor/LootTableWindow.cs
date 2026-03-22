using AIWE.Loot;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace AIWE.Editor
{
    public class LootTableWindow : OdinEditorWindow
    {
        private LootTable _target;

        [OnOpenAsset]
        private static bool OnOpenAsset(int instanceID, int line)
        {
            var asset = EditorUtility.EntityIdToObject(EntityId.FromULong((ulong)instanceID)) as LootTable;
            if (asset == null) return false;

            var window = GetWindow<LootTableWindow>("Loot Table");
            window._target = asset;
            window.minSize = new Vector2(800, 500);
            window.Show();
            return true;
        }

        protected override object GetTarget()
        {
            return _target;
        }

        protected override void OnImGUI()
        {
            if (_target == null)
            {
                EditorGUILayout.HelpBox("No LootTable selected. Double-click a LootTable asset to open it.", MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Button("Select Asset", EditorStyles.toolbarButton, GUILayout.Width(100)))
            {
                Selection.activeObject = _target;
                EditorGUIUtility.PingObject(_target);
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label(_target.name, EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            base.OnImGUI();
        }
    }
}
