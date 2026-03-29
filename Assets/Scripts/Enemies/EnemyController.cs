using System;
using Gridlock.AI;
using Gridlock.Combat;
using Gridlock.Core;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Enemies
{
    public class EnemyController : MonoBehaviour, ITargetable
    {
        [SerializeField] private float moveSpeed = 3f;

        private byte _aiState;
        private float _normalizedSpeed;
        private EnemyHealth _health;
        private StatusEffectManager _statusEffects;
        private float _objectiveDamage;
        private float _floatY;

        private Vector3[] _route;
        private int _routeIndex;
        private bool _followingRoute;

        private Vector3 _overrideDestination;
        private bool _hasOverride;
        private bool _stopped;

        public event Action OnReachedObjective;

        public Vector3 Position => transform.position;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Transform Transform => transform;
        public float MoveSpeed => moveSpeed;
        public EnemyAIState AIState => (EnemyAIState)_aiState;
        public float NormalizedSpeed => _normalizedSpeed;
        public int RouteIndex => _routeIndex;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _statusEffects = GetComponent<StatusEffectManager>();
            _floatY = transform.position.y;

            var wf = GetComponent<Visual.WarpFollower>() ?? gameObject.AddComponent<Visual.WarpFollower>();
            wf.SetBaseY(_floatY);

            EnemyRegistry.Register(this, _health);
        }

        private void OnDestroy()
        {
            EnemyRegistry.Unregister(this);
        }

        public void Setup(EnemyDefinition definition)
        {
            moveSpeed = definition.moveSpeed;
            _objectiveDamage = definition.objectiveDamage;
        }

        public void AssignRoute(Vector3[] route, int startIndex)
        {
            _route = route;
            _routeIndex = Mathf.Clamp(startIndex, 0, route.Length - 1);
            _followingRoute = true;
            _hasOverride = false;
        }

        private void Update()
        {
            if (!IsAlive || _stopped) return;

            float speed = moveSpeed;
            if (_statusEffects != null)
                speed *= _statusEffects.SpeedMultiplier;

            if (_hasOverride)
            {
                MoveToward(_overrideDestination, speed);
            }
            else if (_followingRoute && _route != null)
            {
                FollowRouteStep(speed);
            }

            _normalizedSpeed = moveSpeed > 0f ? Mathf.Clamp01(speed / moveSpeed) : 0f;
        }

        private void FollowRouteStep(float speed)
        {
            if (_routeIndex >= _route.Length)
            {
                NotifyReachedObjective();
                return;
            }

            float remaining = speed * Time.deltaTime;

            while (remaining > 0f && _routeIndex < _route.Length)
            {
                var target = _route[_routeIndex];
                target.y = _floatY;

                var toTarget = target - transform.position;
                toTarget.y = 0f;
                float dist = toTarget.magnitude;

                if (dist <= remaining)
                {
                    transform.position = new Vector3(target.x, _floatY, target.z);
                    remaining -= dist;
                    _routeIndex++;
                }
                else
                {
                    var dir = toTarget / dist;
                    transform.position += dir * remaining;
                    remaining = 0f;
                }
            }

            if (_routeIndex >= _route.Length)
                NotifyReachedObjective();
        }

        private void MoveToward(Vector3 target, float speed)
        {
            var direction = target - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                var move = direction.normalized * (speed * Time.deltaTime);
                if (move.sqrMagnitude > direction.sqrMagnitude)
                    move = direction;

                transform.position += move;
            }
        }

        public void SetDestination(Vector3 destination)
        {
            _overrideDestination = destination;
            _hasOverride = true;
            _followingRoute = false;
            _stopped = false;
        }

        public void ResumeRoute()
        {
            _hasOverride = false;
            _followingRoute = true;
            _stopped = false;
        }

        public void StopMovement()
        {
            _stopped = true;
            _normalizedSpeed = 0f;
        }

        public bool HasReachedDestination(float threshold = 0.3f)
        {
            Vector3 target;
            if (_hasOverride)
                target = _overrideDestination;
            else if (_followingRoute && _route != null && _routeIndex < _route.Length)
                target = _route[_routeIndex];
            else
                return true;

            var diff = target - transform.position;
            diff.y = 0f;
            return diff.sqrMagnitude <= threshold * threshold;
        }

        public void SetAIState(EnemyAIState state)
        {
            _aiState = (byte)state;
        }

        public void NotifyReachedObjective()
        {
            if (!IsAlive) return;

            var objective = ServiceLocator.Get<ObjectiveController>();
            if (objective != null)
                objective.TakeDamage(new DamageInfo(_objectiveDamage, DamageType.Direct));

            OnReachedObjective?.Invoke();
            Destroy(gameObject);
        }
    }
}
