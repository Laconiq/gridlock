using System;
using System.Collections;
using AIWE.Core;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float acceleration = 80f;
        [SerializeField] private float deceleration = 160f;
        [SerializeField] private float airAccelMultiplier = 0.1f;

        [Header("Jump")]
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        private CharacterController _controller;
        private Controls _controls;
        private Vector3 _currentHorizontalVelocity;
        private float _verticalVelocity;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _wasGrounded;
        private float _fallStartVelocity;

        public bool InputEnabled { get; set; } = true;
        public float CurrentSpeedNormalized => moveSpeed > 0f ? _currentHorizontalVelocity.magnitude / moveSpeed : 0f;
        public Vector2 MoveInput { get; private set; }
        public bool IsGrounded => _controller.isGrounded;

        public event Action<float> OnLanded;

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

            var cc = GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                cc.enabled = true;
            }

            _controls.Player.Jump.performed += _ => OnJumpInput();

            SetPlayerInputActive(false);
            StartCoroutine(WaitForGameManager());
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
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

            UpdateTimers();
            HandleMovement();
            HandleGravity();
            HandleJumpBuffer();
            DetectLanding();
        }

        private void UpdateTimers()
        {
            if (_controller.isGrounded)
                _coyoteTimer = coyoteTime;
            else
                _coyoteTimer -= Time.deltaTime;

            _jumpBufferTimer -= Time.deltaTime;
        }

        private void HandleMovement()
        {
            MoveInput = _controls.Player.Move.ReadValue<Vector2>();
            var wishDir = transform.right * MoveInput.x + transform.forward * MoveInput.y;

            if (wishDir.sqrMagnitude > 1f)
                wishDir.Normalize();

            var targetVelocity = wishDir * moveSpeed;
            bool isAccelerating = wishDir.sqrMagnitude > 0.01f;

            float accelRate;
            if (_controller.isGrounded)
                accelRate = isAccelerating ? acceleration : deceleration;
            else
                accelRate = (isAccelerating ? acceleration : deceleration) * airAccelMultiplier;

            _currentHorizontalVelocity = Vector3.MoveTowards(
                _currentHorizontalVelocity,
                targetVelocity,
                accelRate * Time.deltaTime
            );

            var finalMove = _currentHorizontalVelocity + Vector3.up * _verticalVelocity;
            _controller.Move(finalMove * Time.deltaTime);
        }

        private void HandleGravity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                if (!_wasGrounded && _verticalVelocity < _fallStartVelocity)
                    _fallStartVelocity = _verticalVelocity;

                _verticalVelocity += gravity * Time.deltaTime;
            }
        }

        private void HandleJumpBuffer()
        {
            if (_jumpBufferTimer > 0 && _coyoteTimer > 0)
            {
                ExecuteJump();
            }
        }

        private void DetectLanding()
        {
            if (_controller.isGrounded && !_wasGrounded)
            {
                float fallSpeed = Mathf.Abs(_fallStartVelocity);
                if (fallSpeed > 3f)
                    OnLanded?.Invoke(fallSpeed);

                _fallStartVelocity = 0f;
            }
            _wasGrounded = _controller.isGrounded;
        }

        private void OnJumpInput()
        {
            if (!InputEnabled) return;
            _jumpBufferTimer = jumpBufferTime;

            if (_coyoteTimer > 0)
                ExecuteJump();
        }

        private void ExecuteJump()
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
        }

        public override void OnDestroy()
        {
            _controls?.Dispose();
            base.OnDestroy();
        }
    }
}
