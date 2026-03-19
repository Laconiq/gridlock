using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Network;
using AIWE.NodeEditor.UI;
using AIWE.Towers;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.NodeEditor
{
    public class NodeEditorController : MonoBehaviour
    {
        [SerializeField] private NodeEditorScreen screen;

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
            // Find the tower we're looking at and open the editor
            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer == null) return;

            var interaction = localPlayer.GetComponent<Player.PlayerInteraction>();
            if (interaction?.CurrentInteractable is TowerInteractable towerInteractable)
            {
                screen?.Open(towerInteractable.Chassis);
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
