using System;
using AIWE.Combat;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyController : NetworkBehaviour, ITargetable
    {
        [SerializeField] private float moveSpeed = 3f;

        private readonly NetworkVariable<Vector3> _targetPosition = new();
        private EnemyHealth _health;
        private StatusEffectManager _statusEffects;

        public event Action OnReachedObjective;

        public Vector3 Position => transform.position;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Transform Transform => transform;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _statusEffects = GetComponent<StatusEffectManager>();
        }

        public void Setup(EnemyDefinition definition, Vector3 target)
        {
            if (!IsServer) return;

            moveSpeed = definition.moveSpeed;
            _targetPosition.Value = target;
            transform.localScale = Vector3.one * definition.scale;
        }

        private void Update()
        {
            if (!IsServer) return;
            if (!IsAlive) return;

            var speed = moveSpeed;
            if (_statusEffects != null)
                speed *= _statusEffects.SpeedMultiplier;

            var direction = (_targetPosition.Value - transform.position).normalized;
            transform.position += direction * (speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, _targetPosition.Value) < 0.5f)
            {
                OnReachedObjective?.Invoke();
                NetworkObject.Despawn();
            }
        }
    }
}
