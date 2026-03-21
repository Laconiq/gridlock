using AIWE.Core;
using AIWE.Network;
using AIWE.NodeEditor.UI;
using AIWE.Player;
using AIWE.Towers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.RadialMenu
{
    public class RadialMenuController : NetworkBehaviour
    {
        private Controls _controls;
        private bool _waitingForLock;
        private TowerInteractable _pendingTower;

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
            _controls.Player.OpenRadialMenu.performed += OnRadialMenuInput;

            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
            {
                lockManager.OnLockGranted += OnLockGranted;
                lockManager.OnLockDenied += OnLockDenied;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;

            _controls.Player.OpenRadialMenu.performed -= OnRadialMenuInput;

            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
            {
                lockManager.OnLockGranted -= OnLockGranted;
                lockManager.OnLockDenied -= OnLockDenied;
            }
        }

        private void OnRadialMenuInput(InputAction.CallbackContext ctx)
        {
            var screen = RadialMenuScreen.Instance;
            if (screen == null) return;

            if (screen.IsOpen)
            {
                CloseRadialMenu();
                return;
            }

            var nodeEditor = NodeEditorScreen.Instance;
            if (nodeEditor != null && nodeEditor.IsOpen) return;

            var interaction = GetComponent<PlayerInteraction>();
            if (interaction?.CurrentInteractable == null) return;

            var tower = interaction.CurrentInteractable as TowerInteractable;
            if (tower == null) return;

            _pendingTower = tower;
            _waitingForLock = true;
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            lockManager?.RequestLockRpc(OwnerClientId);
        }

        private void OnLockGranted()
        {
            if (!_waitingForLock || _pendingTower == null) return;
            _waitingForLock = false;

            var screen = RadialMenuScreen.Instance;
            var inventory = GetComponent<PlayerInventory>();
            var playerController = GetComponent<PlayerController>();
            var camera = GetComponentInChildren<PlayerCamera>();
            var interaction = GetComponent<PlayerInteraction>();
            var weaponEditor = GetComponent<PlayerWeaponEditorController>();

            if (playerController != null) playerController.InputEnabled = false;
            if (camera != null) camera.InputEnabled = false;
            if (interaction != null) interaction.InputEnabled = false;
            if (weaponEditor != null) weaponEditor.InputEnabled = false;

            screen?.Open(_pendingTower.Chassis, inventory);
        }

        private void OnLockDenied()
        {
            _waitingForLock = false;
            _pendingTower = null;
        }

        private void CloseRadialMenu()
        {
            var screen = RadialMenuScreen.Instance;
            screen?.Close();

            var lockManager = ServiceLocator.Get<EditorLockManager>();
            lockManager?.ReleaseLockRpc(OwnerClientId);

            var playerController = GetComponent<PlayerController>();
            var camera = GetComponentInChildren<PlayerCamera>();
            var interaction = GetComponent<PlayerInteraction>();
            var weaponEditor = GetComponent<PlayerWeaponEditorController>();

            if (playerController != null) playerController.InputEnabled = true;
            if (camera != null) camera.InputEnabled = true;
            if (interaction != null) interaction.InputEnabled = true;
            if (weaponEditor != null) weaponEditor.InputEnabled = true;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            _pendingTower = null;
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
