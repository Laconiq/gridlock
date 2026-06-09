using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Combat;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Mods.Pipeline;
using Gridlock.Towers;

namespace Gridlock.Mods
{
    public sealed class ModSlotExecutor
    {
        private readonly Tower _tower;
        private readonly List<ModSlotData> _modSlots = new();
        private readonly List<SynergyEffect> _activeSynergies = new();

        private float _fireTimer;
        private ModPipeline? _cachedPipeline;
        private ModContext _cachedBaseCtx;
        private bool _configDirty = true;

        private const float MinFireRate = 0.1f;
        private const float MaxFireRate = 20f;

        public List<ModSlotData> ModSlots => _modSlots;
        public TargetingMode TargetingMode { get; set; } = TargetingMode.First;

        public event Action<ModProjectile>? OnProjectileSpawned;

        public ModSlotExecutor(Tower tower)
        {
            _tower = tower;
        }

        public void ApplyPreset(ModSlotPreset preset)
        {
            TargetingMode = preset.TargetingMode;
            _modSlots.Clear();
            foreach (var modType in preset.Slots)
                _modSlots.Add(new ModSlotData { modType = modType });
            _configDirty = true;
        }

        public void SetSlots(List<ModSlotData> slots)
        {
            _modSlots.Clear();
            _modSlots.AddRange(slots);
            _configDirty = true;
        }

        public void Update(float dt)
        {
            if (_modSlots.Count == 0) return;

            if (_configDirty)
            {
                (_cachedPipeline, _cachedBaseCtx) = PipelineCompiler.Compile(
                    _modSlots, _tower.Data.BaseDamage, _activeSynergies);
                _configDirty = false;
            }

            float fireRate = Math.Clamp(_tower.Data.FireRate, MinFireRate, MaxFireRate);

            if (_activeSynergies.Contains(SynergyEffect.Machinegun))
                fireRate = MathF.Min(fireRate * 2f, MaxFireRate);

            float interval = 1f / fireRate;
            _fireTimer += dt;
            if (_fireTimer < interval) return;

            var target = SelectTarget();
            if (target == null) return;

            _fireTimer = 0f;
            SpawnProjectile(target, dt);
        }

        [ThreadStatic] private static List<Enemy>? _targetBuffer;

        private ITargetable? SelectTarget()
        {
            float range = _tower.Data.BaseRange;
            float rangeSq = range * range;
            Vector3 towerPos = _tower.Position;

            // Query the spatial hash by range instead of scanning every enemy. The box query is
            // narrowed back to the exact range circle by the distSq check below, so the selected
            // target is identical to the old full-registry scan.
            var buf = _targetBuffer ??= new List<Enemy>(32);
            buf.Clear();
            EnemyRegistry.Spatial.QuerySegment(towerPos, towerPos, range, buf);
            if (buf.Count == 0) return null;

            Enemy? best = null;
            float bestScore = float.MaxValue;

            for (int i = 0; i < buf.Count; i++)
            {
                var enemy = buf[i];
                if (!enemy.IsAlive) continue;

                float distSq = Vector3.DistanceSquared(enemy.Position, towerPos);
                if (distSq > rangeSq) continue;

                float score = EvaluateTarget(enemy, distSq);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            return best;
        }

        private float EvaluateTarget(Enemy enemy, float distSqToTower)
        {
            return TargetingMode switch
            {
                TargetingMode.First => -enemy.RouteProgress,
                TargetingMode.Last => enemy.RouteProgress,
                TargetingMode.Nearest => distSqToTower,
                TargetingMode.Strongest => -enemy.Health.CurrentHP,
                TargetingMode.Weakest => enemy.Health.CurrentHP,
                _ => distSqToTower
            };
        }

        private void SpawnProjectile(ITargetable target, float dt)
        {
            if (_cachedPipeline == null) return;

            Vector3 spawnPos = _tower.FirePoint;
            // The compiled pipeline is immutable during flight (no stage mutates pipeline/stage
            // state), so share it across shots instead of cloning per shot. Only the context holds
            // per-projectile mutable state and still needs a per-shot copy.
            var pipeline = _cachedPipeline;
            var ctx = _cachedBaseCtx.Clone();
            ctx.DeltaTime = dt;
            ctx.ObjectiveHealer = ObjectiveController.Instance;

            var projectile = new ModProjectile();
            projectile.Initialize(pipeline, ctx, target, spawnPos);
            OnProjectileSpawned?.Invoke(projectile);
        }
    }
}
