using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerInteraction : NetworkBehaviour
    {
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private LayerMask interactionMask = ~0;

        private Controls _controls;
        private IInteractable _currentInteractable;
        private PlayerHUD _hud;

        public IInteractable CurrentInteractable => _currentInteractable;

        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            _controls = new Controls();
            _hud = GetComponentInChildren<PlayerHUD>();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _controls.Player.Enable();
            _controls.Player.Interact.performed += _ => TryInteract();
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

                if (_hud != null)
                {
                    if (_currentInteractable != null)
                    {
                        var canInteract = _currentInteractable.CanInteract(OwnerClientId);
                        _hud.ShowInteractionPrompt(_currentInteractable.GetPromptText(), canInteract);
                    }
                    else
                    {
                        _hud.HideInteractionPrompt();
                    }
                }
            }
            else if (_currentInteractable != null && _hud != null)
            {
                var canInteract = _currentInteractable.CanInteract(OwnerClientId);
                _hud.UpdateInteractionPrompt(_currentInteractable.GetPromptText(), canInteract);
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
