using AIWE.AI;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyAnimationController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float animSpeedDamp = 0.1f;
        [SerializeField] private float hitAnimCooldown = 0.3f;

        private EnemyController _controller;
        private EnemyHealth _health;
        private float _lastHitTime;

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int AIStateHash = Animator.StringToHash("AIState");
        private static readonly int HitHash = Animator.StringToHash("Hit");
        private static readonly int DeadHash = Animator.StringToHash("Dead");

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
            _health = GetComponent<EnemyHealth>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health._currentHPChanged += OnDamaged;
                _health.OnDeath += OnDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health._currentHPChanged -= OnDamaged;
                _health.OnDeath -= OnDeath;
            }
        }

        private void Update()
        {
            if (_animator == null || _controller == null) return;

            _animator.SetFloat(SpeedHash, _controller.NormalizedSpeed, animSpeedDamp, Time.deltaTime);
            _animator.SetInteger(AIStateHash, (int)_controller.AIState);
        }

        private void OnDamaged(float damage)
        {
            if (_animator == null) return;
            if (Time.time - _lastHitTime < hitAnimCooldown) return;

            _lastHitTime = Time.time;
            _animator.SetTrigger(HitHash);
        }

        private void OnDeath()
        {
            if (_animator == null) return;
            _animator.SetBool(DeadHash, true);
        }
    }
}
