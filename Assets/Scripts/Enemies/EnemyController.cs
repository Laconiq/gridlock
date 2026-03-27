using System;
using AIWE.AI;
using AIWE.Combat;
using AIWE.Core;
using AIWE.Interfaces;
using UnityEngine;
using UnityEngine.AI;

namespace AIWE.Enemies
{
    public class EnemyController : MonoBehaviour, ITargetable
    {
        [SerializeField] private float moveSpeed = 3f;

        private byte _aiState;
        private float _normalizedSpeed;
        private EnemyHealth _health;
        private StatusEffectManager _statusEffects;
        private NavMeshAgent _agent;
        private float _objectiveDamage;

        public event Action OnReachedObjective;

        public Vector3 Position => transform.position;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Transform Transform => transform;
        public float MoveSpeed => moveSpeed;
        public EnemyAIState AIState => (EnemyAIState)_aiState;
        public float NormalizedSpeed => _normalizedSpeed;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _statusEffects = GetComponent<StatusEffectManager>();
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Setup(EnemyDefinition definition)
        {
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
            if (!IsAlive || _agent == null || !_agent.enabled) return;

            float speed = moveSpeed;
            if (_statusEffects != null)
                speed *= _statusEffects.SpeedMultiplier;

            _agent.speed = speed;
            _normalizedSpeed = moveSpeed > 0f
                ? Mathf.Clamp01(_agent.velocity.magnitude / moveSpeed)
                : 0f;
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
            _aiState = (byte)state;
        }

        public void NotifyReachedObjective()
        {
            var objective = ServiceLocator.Get<ObjectiveController>();
            if (objective != null)
                objective.TakeDamage(new DamageInfo(_objectiveDamage, (ulong)gameObject.GetInstanceID(), DamageType.Direct));

            OnReachedObjective?.Invoke();
            Destroy(gameObject);
        }
    }
}
