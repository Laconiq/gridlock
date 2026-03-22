using AIWE.Combat;
using AIWE.Core;
using AIWE.Enemies;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] private float detectionRadius = 12f;
        [SerializeField] private float threatEvalInterval = 0.5f;

        [Header("Leash / De-aggro")]
        [SerializeField] private float leashRadius = 15f;
        [SerializeField] private float maxRouteDistance = 20f;
        [SerializeField] private float combatTimeout = 5f;

        [Header("Movement")]
        [SerializeField] private float waypointReachDistance = 1.5f;

        [Header("Melee")]
        [SerializeField] private float meleeRange = 1.5f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private float attackDamage = 10f;

        [Header("Threat Config")]
        [SerializeField] private ThreatCalculatorConfig threatConfig;

        private EnemyController _controller;
        private ThreatCalculator _threatCalc;

        private EnemyAIState _state;
        private Vector3[] _route;
        private int _routeId;
        private int _currentWaypointIndex;
        private ITargetable _currentTarget;
        private float _attackTimer;
        private float _combatTimer;
        private float _threatEvalTimer;
        private bool _initialized;

        public float RouteProgress
        {
            get
            {
                if (_route == null || _route.Length == 0) return 0f;
                if (_currentWaypointIndex >= _route.Length) return _route.Length;

                float baseProg = _currentWaypointIndex;
                if (_currentWaypointIndex < _route.Length)
                {
                    float segDist = Vector3.Distance(
                        _currentWaypointIndex > 0 ? _route[_currentWaypointIndex - 1] : transform.position,
                        _route[_currentWaypointIndex]);
                    if (segDist > 0.01f)
                    {
                        float toDest = Vector3.Distance(transform.position, _route[_currentWaypointIndex]);
                        baseProg += 1f - Mathf.Clamp01(toDest / segDist);
                    }
                }

                return baseProg;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
        }

        public void Setup(int routeId, EnemyDefinition definition)
        {
            _routeId = routeId;

            detectionRadius = definition.detectionRadius;
            leashRadius = definition.leashRadius;
            attackDamage = definition.attackDamage;
            attackCooldown = definition.attackCooldown;
            meleeRange = definition.attackRange;

            var routeManager = ServiceLocator.Get<RouteManager>();
            if (routeManager != null)
            {
                _route = routeManager.GetRoute(routeId);
                _currentWaypointIndex = routeManager.GetNearestWaypointIndex(routeId, transform.position);
            }

            if (threatConfig != null)
                _threatCalc = new ThreatCalculator(threatConfig);

            var netObj = _controller.NetworkObject;
            _threatEvalTimer = (netObj != null ? netObj.NetworkObjectId % 10 : 0) * 0.05f;

            TransitionTo(EnemyAIState.FollowRoute);
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || !_controller.IsServer || !_controller.IsAlive) return;

            switch (_state)
            {
                case EnemyAIState.FollowRoute:
                    TickFollowRoute();
                    break;
                case EnemyAIState.ChaseTarget:
                    TickChaseTarget();
                    break;
                case EnemyAIState.Attack:
                    TickAttack();
                    break;
                case EnemyAIState.ReturnToRoute:
                    TickReturnToRoute();
                    break;
            }
        }

        private void TickFollowRoute()
        {
            if (_route == null || _route.Length == 0) return;

            if (_currentWaypointIndex >= _route.Length)
            {
                _controller.NotifyReachedObjective();
                return;
            }

            var target = _route[_currentWaypointIndex];
            _controller.SetDestination(target);

            if (_controller.HasReachedDestination(waypointReachDistance))
            {
                _currentWaypointIndex++;
                if (_currentWaypointIndex >= _route.Length)
                {
                    _controller.NotifyReachedObjective();
                    return;
                }
            }

            TryEvaluateThreats();
        }

        private void TickChaseTarget()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                ClearTarget();
                TransitionTo(EnemyAIState.ReturnToRoute);
                return;
            }

            if (CheckDeAggro())
            {
                ClearTarget();
                TransitionTo(EnemyAIState.ReturnToRoute);
                return;
            }

            _controller.SetDestination(_currentTarget.Position);

            float dist = Vector3.Distance(transform.position, _currentTarget.Position);
            if (dist <= meleeRange)
                TransitionTo(EnemyAIState.Attack);
        }

        private void TickAttack()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                ClearTarget();
                TransitionTo(EnemyAIState.ReturnToRoute);
                return;
            }

            if (CheckDeAggro())
            {
                ClearTarget();
                TransitionTo(EnemyAIState.ReturnToRoute);
                return;
            }

            float dist = Vector3.Distance(transform.position, _currentTarget.Position);
            if (dist > meleeRange * 1.2f)
            {
                TransitionTo(EnemyAIState.ChaseTarget);
                return;
            }

            _controller.StopMovement();

            var dir = (_currentTarget.Position - transform.position).normalized;
            if (dir.sqrMagnitude > 0.001f)
            {
                dir.y = 0f;
                transform.rotation = Quaternion.LookRotation(dir);
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                _attackTimer = attackCooldown;
                var damageable = _currentTarget.Transform.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    var dmgInfo = new DamageInfo(attackDamage, _controller.NetworkObjectId, DamageType.Direct);
                    damageable.TakeDamage(dmgInfo);
                    _combatTimer = 0f;
                }
            }
        }

        private void TickReturnToRoute()
        {
            if (_route == null || _route.Length == 0)
            {
                TransitionTo(EnemyAIState.FollowRoute);
                return;
            }

            var routeManager = ServiceLocator.Get<RouteManager>();
            if (routeManager != null)
                _currentWaypointIndex = routeManager.GetNearestWaypointIndex(_routeId, transform.position);

            _controller.SetDestination(_route[_currentWaypointIndex]);

            TryEvaluateThreats();

            if (_controller.HasReachedDestination(waypointReachDistance))
                TransitionTo(EnemyAIState.FollowRoute);
        }

        private void TryEvaluateThreats()
        {
            if (_threatCalc == null) return;

            _threatEvalTimer -= Time.deltaTime;
            if (_threatEvalTimer > 0f) return;
            _threatEvalTimer = threatEvalInterval;

            var (target, score) = _threatCalc.Evaluate(transform.position, detectionRadius, transform);
            if (target != null)
            {
                SetTarget(target);
                TransitionTo(EnemyAIState.ChaseTarget);
            }
        }

        private bool CheckDeAggro()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
                return true;

            float distToTarget = Vector3.Distance(transform.position, _currentTarget.Position);
            if (distToTarget > leashRadius)
                return true;

            _combatTimer += Time.deltaTime;
            if (_combatTimer >= combatTimeout)
                return true;

            var routeManager = ServiceLocator.Get<RouteManager>();
            if (routeManager != null)
            {
                float distToRoute = routeManager.GetDistanceToRoute(_routeId, transform.position);
                if (distToRoute > maxRouteDistance)
                    return true;
            }

            return false;
        }

        private void TransitionTo(EnemyAIState newState)
        {
            _state = newState;
            _controller.SetAIState(newState);

            if (newState == EnemyAIState.Attack)
                _attackTimer = 0f;

            if (newState == EnemyAIState.ChaseTarget)
                _combatTimer = 0f;
        }

        private void SetTarget(ITargetable target)
        {
            if (_currentTarget != null)
                EnemyTargetRegistry.UnregisterTarget(_currentTarget);

            _currentTarget = target;
            EnemyTargetRegistry.RegisterTarget(target);
            _combatTimer = 0f;
        }

        private void ClearTarget()
        {
            if (_currentTarget != null)
                EnemyTargetRegistry.UnregisterTarget(_currentTarget);

            _currentTarget = null;
        }

        private void OnDestroy()
        {
            ClearTarget();
        }
    }
}
