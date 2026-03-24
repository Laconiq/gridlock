using AIWE.NodeEditor.UI;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    public class PlayerWeaponEditorController : NetworkBehaviour
    {
        private Controls _controls;
        private PlayerWeaponChassis _chassis;
        private PlayerWeaponExecutor _executor;
        private PlayerInventory _inventory;
        private bool _inputEnabled;
        private Camera _cachedCamera;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set => _inputEnabled = value;
        }

        private void Awake()
        {
            _chassis = GetComponent<PlayerWeaponChassis>();
            _executor = GetComponent<PlayerWeaponExecutor>();
            _inventory = GetComponent<PlayerInventory>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            var provider = GetComponent<PlayerInputProvider>();
            _controls = provider.Controls;
            _controls.Player.OpenWeaponEditor.performed += OnToggleWeaponEditorPerformed;
            _controls.Player.Attack.performed += OnAttackPerformed;
            _controls.Player.AltAttack.performed += OnAltAttackPerformed;
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            if (_controls != null)
            {
                _controls.Player.OpenWeaponEditor.performed -= OnToggleWeaponEditorPerformed;
                _controls.Player.Attack.performed -= OnAttackPerformed;
                _controls.Player.AltAttack.performed -= OnAltAttackPerformed;
            }
        }

        private void OnToggleWeaponEditorPerformed(InputAction.CallbackContext ctx) => ToggleWeaponEditor();
        private void OnAttackPerformed(InputAction.CallbackContext ctx) => OnAttack();
        private void OnAltAttackPerformed(InputAction.CallbackContext ctx) => OnAltAttack();

        private void ToggleWeaponEditor()
        {
            if (!_inputEnabled) return;

            var screen = NodeEditorScreen.Instance;
            if (screen == null) return;

            if (screen.IsOpen && screen.IsWeaponMode)
            {
                screen.OnSaveButtonClicked();
            }
            else if (!screen.IsOpen)
            {
                screen.Open(_chassis, _inventory);
            }
        }

        private void OnAttack()
        {
            if (!_inputEnabled) return;
            var screen = NodeEditorScreen.Instance;
            if (screen != null && screen.IsOpen) return;

            var aim = GetAimDirection();
            var origin = GetFireOrigin();
            _executor?.SpawnLocalProjectile(origin, aim, false);
            _executor?.FireLeftClickRpc(origin, aim);
        }

        private void OnAltAttack()
        {
            if (!_inputEnabled) return;
            var screen = NodeEditorScreen.Instance;
            if (screen != null && screen.IsOpen) return;

            var aim = GetAimDirection();
            var origin = GetFireOrigin();
            _executor?.SpawnLocalProjectile(origin, aim, true);
            _executor?.FireRightClickRpc(origin, aim);
        }

        private Vector3 GetAimDirection()
        {
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            return _cachedCamera != null ? _cachedCamera.transform.forward : transform.forward;
        }

        private Vector3 GetFireOrigin()
        {
            return _chassis?.FirePoint != null ? _chassis.FirePoint.position : transform.position;
        }

    }
}
