using System.Collections;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [SerializeField] private float interactionRange = 4f;
        [SerializeField] private float holdDuration = 0.4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private Controls _controls;
        private IInteractable _currentInteractable;
        private InteractionHUD _interactionHUD;
        private bool _inputEnabled;
        private Camera _cachedCamera;

        private bool _isHolding;
        private float _holdTimer;

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
                    _isHolding = false;
                    _holdTimer = 0f;
                    if (_interactionHUD != null) _interactionHUD.Hide();
                }
            }
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
            _controls.Player.Interact.started += OnInteractStarted;
            _controls.Player.Interact.canceled += OnInteractCanceled;
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

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            if (_controls != null)
            {
                _controls.Player.Interact.started -= OnInteractStarted;
                _controls.Player.Interact.canceled -= OnInteractCanceled;
            }
        }

        private void OnInteractStarted(InputAction.CallbackContext ctx)
        {
            if (!_inputEnabled) return;
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract(OwnerClientId)) return;

            _isHolding = true;
            _holdTimer = 0f;
        }

        private void OnInteractCanceled(InputAction.CallbackContext ctx)
        {
            _isHolding = false;
            _holdTimer = 0f;
            _interactionHUD?.SetProgress(0f);
        }

        private void Update()
        {
            if (!IsOwner || !_inputEnabled) return;

            CheckForInteractable();
            UpdateHold();
        }

        private void CheckForInteractable()
        {
            if (_cachedCamera == null) _cachedCamera = Camera.main;
            var cam = _cachedCamera;
            if (cam == null) return;

            IInteractable found = null;

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactionRange, interactionMask))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (found != _currentInteractable)
            {
                _currentInteractable = found;
                _isHolding = false;
                _holdTimer = 0f;
                UpdateHUD();
            }
            else if (_currentInteractable != null)
            {
                UpdateHUD();
            }
        }

        private void UpdateHold()
        {
            if (!_isHolding || _currentInteractable == null) return;

            if (!_currentInteractable.CanInteract(OwnerClientId))
            {
                _isHolding = false;
                _holdTimer = 0f;
                _interactionHUD?.SetProgress(0f);
                return;
            }

            _holdTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(_holdTimer / holdDuration);
            _interactionHUD?.SetProgress(progress);

            if (_holdTimer >= holdDuration)
            {
                AIWE.NodeEditor.NodeEditorController.WaitingForLock = true;
                _currentInteractable.Interact(OwnerClientId);
                _isHolding = false;
                _holdTimer = 0f;
                _interactionHUD?.SetProgress(0f);
            }
        }

        private void UpdateHUD()
        {
            if (_interactionHUD == null) return;

            if (_currentInteractable != null)
            {
                var canInteract = _currentInteractable.CanInteract(OwnerClientId);
                _interactionHUD.Show(_currentInteractable.GetPromptText(), canInteract);
            }
            else
            {
                _interactionHUD.Hide();
            }
        }

    }
}
