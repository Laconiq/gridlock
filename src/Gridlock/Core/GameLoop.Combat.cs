using System.Numerics;
using Gridlock.Audio;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Raylib_cs;
using Color = Raylib_cs.Color;

namespace Gridlock.Core
{
    public sealed partial class GameLoop
    {
        private void OnProjectileSpawned(ModProjectile projectile)
        {
            _projectiles.Add(projectile);
            projectile.OnDestroyed += OnProjectileDestroyed;
            projectile.OnSpawnRequested += OnSubProjectileRequested;

            var tags = projectile.Context.Tags;
            var projectileColor = GetProjectileColor(tags);
            float trailWidth = 0.1f + projectile.Context.Damage / 40f * 0.15f;
            var startColor = new Color((byte)255, (byte)255, (byte)255, (byte)230);
            var endColor = new Color((byte)(projectileColor.R / 3), (byte)(projectileColor.G / 3), (byte)(projectileColor.B / 3), (byte)0);
            int trailId = _trails.CreateTrail(0.2f, trailWidth, startColor, endColor, projectileColor);
            _projectileTrails[projectile.GetHashCode()] = trailId;

            _particles.Burst(projectile.Position, 8, 7.5f, 0f, 0.09f, projectileColor, 18f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 1.5f, 2f, new Color(0, 255, 255, 255));

            _soundManager.Play(SoundType.TowerFire, worldPos: projectile.Position);
        }

        private void OnProjectileDestroyed(ModProjectile projectile)
        {
            var projColor = GetProjectileColor(projectile.Context.Tags);
            _particles.BurstSphere(projectile.Position, 12, 4f, 5f, 0.15f, projColor);

            _impactFlash.Spawn(projectile.Position, projColor, 0.5f, 0.15f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 3f, 3f, new Color(255, 102, 26, 255));

            if (_projectileTrails.TryGetValue(projectile.GetHashCode(), out int trailId))
            {
                _trails.DestroyTrail(trailId);
                _projectileTrails.Remove(projectile.GetHashCode());
            }

            _soundManager.Play(SoundType.ProjectileImpact, worldPos: projectile.Position);

            if (_postProcessingAvailable)
                _bloomPulse = MathF.Max(_bloomPulse, BaseBloomIntensity + 1f);

            TriggerShake(0.05f, 0.08f);
        }

        private void OnSubProjectileRequested(ModProjectile parent, SpawnRequest request)
        {
            var sub = new ModProjectile();
            var pipeline = request.Pipeline?.Clone() ?? parent.Context.OwnerPipeline?.Clone();
            if (pipeline == null) return;

            var ctx = parent.Context.CloneForSub(request.DamageScale);
            sub.Initialize(pipeline, ctx, request.Target!, request.Origin);

            if (!sub.IsDestroyed)
            {
                if (request.Direction != Vector3.Zero)
                    sub.OverrideDirection(request.Direction);

                _projectileSpawnBuffer.Add(sub);
            }
        }

        private void DrainProjectileSpawnBuffer()
        {
            foreach (var proj in _projectileSpawnBuffer)
            {
                _projectiles.Add(proj);
                proj.OnDestroyed += OnProjectileDestroyed;
                proj.OnSpawnRequested += OnSubProjectileRequested;

                var tags = proj.Context.Tags;
                var trailColor = GetProjectileColor(tags);
                float subTrailWidth = 0.08f + proj.Context.Damage / 40f * 0.1f;
                var subStartColor = new Color((byte)255, (byte)255, (byte)255, (byte)200);
                var subEndColor = new Color((byte)(trailColor.R / 3), (byte)(trailColor.G / 3), (byte)(trailColor.B / 3), (byte)0);
                int trailId = _trails.CreateTrail(0.25f, subTrailWidth, subStartColor, subEndColor, trailColor);
                _projectileTrails[proj.GetHashCode()] = trailId;
            }
            _projectileSpawnBuffer.Clear();
        }

        private void CleanupDestroyedProjectiles()
        {
            _projectileRemovalBuffer.Clear();
            for (int i = 0; i < _projectiles.Count; i++)
            {
                if (_projectiles[i].IsDestroyed)
                    _projectileRemovalBuffer.Add(_projectiles[i]);
            }
            foreach (var proj in _projectileRemovalBuffer)
            {
                _projectiles.Remove(proj);
                proj.OnDestroyed -= OnProjectileDestroyed;
                proj.OnSpawnRequested -= OnSubProjectileRequested;
            }
        }

        private void UpdateProjectileTrails()
        {
            foreach (var proj in _projectiles)
            {
                if (proj.IsDestroyed) continue;
                if (_projectileTrails.TryGetValue(proj.GetHashCode(), out int trailId))
                {
                    float warpY = _warpManager.Initialized
                        ? _warpManager.GetWarpOffset(proj.Position.X, proj.Position.Z) : 0f;
                    var warpedPos = new Vector3(proj.Position.X, proj.Position.Y + warpY, proj.Position.Z);
                    _trails.AddPoint(trailId, warpedPos);
                }
            }
        }
    }
}
