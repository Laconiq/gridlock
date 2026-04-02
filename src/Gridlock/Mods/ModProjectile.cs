using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Combat;
using Gridlock.Enemies;
using Gridlock.Mods.Pipeline;

namespace Gridlock.Mods
{
    public sealed class ModProjectile
    {
        private const float FlyHeight = 0.5f;
        private const float BaseHitRadius = 0.5f;

        private ModPipeline _pipeline = null!;
        private ModContext _ctx;
        private bool _initialized;
        private bool _destroyed;

        public Vector3 Position => _ctx.Position;
        public Vector3 Direction => _ctx.Direction;
        public bool IsDestroyed => _destroyed;
        public ModContext Context => _ctx;

        private float HitRadius => _ctx.Size + BaseHitRadius;

        public event Action<ModProjectile>? OnDestroyed;
        public event Action<ModProjectile, SpawnRequest>? OnSpawnRequested;

        public void Initialize(ModPipeline pipeline, ModContext ctx, ITargetable target, Vector3 origin)
        {
            _pipeline = pipeline;
            _ctx = ctx;
            _ctx.OwnerPipeline = pipeline;

            origin.Y = FlyHeight;
            _ctx.Position = origin;
            _ctx.Target = target;
            _ctx.Direction = (target != null && target.IsAlive)
                ? FlatDirection(target.Position, origin)
                : new Vector3(0f, 0f, 1f);

            _pipeline.RunPhase(StagePhase.Configure, ref _ctx);

            if (_ctx.Consumed)
            {
                DrainSpawns();
                DestroyProjectile();
                return;
            }

            _initialized = true;
        }

        public void OverrideDirection(Vector3 dir)
        {
            _ctx.Direction = FlatNormalized(dir);
        }

        public void Update(float dt)
        {
            if (!_initialized || _destroyed) return;

            _ctx.DeltaTime = dt;
            _ctx.Lifetime += dt;

            if (_ctx.Lifetime >= _ctx.MaxLifetime)
            {
                _pipeline.RunPhase(StagePhase.OnExpire, ref _ctx);
                DrainSpawns();
                DestroyProjectile();
                return;
            }

            _pipeline.RunPhase(StagePhase.OnUpdate, ref _ctx);

            if (_ctx.SpawnRequests.Count > 0)
                DrainSpawns();

            if (_ctx.Consumed)
            {
                DrainSpawns();
                DestroyProjectile();
                return;
            }

            _ctx.Position += _ctx.Direction * (_ctx.Speed * dt);
            CheckCollision();
        }

        private void CheckCollision()
        {
            bool homing = _ctx.Tags.HasFlag(ModTags.Homing);

            if (homing && _ctx.Target != null && _ctx.Target.IsAlive)
            {
                int id = _ctx.Target.EntityId;
                if (_ctx.HitInstances.Contains(id))
                {
                    _ctx.Target = null;
                }
                else
                {
                    float distSq = FlatDistanceSq(_ctx.Position, _ctx.Target.Position);
                    float r = HitRadius;
                    if (distSq > r * r) return;

                    _ctx.HitInstances.Add(id);
                    var dmg = _ctx.Target.Damageable;
                    if (dmg != null)
                        ProcessHit(dmg, _ctx.Target, _ctx.Target.Position);
                    return;
                }
            }

            SweepCollision();
        }

        private void SweepCollision()
        {
            float r = HitRadius;
            Vector3 pos = _ctx.Position;
            Vector3 nextPos = pos + _ctx.Direction * (_ctx.Speed * _ctx.DeltaTime);

            Enemy? bestEnemy = null;
            float bestDist = float.MaxValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var enemy = entries[i];
                if (!enemy.IsAlive) continue;
                if (_ctx.HitInstances.Contains(enemy.EntityId)) continue;

                float dist = DistanceToSegmentXZ(enemy.Position, pos, nextPos);
                if (dist <= r && dist < bestDist)
                {
                    bestDist = dist;
                    bestEnemy = enemy;
                }
            }

            if (bestEnemy == null) return;

            _ctx.HitInstances.Add(bestEnemy.EntityId);
            ProcessHit(bestEnemy.Health, bestEnemy, bestEnemy.Position);
        }

        private void ProcessHit(IDamageable damageable, ITargetable hitTarget, Vector3 hitPoint)
        {
            if (_destroyed) return;

            _ctx.HitTarget = hitTarget;
            _ctx.Position = hitPoint;

            bool wasAlive = hitTarget.IsAlive;
            float hpBeforeHit = hitTarget.CurrentHP;

            damageable.TakeDamage(new DamageInfo(_ctx.Damage, DamageType.Projectile));

            _ctx.KilledThisHit = wasAlive && !hitTarget.IsAlive;
            _ctx.OverkillAmount = _ctx.KilledThisHit ? MathF.Max(0f, _ctx.Damage - hpBeforeHit) : 0f;

            _pipeline.RunPhase(StagePhase.OnHit, ref _ctx);

            _ctx.Consumed = true;
            _pipeline.RunPhase(StagePhase.PostHit, ref _ctx);

            DrainSpawns();

            if (_ctx.Consumed)
                DestroyProjectile();
        }

        private void DrainSpawns()
        {
            for (int i = 0; i < _ctx.SpawnRequests.Count; i++)
                OnSpawnRequested?.Invoke(this, _ctx.SpawnRequests[i]);

            _ctx.SpawnRequests.Clear();
        }

        private void DestroyProjectile()
        {
            if (_destroyed) return;
            _destroyed = true;
            OnDestroyed?.Invoke(this);
        }

        private static Vector3 FlatDirection(Vector3 to, Vector3 from)
        {
            var d = to - from;
            d.Y = 0f;
            return d.LengthSquared() > 0.001f ? Vector3.Normalize(d) : new Vector3(0f, 0f, 1f);
        }

        private static Vector3 FlatNormalized(Vector3 v)
        {
            v.Y = 0f;
            return v.LengthSquared() > 0.001f ? Vector3.Normalize(v) : new Vector3(0f, 0f, 1f);
        }

        private static float FlatDistanceSq(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return dx * dx + dz * dz;
        }

        private static float DistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            var p2 = new Vector2(point.X, point.Z);
            var a2 = new Vector2(a.X, a.Z);
            var b2 = new Vector2(b.X, b.Z);
            var ab = b2 - a2;
            var ap = p2 - a2;
            float denom = Vector2.Dot(ab, ab);
            if (denom < 0.0001f) return Vector2.Distance(p2, a2);
            float t = Math.Clamp(Vector2.Dot(ap, ab) / denom, 0f, 1f);
            return Vector2.Distance(p2, a2 + ab * t);
        }
    }
}
