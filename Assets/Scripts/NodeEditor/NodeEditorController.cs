using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Network;
using AIWE.NodeEditor.UI;
using AIWE.Player;
using AIWE.Towers;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.NodeEditor
{
    public class NodeEditorController : MonoBehaviour
    {
        [SerializeField] private NodeEditorScreen screen;

        public static bool WaitingForLock { get; set; }

        private void Start()
        {
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
            {
                lockManager.OnLockGranted += OnLockGranted;
                lockManager.OnLockDenied += OnLockDenied;
            }
        }

        private void OnLockGranted()
        {
            if (!WaitingForLock) return;
            WaitingForLock = false;

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer == null) return;

            var interaction = localPlayer.GetComponent<PlayerInteraction>();
            var inventory = localPlayer.GetComponent<PlayerInventory>();
            if (interaction?.CurrentInteractable is TowerInteractable towerInteractable)
            {
                screen?.Open(towerInteractable.Chassis, inventory);
            }
        }

        private void OnLockDenied()
        {
            Debug.Log("[NodeEditor] Lock denied - editor in use by another player");
        }

        private void OnDestroy()
        {
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
            {
                lockManager.OnLockGranted -= OnLockGranted;
                lockManager.OnLockDenied -= OnLockDenied;
            }
        }
    }
}
