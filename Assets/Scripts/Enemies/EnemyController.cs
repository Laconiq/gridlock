using System;
using AIWE.AI;
using AIWE.Combat;
using AIWE.Core;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace AIWE.Enemies
{
    public class EnemyController : NetworkBehaviour, ITargetable
    {
        [SerializeField] private float moveSpeed = 3f;

        private readonly NetworkVariable<byte> _aiState = new();
        private readonly NetworkVariable<float> _normalizedSpeed = new();
        private EnemyHealth _health;
        private StatusEffectManager _statusEffects;
        private NavMeshAgent _agent;
        private float _objectiveDamage;

        public event Action OnReachedObjective;

        public Vector3 Position => transform.position;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Transform Transform => transform;
        public float MoveSpeed => moveSpeed;
        public EnemyAIState AIState => (EnemyAIState)_aiState.Value;
        public float NormalizedSpeed => _normalizedSpeed.Value;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _statusEffects = GetComponent<StatusEffectManager>();
            _agent = GetComponent<NavMeshAgent>();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsServer && _agent != null)
                _agent.enabled = false;
        }

        public void Setup(EnemyDefinition definition)
        {
            if (!IsServer) return;

            moveSpeed = definition.moveSpeed;
            _objectiveDamage = definition.objectiveDamage;
            transform.localScale = Vector3.one * definition.scale;

            if (_agent != null)
            {
                _agent.speed = moveSpeed;
                _agent.updateRotation = true;
            }
        }

        private void Update()
        {
            if (!IsServer || !IsAlive || _agent == null || !_agent.enabled) return;

            float speed = moveSpeed;
            if (_statusEffects != null)
                speed *= _statusEffects.SpeedMultiplier;

            _agent.speed = speed;
            float newSpeed = moveSpeed > 0f
                ? Mathf.Clamp01(_agent.velocity.magnitude / moveSpeed)
                : 0f;
            if (Mathf.Abs(newSpeed - _normalizedSpeed.Value) > 0.01f)
                _normalizedSpeed.Value = newSpeed;
        }

        public void SetDestination(Vector3 destination)
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.SetDestination(destination);
        }

        public void StopMovement()
        {
            if (_agent != null && _agent.enabled && _agent.isOnNavMesh)
                _agent.ResetPath();
        }

        public bool HasReachedDestination(float threshold = 0.5f)
        {
            if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return false;
            return !_agent.pathPending && _agent.remainingDistance <= threshold;
        }

        public void SetAIState(EnemyAIState state)
        {
            if (IsServer)
                _aiState.Value = (byte)state;
        }

        public void NotifyReachedObjective()
        {
            var objective = ServiceLocator.Get<ObjectiveController>();
            if (objective != null)
                objective.TakeDamage(new DamageInfo(_objectiveDamage, NetworkObjectId, DamageType.Direct));

            OnReachedObjective?.Invoke();
            NetworkObject.Despawn();
        }
    }
}
