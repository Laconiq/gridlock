using System;
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
            float projScale = 0.2f + Math.Clamp(projectile.Context.Damage / 40f, 0f, 1f) * 0.3f;
            float trailWidth = projScale * 0.6f;
            var startColor = new Color(
                (byte)(projectileColor.R + (255 - projectileColor.R) / 2),
                (byte)(projectileColor.G + (255 - projectileColor.G) / 2),
                (byte)(projectileColor.B + (255 - projectileColor.B) / 2),
                (byte)230);
            var endColor = new Color((byte)(projectileColor.R / 4), (byte)(projectileColor.G / 4), (byte)(projectileColor.B / 4), (byte)0);
            int trailId = _trails.CreateTrail(0.2f, trailWidth, startColor, endColor, projectileColor);
            _projectileTrails[projectile.GetHashCode()] = trailId;

            float spawnWarpY = _warpManager.Initialized
                ? _warpManager.GetWarpOffset(projectile.Position.X, projectile.Position.Z) : 0f;
            var muzzlePos = new Vector3(projectile.Position.X, projectile.Position.Y + spawnWarpY, projectile.Position.Z);
            _particles.Burst(muzzlePos, 8, 7.5f, 0f, 0.09f, projectileColor, 18f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 1.5f, 2f, projectileColor);

            _soundManager.Play(SoundType.TowerFire, worldPos: projectile.Position);
        }

        private void OnProjectileDestroyed(ModProjectile projectile)
        {
            var projColor = GetProjectileColor(projectile.Context.Tags);

            float warpY = _warpManager.Initialized
                ? _warpManager.GetWarpOffset(projectile.Position.X, projectile.Position.Z) : 0f;
            var impactPos = new Vector3(projectile.Position.X, projectile.Position.Y + warpY, projectile.Position.Z);

            float intensity = Math.Clamp(projectile.Context.Damage / 30f, 0f, 1f);
            int particleCount = (int)MathF.Round(6f + 10f * intensity);
            float particleSpeed = (3f + 5f * intensity) * intensity;

            _particles.BurstSphere(impactPos, particleCount, particleSpeed, 5f, 0.15f, projColor);
            _impactFlash.Spawn(impactPos, projColor, 0.4f + 0.2f * intensity, 0.15f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 3f, 3f, projColor);

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
                float subScale = 0.15f + Math.Clamp(proj.Context.Damage / 40f, 0f, 1f) * 0.2f;
                float subTrailWidth = subScale * 0.5f;
                var subStartColor = new Color(
                    (byte)(trailColor.R + (255 - trailColor.R) / 2),
                    (byte)(trailColor.G + (255 - trailColor.G) / 2),
                    (byte)(trailColor.B + (255 - trailColor.B) / 2),
                    (byte)200);
                var subEndColor = new Color((byte)(trailColor.R / 4), (byte)(trailColor.G / 4), (byte)(trailColor.B / 4), (byte)0);
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
