using Gridlock.NodeEditor.UI;
using Gridlock.Player;
using Gridlock.Towers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gridlock.RadialMenu
{
    public class RadialMenuController : MonoBehaviour
    {
        private Controls _controls;
        private TowerInteractable _pendingTower;
        private PlayerController _cachedPlayerController;
        private PlayerInteraction _cachedInteraction;

        private void Start()
        {
            var provider = GetComponent<PlayerInputProvider>();
            _controls = provider.Controls;
            _controls.Player.OpenRadialMenu.performed += OnRadialMenuInput;

            _cachedPlayerController = GetComponent<PlayerController>();
            _cachedInteraction = GetComponent<PlayerInteraction>();
        }

        private void OnDestroy()
        {
            if (_controls != null)
                _controls.Player.OpenRadialMenu.performed -= OnRadialMenuInput;
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
            OpenRadialMenu();
        }

        private void OpenRadialMenu()
        {
            if (_pendingTower == null) return;

            var screen = RadialMenuScreen.Instance;
            var inventory = GetComponent<PlayerInventory>();

            if (_cachedPlayerController != null) _cachedPlayerController.InputEnabled = false;
            if (_cachedInteraction != null) _cachedInteraction.InputEnabled = false;

            screen?.Open(_pendingTower.Chassis, inventory);
        }

        private void CloseRadialMenu()
        {
            var screen = RadialMenuScreen.Instance;
            screen?.Close();

            if (_cachedPlayerController != null) _cachedPlayerController.InputEnabled = true;
            if (_cachedInteraction != null) _cachedInteraction.InputEnabled = true;

            _pendingTower = null;
        }
    }
}
