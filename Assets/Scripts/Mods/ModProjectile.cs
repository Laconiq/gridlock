using Gridlock.Combat;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using Gridlock.Mods.Pipeline;
using Gridlock.Visual;
using UnityEngine;

namespace Gridlock.Mods
{
    public class ModProjectile : MonoBehaviour
    {
        private const float FlyHeight = 0.5f;

        [SerializeField] private float baseHitRadius = 0.5f;

        private ModPipeline _pipeline;
        private ModContext _ctx;
        private bool _initialized;
        private bool _destroyed;

        private float HitRadius => _ctx.Size + baseHitRadius;

        public void Initialize(ModPipeline pipeline, ModContext ctx, ITargetable target, Vector3 origin)
        {
            _pipeline = pipeline;
            _ctx = ctx;
            _ctx.OwnerPipeline = pipeline;

            origin.y = FlyHeight;
            _ctx.Position = origin;
            _ctx.Target = target;
            _ctx.Direction = (target != null && target.IsAlive)
                ? FlatDirection(target.Position, origin)
                : FlatNormalized(transform.forward);

            transform.position = _ctx.Position;
            var initDir = _ctx.Direction;
            initDir.y = 0f;
            if (initDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(initDir);

            var wf = GetComponent<WarpFollower>();
            if (wf == null) wf = gameObject.AddComponent<WarpFollower>();
            wf.SetBaseY(FlyHeight);

            _pipeline.RunPhase(StagePhase.Configure, ref _ctx);

            if (_ctx.Consumed)
            {
                DrainSpawns();
                DestroyProjectile();
                return;
            }

            GameJuice.Instance?.OnTowerFired(origin);
            _initialized = true;
        }

        public void OverrideDirection(Vector3 dir)
        {
            _ctx.Direction = FlatNormalized(dir);
            if (_ctx.Direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(_ctx.Direction);
        }

        private void Update()
        {
            if (!_initialized || _destroyed) return;

            float dt = Time.deltaTime;
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
            transform.position = _ctx.Position;

            var dir = _ctx.Direction;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);

            CheckCollision();
        }

        private void CheckCollision()
        {
            bool homing = _ctx.Tags.HasFlag(ModTags.Homing);

            if (homing && _ctx.Target != null && _ctx.Target.IsAlive && _ctx.Target.Transform != null)
            {
                var id = _ctx.Target.Transform.gameObject.GetEntityId();
                if (_ctx.HitInstances.Contains(id))
                {
                    _ctx.Target = null;
                }
                else
                {
                    float distSq = FlatDistanceSq(transform.position, _ctx.Target.Position);
                    float r = HitRadius;
                    if (distSq > r * r) return;

                    _ctx.HitInstances.Add(id);
                    var dmg = _ctx.Target.Transform.GetComponentInParent<IDamageable>();
                    if (dmg != null)
                        ProcessHit(dmg, _ctx.Target.Transform.gameObject, _ctx.Target.Position);
                    return;
                }
            }

            SweepCollision();
        }

        private void SweepCollision()
        {
            float r = HitRadius;
            Vector3 pos = transform.position;
            Vector3 nextPos = pos + _ctx.Direction * (_ctx.Speed * Time.deltaTime);

            EnemyEntry? bestEntry = null;
            float bestDist = float.MaxValue;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.Controller == null || !entry.Controller.IsAlive) continue;

                var id = entry.Controller.gameObject.GetEntityId();
                if (_ctx.HitInstances.Contains(id)) continue;

                float dist = DistanceToSegmentXZ(entry.Controller.Position, pos, nextPos);
                if (dist <= r && dist < bestDist)
                {
                    bestDist = dist;
                    bestEntry = entry;
                }
            }

            if (!bestEntry.HasValue) return;

            var e = bestEntry.Value;
            _ctx.HitInstances.Add(e.Controller.gameObject.GetEntityId());
            var damageable = e.Health as IDamageable;
            if (damageable != null)
                ProcessHit(damageable, e.Controller.gameObject, e.Controller.Position);
        }

        private void ProcessHit(IDamageable damageable, GameObject hitObject, Vector3 hitPoint)
        {
            if (_destroyed) return;

            _ctx.HitObject = hitObject;
            _ctx.Position = hitPoint;

            var health = hitObject.GetComponentInParent<EnemyHealth>();
            bool wasAlive = health != null && health.IsAlive;
            float hpBeforeHit = health != null ? health.CurrentHP : 0f;

            damageable.TakeDamage(new DamageInfo(_ctx.Damage, DamageType.Projectile));

            _ctx.KilledThisHit = wasAlive && health != null && !health.IsAlive;
            _ctx.OverkillAmount = _ctx.KilledThisHit ? Mathf.Max(0f, _ctx.Damage - hpBeforeHit) : 0f;

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
                var go = Instantiate(gameObject, req.Origin, Quaternion.identity);
                var proj = go.GetComponent<ModProjectile>();
                var subPipeline = req.Pipeline ?? new ModPipeline();
                var subCtx = _ctx.CloneForSub(req.DamageScale);
                subCtx.Tags = subPipeline.AccumulatedTags;
                if (subCtx.Tags.HasFlag(ModTags.Pierce)) subCtx.PierceRemaining = 3;
                if (subCtx.Tags.HasFlag(ModTags.Bounce)) subCtx.BounceRemaining = 3;
                proj.Initialize(subPipeline, subCtx, req.Target ?? _ctx.Target, req.Origin);
                if (req.Direction.sqrMagnitude > 0.001f)
                    proj.OverrideDirection(req.Direction);
            }
            _ctx.SpawnRequests.Clear();
        }

        private void DestroyProjectile()
        {
            if (_destroyed) return;
            _destroyed = true;
            Destroy(gameObject);
        }

        private static Vector3 FlatDirection(Vector3 to, Vector3 from)
        {
            var d = to - from;
            d.y = 0f;
            return d.sqrMagnitude > 0.001f ? d.normalized : Vector3.forward;
        }

        private static Vector3 FlatNormalized(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
        }

        private static float FlatDistanceSq(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        private static float DistanceToSegmentXZ(Vector3 point, Vector3 a, Vector3 b)
        {
            var p2 = new Vector2(point.x, point.z);
            var a2 = new Vector2(a.x, a.z);
            var b2 = new Vector2(b.x, b.z);
            var ab = b2 - a2;
            var ap = p2 - a2;
            float denom = Vector2.Dot(ab, ab);
            if (denom < 0.0001f) return Vector2.Distance(p2, a2);
            float t = Mathf.Clamp01(Vector2.Dot(ap, ab) / denom);
            return Vector2.Distance(p2, a2 + ab * t);
        }
    }
}
