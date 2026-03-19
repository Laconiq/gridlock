using System.Collections;
using AIWE.Core;
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

        public IInteractable CurrentInteractable => _currentInteractable;
        public bool InputEnabled { get; set; }

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
            // HUD is created at runtime, wait for it
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
            if (!IsOwner || !InputEnabled) return;
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
            if (!InputEnabled) return;
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract(OwnerClientId)) return;

            _currentInteractable.Interact(OwnerClientId);
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
