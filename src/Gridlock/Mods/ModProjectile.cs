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

        private static int _nextId;
        public int EntityId { get; } = _nextId++;

        private ModPipeline _pipeline = null!;
        private ModContext _ctx;
        private bool _initialized;
        private bool _destroyed;

        public Vector3 Position => _ctx.Position;
        public Vector3 Direction => _ctx.Direction;
        public bool IsDestroyed => _destroyed;
        public ModContext Context => _ctx;

        private float HitRadius => _ctx.Size + BaseHitRadius;

        public static Action<ModProjectile>? OnProjectileCreated;
        public event Action<ModProjectile>? OnDestroyed;

        public void Initialize(ModPipeline pipeline, ModContext ctx, ITargetable? target, Vector3 origin)
        {
            _pipeline = pipeline;
            _ctx = ctx;
            _ctx.OwnerPipeline = pipeline;

            origin.Y = FlyHeight;
            _ctx.Position = origin;
            _ctx.SetTarget(target);
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

            Vector3 prevPos = _ctx.Position;
            _ctx.Position += _ctx.Direction * (_ctx.Speed * dt);
            CheckCollision(prevPos);
        }

        private void CheckCollision(Vector3 prevPos)
        {
            var target = _ctx.Tags.HasFlag(ModTags.Homing) ? _ctx.ValidTarget : null;

            if (target != null)
            {
                int id = target.EntityId;
                if (_ctx.HitInstances.Contains(id))
                {
                    _ctx.SetTarget(null);
                }
                else if (DistanceToSegmentXZ(target.Position, prevPos, _ctx.Position, out _) <= HitRadius)
                {
                    // Test the swept segment, not the end point: fast homing projectiles
                    // overshoot their target by more than the hit radius each tick.
                    _ctx.HitInstances.Add(id);
                    var dmg = target.Damageable;
                    if (dmg != null)
                        ProcessHit(dmg, target, target.Position);
                    return;
                }
            }

            SweepCollision(prevPos);
        }

        [ThreadStatic] private static List<Enemy>? _sweepBuffer;

        private void SweepCollision(Vector3 prevPos)
        {
            float r = HitRadius;
            // Sweep the segment actually travelled this frame [prevPos -> current position],
            // not one full step ahead of it.
            Vector3 pos = prevPos;
            Vector3 nextPos = _ctx.Position;

            _sweepBuffer ??= new List<Enemy>(32);
            _sweepBuffer.Clear();
            EnemyRegistry.Spatial.QuerySegment(pos, nextPos, r, _sweepBuffer);

            // Hit the first enemy along the travelled segment, not the one closest to its centre
            // line, so a piercing projectile doesn't skip an enemy it passed through first.
            Enemy? bestEnemy = null;
            float bestT = float.MaxValue;

            for (int i = 0; i < _sweepBuffer.Count; i++)
            {
                var enemy = _sweepBuffer[i];
                if (!enemy.IsAlive) continue;
                if (_ctx.HitInstances.Contains(enemy.EntityId)) continue;

                float dist = DistanceToSegmentXZ(enemy.Position, pos, nextPos, out float t);
                if (dist <= r && t < bestT)
                {
                    bestT = t;
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
            {
                var req = _ctx.SpawnRequests[i];
                var subPipeline = req.Pipeline ?? new ModPipeline();
                var subCtx = _ctx.CloneForSub(req.DamageScale);
                subCtx.Tags = subPipeline.AccumulatedTags;
                PipelineCompiler.ApplyContextSynergies(ref subCtx, subCtx.Tags, subCtx.Synergies);

                var sub = new ModProjectile();
                sub.Initialize(subPipeline, subCtx, req.Target ?? _ctx.ValidTarget, req.Origin);
                if (!sub.IsDestroyed)
                {
                    if (req.Direction.LengthSquared() > 0.001f)
                        sub.OverrideDirection(req.Direction);
                    OnProjectileCreated?.Invoke(sub);
                }
            }
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

        private static float DistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b, out float t)
        {
            var p2 = new Vector2(point.X, point.Z);
            var a2 = new Vector2(a.X, a.Z);
            var b2 = new Vector2(b.X, b.Z);
            var ab = b2 - a2;
            var ap = p2 - a2;
            float denom = Vector2.Dot(ab, ab);
            t = 0f;
            if (denom < 0.0001f) return Vector2.Distance(p2, a2);
            t = Math.Clamp(Vector2.Dot(ap, ab) / denom, 0f, 1f);
            return Vector2.Distance(p2, a2 + ab * t);
        }
    }
}
