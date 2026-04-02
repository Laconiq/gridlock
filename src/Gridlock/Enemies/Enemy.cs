using System;
using System.Numerics;
using Gridlock.Combat;

namespace Gridlock.Enemies
{
    public sealed class Enemy : ITargetable
    {
        private static int _nextId;

        private readonly float _floatY = 0.5f;
        private float _moveSpeed;
        private float _objectiveDamage;

        private Vector3[]? _route;
        private int _routeIndex;
        private bool _followingRoute;
        private bool _reachedObjective;

        public int EntityId { get; private set; }
        public Vector3 Position { get; set; }
        public bool IsAlive => Health.IsAlive;
        public EnemyHealth Health { get; }
        public StatusEffectManager StatusEffects { get; }
        public float MoveSpeed => _moveSpeed;
        public float NormalizedSpeed { get; private set; }
        public int RouteIndex => _routeIndex;
        private EnemyData _data;
        public EnemyData Data => _data;

        public IDamageable Damageable => Health;
        public float CurrentHP => Health.CurrentHP;
        public float MaxHP => Health.MaxHP;
        StatusEffectManager? ITargetable.StatusEffects => StatusEffects;

        public event Action<Enemy>? OnReachedObjective;
        public event Action<Enemy>? OnRemoved;

        public Enemy(EnemyData data, Vector3 spawnPos)
        {
            EntityId = _nextId++;
            _data = data;
            _moveSpeed = data.MoveSpeed;
            _objectiveDamage = data.ObjectiveDamage;

            spawnPos.Y = _floatY;
            Position = spawnPos;

            Health = new EnemyHealth(data.MaxHP);
            StatusEffects = new StatusEffectManager(Health);
            Health.SetStatusEffects(StatusEffects);
        }

        public void AssignRoute(Vector3[] route, int startIndex)
        {
            _route = route;
            _routeIndex = Math.Clamp(startIndex, 0, route.Length - 1);
            _followingRoute = true;
        }

        public void Update(float dt)
        {
            Health.Update(dt);
            StatusEffects.Update(dt);

            if (Health.PendingRemoval)
            {
                OnRemoved?.Invoke(this);
                return;
            }

            if (!IsAlive) return;

            float speed = _moveSpeed * StatusEffects.SpeedMultiplier;

            if (_followingRoute && _route != null)
                FollowRouteStep(speed, dt);

            NormalizedSpeed = _moveSpeed > 0f ? Math.Clamp(speed / _moveSpeed, 0f, 1f) : 0f;
        }

        private void FollowRouteStep(float speed, float dt)
        {
            if (_route == null || _routeIndex >= _route.Length)
            {
                NotifyReachedObjective();
                return;
            }

            float remaining = speed * dt;

            while (remaining > 0f && _routeIndex < _route.Length)
            {
                var target = _route[_routeIndex];
                target.Y = _floatY;

                var toTarget = target - Position;
                toTarget.Y = 0f;
                float dist = toTarget.Length();

                if (dist <= remaining)
                {
                    Position = new Vector3(target.X, _floatY, target.Z);
                    remaining -= dist;
                    _routeIndex++;
                }
                else
                {
                    var dir = toTarget / dist;
                    Position += dir * remaining;
                    remaining = 0f;
                }
            }

            if (_routeIndex >= _route.Length)
                NotifyReachedObjective();
        }

        private void NotifyReachedObjective()
        {
            if (!IsAlive || _reachedObjective) return;
            _reachedObjective = true;

            var objective = Core.ServiceLocator.Get<Core.ObjectiveController>();
            objective?.TakeDamage(new DamageInfo(_objectiveDamage, DamageType.Direct));

            OnReachedObjective?.Invoke(this);
            Health.ForceKill();
        }

        public float RouteProgress
        {
            get
            {
                if (_route == null || _route.Length == 0) return 0f;
                if (_routeIndex >= _route.Length) return _route.Length;

                float baseProg = _routeIndex;
                if (_routeIndex < _route.Length)
                {
                    var prevPos = _routeIndex > 0 ? _route[_routeIndex - 1] : Position;
                    float segDist = Vector3.Distance(prevPos, _route[_routeIndex]);
                    if (segDist > 0.01f)
                    {
                        float toDest = Vector3.Distance(Position, _route[_routeIndex]);
                        baseProg += 1f - Math.Clamp(toDest / segDist, 0f, 1f);
                    }
                }

                return baseProg;
            }
        }

        public void Reset(EnemyData data, Vector3 spawnPos)
        {
            EntityId = _nextId++;
            _data = data;
            _moveSpeed = data.MoveSpeed;
            _objectiveDamage = data.ObjectiveDamage;

            spawnPos.Y = _floatY;
            Position = spawnPos;

            _route = null;
            _routeIndex = 0;
            _followingRoute = false;
            _reachedObjective = false;
            NormalizedSpeed = 0f;

            Health.Reset(data.MaxHP);
            Health.SetStatusEffects(StatusEffects);
            StatusEffects.Reset();

            OnReachedObjective = null;
            OnRemoved = null;
        }

        public static void ResetIdCounter() => _nextId = 0;
    }
}
