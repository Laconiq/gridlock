using System.Collections;
using AIWE.Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float gravity = -15f;
        [SerializeField] private float jumpHeight = 1.5f;

        private CharacterController _controller;
        private Controls _controls;
        private Vector3 _velocity;
        private bool _isSprinting;

        public bool InputEnabled { get; set; } = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _controls = new Controls();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            _controls.Player.Jump.performed += OnJump;
            _controls.Player.Sprint.performed += _ => _isSprinting = true;
            _controls.Player.Sprint.canceled += _ => _isSprinting = false;

            // Don't lock cursor or enable input yet — wait for game to start
            SetPlayerInputActive(false);

            StartCoroutine(WaitForGameManager());
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            _controls.Player.Jump.performed -= OnJump;
            _controls.Player.Disable();

            if (GameManager.Instance != null)
                GameManager.Instance.CurrentState.OnValueChanged -= OnGameStateChanged;
        }

        private IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
                yield return null;

            GameManager.Instance.CurrentState.OnValueChanged += OnGameStateChanged;
            CheckGameState(GameManager.Instance.CurrentState.Value);
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            CheckGameState(current);
        }

        private void CheckGameState(GameState state)
        {
            bool gameActive = state == GameState.Preparing || state == GameState.Wave || state == GameState.Intermission;
            SetPlayerInputActive(gameActive);
        }

        public void SetPlayerInputActive(bool active)
        {
            InputEnabled = active;
            if (active)
            {
                _controls.Player.Enable();
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                _controls.Player.Disable();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            var cam = GetComponentInChildren<PlayerCamera>();
            if (cam != null) cam.InputEnabled = active;

            var interaction = GetComponent<PlayerInteraction>();
            if (interaction != null) interaction.InputEnabled = active;

            var weaponEditor = GetComponent<PlayerWeaponEditorController>();
            if (weaponEditor != null) weaponEditor.InputEnabled = active;
        }

        private void Update()
        {
            if (!IsOwner || !InputEnabled) return;

            HandleMovement();
            HandleGravity();
        }

        private void HandleMovement()
        {
            var moveInput = _controls.Player.Move.ReadValue<Vector2>();
            var speed = _isSprinting ? sprintSpeed : walkSpeed;

            var move = transform.right * moveInput.x + transform.forward * moveInput.y;
            _controller.Move(move * (speed * Time.deltaTime));
        }

        private void HandleGravity()
        {
            if (_controller.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            _velocity.y += gravity * Time.deltaTime;
            _controller.Move(_velocity * Time.deltaTime);
        }

        private void OnJump(InputAction.CallbackContext ctx)
        {
            if (!InputEnabled) return;
            if (_controller.isGrounded)
            {
                _velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
