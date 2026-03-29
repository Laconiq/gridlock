using Gridlock.Interfaces;
using Gridlock.Mods;
using Gridlock.Mods.UI;
using UnityEngine;

namespace Gridlock.Towers
{
    [RequireComponent(typeof(TowerChassis))]
    public class TowerInteractable : MonoBehaviour, IInteractable
    {
        private TowerChassis _chassis;
        private ModSlotExecutor _executor;

        public TowerChassis Chassis => _chassis;

        private void Awake()
        {
            _chassis = GetComponent<TowerChassis>();
            _executor = GetComponent<ModSlotExecutor>();
        }

        public string GetPromptText()
        {
            return "Click - Edit Tower";
        }

        public bool CanInteract()
        {
            return ModSlotPanel.Instance == null || !ModSlotPanel.Instance.IsOpen;
        }

        public void Interact()
        {
            if (_executor == null) return;
            ModSlotPanel.Instance?.Open(_executor, transform);
        }
    }
}
