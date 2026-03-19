using System.Collections;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [SerializeField] private float interactionRange = 4f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private Controls _controls;
        private IInteractable _currentInteractable;
        private InteractionHUD _interactionHUD;
        private bool _inputEnabled;

        public IInteractable CurrentInteractable => _currentInteractable;

        public bool InputEnabled
        {
            get => _inputEnabled;
            set
            {
                _inputEnabled = value;
                if (_controls == null) return;
                if (value)
                    _controls.Player.Enable();
                else
                {
                    _controls.Player.Disable();
                    _currentInteractable = null;
                    if (_interactionHUD != null) _interactionHUD.Hide();
                }
            }
        }

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

            _controls.Player.Interact.performed += _ => TryInteract();
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
            _controls.Player.Disable();
        }

        private void Update()
        {
            if (!IsOwner || !_inputEnabled) return;
            CheckForInteractable();
        }

        private void CheckForInteractable()
        {
            var cam = Camera.main;
            if (cam == null) return;

            IInteractable found = null;

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, interactionRange, interactionMask))
            {
                found = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (found != _currentInteractable)
            {
                _currentInteractable = found;
                UpdateHUD();
            }
            else if (_currentInteractable != null)
            {
                UpdateHUD();
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

        private void TryInteract()
        {
            if (!_inputEnabled) return;
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract(OwnerClientId)) return;

            Debug.Log($"[Interaction] Interacting with {_currentInteractable.GetPromptText()}");
            _currentInteractable.Interact(OwnerClientId);
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
