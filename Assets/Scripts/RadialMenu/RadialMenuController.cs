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
        private PlayerController _cachedPlayerController;
        private PlayerCamera _cachedCamera;
        private PlayerInteraction _cachedInteraction;
        private PlayerWeaponEditorController _cachedWeaponEditor;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            var provider = GetComponent<PlayerInputProvider>();
            _controls = provider.Controls;
            _controls.Player.OpenRadialMenu.performed += OnRadialMenuInput;

            _cachedPlayerController = GetComponent<PlayerController>();
            _cachedCamera = GetComponentInChildren<PlayerCamera>();
            _cachedInteraction = GetComponent<PlayerInteraction>();
            _cachedWeaponEditor = GetComponent<PlayerWeaponEditorController>();

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

            if (_controls != null)
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
            lockManager?.RequestLockRpc();
        }

        private void OnLockGranted()
        {
            if (!_waitingForLock || _pendingTower == null) return;
            _waitingForLock = false;

            var screen = RadialMenuScreen.Instance;
            var inventory = GetComponent<PlayerInventory>();

            if (_cachedPlayerController != null) _cachedPlayerController.InputEnabled = false;
            if (_cachedCamera != null) _cachedCamera.InputEnabled = false;
            if (_cachedInteraction != null) _cachedInteraction.InputEnabled = false;
            if (_cachedWeaponEditor != null) _cachedWeaponEditor.InputEnabled = false;

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
            lockManager?.ReleaseLockRpc();

            if (_cachedPlayerController != null) _cachedPlayerController.InputEnabled = true;
            if (_cachedCamera != null) _cachedCamera.InputEnabled = true;
            if (_cachedInteraction != null) _cachedInteraction.InputEnabled = true;
            if (_cachedWeaponEditor != null) _cachedWeaponEditor.InputEnabled = true;

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            _pendingTower = null;
        }
    }
}
