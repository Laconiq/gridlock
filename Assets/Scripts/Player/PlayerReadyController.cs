using AIWE.Core;
using AIWE.NodeEditor.UI;
using Unity.Netcode;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    public class PlayerReadyController : NetworkBehaviour
    {
        private Controls _controls;

        private void Awake()
        {
            _controls = new Controls();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _controls.Player.Enable();
            _controls.Player.ReadyUp.performed += OnReadyUpPerformed;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _controls.Player.ReadyUp.performed -= OnReadyUpPerformed;
            _controls.Player.Disable();
        }

        private void OnReadyUpPerformed(InputAction.CallbackContext ctx)
        {
            if (GameManager.Instance?.CurrentState.Value != GameState.Preparing) return;
            if (NodeEditorScreen.Instance != null && NodeEditorScreen.Instance.IsOpen) return;

            ReadyManager.Instance?.ToggleReadyServerRpc();
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
