using Gridlock.Interfaces;
using Gridlock.NodeEditor.UI;
using Gridlock.Player;
using UnityEngine;

namespace Gridlock.Towers
{
    [RequireComponent(typeof(TowerChassis))]
    public class TowerInteractable : MonoBehaviour, IInteractable
    {
        private TowerChassis _chassis;
        private PlayerInventory _cachedInventory;

        public TowerChassis Chassis => _chassis;

        private void Awake()
        {
            _chassis = GetComponent<TowerChassis>();
        }

        public string GetPromptText()
        {
            return "Click - Edit Tower";
        }

        public bool CanInteract()
        {
            var editor = NodeEditorScreen.Instance;
            return editor != null && !editor.IsOpen;
        }

        public void Interact()
        {
            var editor = NodeEditorScreen.Instance;
            if (editor == null || editor.IsOpen) return;

            _cachedInventory ??= FindAnyObjectByType<PlayerInventory>();
            editor.Open(_chassis, _cachedInventory, transform);
        }
    }
}
