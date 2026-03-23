using AIWE.Core;
using AIWE.NodeEditor.UI;
using AIWE.RadialMenu;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    public class PlayerReadyController : NetworkBehaviour
    {
        private Controls _controls;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            var provider = GetComponent<PlayerInputProvider>();
            _controls = provider.Controls;
            _controls.Player.ReadyUp.performed += OnReadyUpPerformed;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            if (_controls != null)
                _controls.Player.ReadyUp.performed -= OnReadyUpPerformed;
        }

        private void OnReadyUpPerformed(InputAction.CallbackContext ctx)
        {
            if (GameManager.Instance?.CurrentState.Value != GameState.Preparing) return;
            if (NodeEditorScreen.Instance != null && NodeEditorScreen.Instance.IsOpen) return;
            if (RadialMenuScreen.Instance != null && RadialMenuScreen.Instance.IsOpen) return;

            ReadyManager.Instance?.ToggleReadyServerRpc();
        }

    }
}
