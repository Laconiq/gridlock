using System.Collections;
using Gridlock.Interfaces;
using Gridlock.Towers;
using Gridlock.Visual;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gridlock.Player
{
    public class PlayerInteraction : MonoBehaviour
    {
        [SerializeField] private LayerMask interactionMask = ~0;

        private Controls _controls;
        private IInteractable _currentInteractable;
        private InteractionHUD _interactionHUD;
        private bool _inputEnabled;
        private Camera _cachedCamera;
        private TowerRangeIndicator _activeRange;

        public IInteractable CurrentInteractable => _currentInteractable;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (!value)
                {
                    _currentInteractable = null;
                    if (_interactionHUD != null) _interactionHUD.Hide();
                    if (_activeRange != null) { _activeRange.Hide(); _activeRange = null; }
                }
            }
        }

        private void Start()
        {
            var provider = GetComponent<PlayerInputProvider>();
            _controls = provider.Controls;
            _controls.Player.Interact.performed += OnInteractPerformed;
            StartCoroutine(WaitForHUD());
        }

        private IEnumerator WaitForHUD()
        {
            while (_interactionHUD == null)
            {
                _interactionHUD = FindAnyObjectByType<InteractionHUD>();
                yield return null;
            }
        }

        private void OnDestroy()
        {
            if (_controls != null)
                _controls.Player.Interact.performed -= OnInteractPerformed;
        }

        private void OnInteractPerformed(InputAction.CallbackContext ctx)
        {
            if (!_inputEnabled) return;
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract()) return;

            _currentInteractable.Interact();
        }

        private void Update()
        {
            if (!_inputEnabled) return;
            CheckForInteractable();
        }

        private void CheckForInteractable()
        {
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            var cam = _cachedCamera;
            if (cam == null) return;

            IInteractable found = null;

            var mousePos = Mouse.current.position.ReadValue();
            var ray = cam.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out var hit, 1000f, interactionMask))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (found != _currentInteractable)
            {
                _currentInteractable = found;
                UpdateHUD();
                UpdateRangeIndicator();
            }
        }

        private void UpdateRangeIndicator()
        {
            if (_activeRange != null)
            {
                _activeRange.Hide();
                _activeRange = null;
            }

            if (_currentInteractable is TowerInteractable tower)
            {
                _activeRange = tower.GetComponent<TowerRangeIndicator>();
                _activeRange?.Show();
            }
        }

        private void UpdateHUD()
        {
            if (_interactionHUD == null) return;

            if (_currentInteractable != null)
            {
                var canInteract = _currentInteractable.CanInteract();
                _interactionHUD.Show(_currentInteractable.GetPromptText(), canInteract);
            }
            else
            {
                _interactionHUD.Hide();
            }
        }
    }
}
