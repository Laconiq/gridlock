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
            RegisterProjectile(projectile);

            float spawnWarpY = _warpManager.Initialized
                ? _warpManager.GetWarpOffset(projectile.Position.X, projectile.Position.Z) : 0f;
            var muzzlePos = new Vector3(projectile.Position.X, projectile.Position.Y + spawnWarpY, projectile.Position.Z);
            var projectileColor = GetProjectileColor(projectile.Context.Tags);
            _particles.Burst(muzzlePos, 8, 7.5f, 0f, 0.09f, projectileColor, 18f);

            if (_warpManager.Initialized)
                _warpManager.DropStone(projectile.Position, 1.5f, 2f, projectileColor);

            _soundManager.Play(SoundType.TowerFire, worldPos: projectile.Position);
        }

        private void RegisterProjectile(ModProjectile projectile)
        {
            _projectiles.Add(projectile);
            projectile.OnDestroyed += OnProjectileDestroyed;

            var tags = projectile.Context.Tags;
            var color = GetProjectileColor(tags);
            float scale = 0.2f + Math.Clamp(projectile.Context.Damage / 40f, 0f, 1f) * 0.3f;
            float trailWidth = scale * 0.6f;
            var startColor = new Color(
                (byte)(color.R + (255 - color.R) / 2),
                (byte)(color.G + (255 - color.G) / 2),
                (byte)(color.B + (255 - color.B) / 2),
                (byte)230);
            var endColor = new Color((byte)(color.R / 4), (byte)(color.G / 4), (byte)(color.B / 4), (byte)0);
            int trailId = _trails.CreateTrail(0.2f, trailWidth, startColor, endColor, color);
            _projectileTrails[projectile.GetHashCode()] = trailId;
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
