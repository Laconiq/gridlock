using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerAnimationController : NetworkBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private RuntimeAnimatorController _animatorController;
        [SerializeField] private float animSpeedDamp = 0.1f;

        private PlayerController _controller;
        private PlayerHealth _health;
        private bool _isJumping;
        private bool _wasGrounded = true;
        private bool _ready;
        private float _lastHP;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
        private static readonly int IsJumpingHash = Animator.StringToHash("IsJumping");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _health = GetComponent<PlayerHealth>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            if (_animator != null && _animator.runtimeAnimatorController == null && _animatorController != null)
                _animator.runtimeAnimatorController = _animatorController;

            _ready = _animator != null && _animator.runtimeAnimatorController != null;

            if (_health != null)
                _lastHP = _health.CurrentHP;

            if (IsOwner)
            {
                _controller.OnJumped += OnJumped;
                _controller.OnLanded += OnLanded;

                HideLocalPlayerHead();
            }

            if (_health != null)
            {
                _health.OnHPChanged += OnHPChanged;
                _health.OnDeath += OnDeath;
                _health.OnRespawn += OnRespawn;
            }
        }

        public override void OnNetworkDespawn()
        {
            if (IsOwner && _controller != null)
            {
                _controller.OnJumped -= OnJumped;
                _controller.OnLanded -= OnLanded;
            }

            if (_health != null)
            {
                _health.OnHPChanged -= OnHPChanged;
                _health.OnDeath -= OnDeath;
                _health.OnRespawn -= OnRespawn;
            }
        }

        private void Update()
        {
            if (!IsOwner || !_ready || _controller == null) return;

            _animator.SetFloat(SpeedHash, _controller.CurrentSpeedNormalized, animSpeedDamp, Time.deltaTime);
            _animator.SetBool(IsGroundedHash, _controller.IsGrounded);

            if (_isJumping && _controller.IsGrounded && !_wasGrounded)
                _isJumping = false;

            _animator.SetBool(IsJumpingHash, _isJumping);
            _wasGrounded = _controller.IsGrounded;
        }

        private void OnJumped()
        {
            _isJumping = true;
        }

        private void OnLanded(float fallSpeed)
        {
            _isJumping = false;
        }

        private void OnHPChanged(float current, float max)
        {
            if (_animator == null) return;
            if (current < _lastHP)
                _animator.SetTrigger(HitHash);
            _lastHP = current;
        }

        private void OnDeath()
        {
            if (_animator != null)
                _animator.SetBool(DeadHash, true);
        }

        private void OnRespawn()
        {
            if (_animator != null)
            {
                _animator.SetBool(DeadHash, false);
                _isJumping = false;
            }
        }

        private void HideLocalPlayerHead()
        {
            if (_animator == null) return;

            var head = _animator.GetBoneTransform(HumanBodyBones.Head);
            if (head != null)
                head.localScale = Vector3.zero;
        }
    }
}
