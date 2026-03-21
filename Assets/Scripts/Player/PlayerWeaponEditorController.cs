using AIWE.NodeEditor.UI;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerWeaponEditorController : NetworkBehaviour
    {
        private Controls _controls;
        private PlayerWeaponChassis _chassis;
        private PlayerWeaponExecutor _executor;
        private PlayerInventory _inventory;
        private bool _inputEnabled;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (_controls == null) return;
                if (value) _controls.Player.Enable();
                else _controls.Player.Disable();
            }
        }

        private void Awake()
        {
            _controls = new Controls();
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

            _controls.Player.OpenWeaponEditor.performed += _ => ToggleWeaponEditor();
            _controls.Player.Attack.performed += _ => OnAttack();
            _controls.Player.AltAttack.performed += _ => OnAltAttack();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _controls.Player.Disable();
        }

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
            var cam = Camera.main;
            return cam != null ? cam.transform.forward : transform.forward;
        }

        private Vector3 GetFireOrigin()
        {
            return _chassis?.FirePoint != null ? _chassis.FirePoint.position : transform.position;
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
