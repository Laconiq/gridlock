using AIWE.Interfaces;
using AIWE.Network;
using AIWE.Core;
using UnityEngine;

namespace AIWE.Towers
{
    [RequireComponent(typeof(TowerChassis))]
    public class TowerInteractable : MonoBehaviour, IInteractable
    {
        private TowerChassis _chassis;

        public TowerChassis Chassis => _chassis;

        private void Awake()
        {
            _chassis = GetComponent<TowerChassis>();
        }

        public string GetPromptText()
        {
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null && lockManager.IsLocked)
            {
                return "Tower editor in use";
            }
            return "E - Edit Tower | R - Quick Edit";
        }

        public bool CanInteract(ulong clientId)
        {
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager == null) return false;
            return !lockManager.IsLocked || lockManager.IsLockedBy(clientId);
        }

        public void Interact(ulong clientId)
        {
            if (!CanInteract(clientId)) return;

            var lockManager = ServiceLocator.Get<EditorLockManager>();
            lockManager?.RequestLockRpc();
        }
    }
}
