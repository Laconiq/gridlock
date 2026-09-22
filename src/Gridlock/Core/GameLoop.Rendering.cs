using System;
using System.Numerics;
using Gridlock.Enemies;
using Gridlock.Grid;
using Gridlock.Loot;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Gridlock.Rendering;
using Gridlock.Visual;
using Gridlock.UI;
using Raylib_cs;
using rlImGui_cs;
using Color = Raylib_cs.Color;

namespace Gridlock.Core
{
    public sealed partial class GameLoop
    {
        private void Render3D(Camera3D cam)
        {
            var prof = Profiler.Instance;

            prof.Begin("  R.Grid");
            _gridVisual.Render();
            if (!_gridVisual.HasShader)
                DrawFallbackGrid();
            prof.End();

            prof.Begin("  R.Path");
            _pathVisualizer.Render(cam);
            prof.End();

            prof.Begin("  R.Towers");
            DrawTowers();
            prof.End();

            prof.Begin("  R.Enemies");
            DrawEnemies();
            prof.End();

            prof.Begin("  R.Trails");
            _trails.Render(cam);
            prof.End();

            prof.Begin("  R.Projectiles");
            DrawProjectiles();
            prof.End();

            DrawPickups();
            DrawPlacementPreview();

            prof.Begin("  R.Voxels");
            _voxelPool.Render();
            prof.End();

            prof.Begin("  R.ImpactFlash");
            _impactFlash.Render();
            prof.End();

            prof.Begin("  R.Particles");
            _particles.Render();
            prof.End();
        }

        private void DrawFallbackGrid()
        {
            var def = _gridManager.Definition;
            var origin = _gridManager.GridOrigin;
            float totalW = def.Width * def.CellSize;
            float totalH = def.Height * def.CellSize;

            for (int y = 0; y < def.Height; y++)
            {
                for (int x = 0; x < def.Width; x++)
                {
                    var cell = _gridManager.GetRuntimeCell(x, y);
                    if (cell == CellType.Blocked && _gridManager.Definition.GetCell(x, y) == CellType.Blocked) continue;

                    var worldPos = _gridManager.GridToWorld(new Vector2Int(x, y));
                    float warpY = _warpManager.Initialized
                        ? _warpManager.GetWarpOffset(worldPos.X, worldPos.Z)
                        : 0f;

                    var color = cell switch
                    {
                        CellType.Path => new Color(255, 0, 180, 40),
                        CellType.TowerSlot => new Color(0, 255, 100, 25),
                        CellType.Spawn => new Color(0, 255, 255, 60),
                        CellType.Objective => new Color(255, 255, 0, 60),
                        CellType.Blocked => new Color(100, 100, 100, 20),
                        _ => new Color(40, 40, 40, 15),
                    };

                    var rlPos = new Vector3(worldPos.X, 0.01f + warpY, worldPos.Z);
                    var rlSize = new Vector3(def.CellSize * 0.95f, 0.02f, def.CellSize * 0.95f);
                    Raylib.DrawCubeV(rlPos, rlSize, color);
                }
            }

            for (int x = 0; x <= def.Width; x++)
            {
                float wx = origin.X + x * def.CellSize;
                var a = new Vector3(wx, 0f, origin.Z);
                var b = new Vector3(wx, 0f, origin.Z + totalH);
                LineBatch.ThickLine3D(a, b, new Color(60, 60, 60, 80));
            }
            for (int y = 0; y <= def.Height; y++)
            {
                float wz = origin.Z + y * def.CellSize;
                var a = new Vector3(origin.X, 0f, wz);
                var b = new Vector3(origin.X + totalW, 0f, wz);
                LineBatch.ThickLine3D(a, b, new Color(60, 60, 60, 80));
            }
        }

        private void DrawTowers()
        {
            float time = (float)Raylib.GetTime();
            float dt = Raylib.GetFrameTime();
            var lb = _lineBatch;

            // Solid pass — all towers batched
            lb.Begin();
            foreach (var tower in _towerPlacement.PlacedTowers)
            {
                int id = tower.EntityId;

                if (!_towerBobPhases.ContainsKey(id))
                    _towerBobPhases[id] = Random.Shared.NextSingle() * MathF.Tau;
                if (!_towerSpinAngles.ContainsKey(id))
                    _towerSpinAngles[id] = 0f;

                _towerSpinAngles[id] += 30f * dt;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(tower.Position.X, tower.Position.Z)
                    : 0f;

                var pos = new Vector3(tower.Position.X, 0.5f + warpY, tower.Position.Z);
                bool selected = _selectedTower == tower;
                bool hovered = _hoveredTower == tower && !selected;

                var baseWireColor = selected
                    ? new Color((byte)0, (byte)255, (byte)200, (byte)255)
                    : hovered
                        ? new Color((byte)0, (byte)230, (byte)255, (byte)240)
                        : new Color((byte)0, (byte)180, (byte)255, (byte)200);

                float bobPhase = _towerBobPhases[id];
                float bob = MathF.Sin(time * 2f + bobPhase) * 0.06f;
                var turretPos = new Vector3(tower.Position.X, 1.2f + warpY + bob, tower.Position.Z);

                var turretColor = selected
                    ? new Color((byte)0, (byte)255, (byte)200, (byte)255)
                    : hovered
                        ? new Color((byte)0, (byte)245, (byte)255, (byte)245)
                        : new Color((byte)0, (byte)220, (byte)255, (byte)220);

                float spin = _towerSpinAngles[id] * MathF.PI / 180f;

                lb.CubeWires(pos, 1.6f, 1.0f, 1.6f, baseWireColor);
                lb.CubeWires(pos, 1.58f, 0.98f, 1.58f, baseWireColor);
                lb.CubeWires(pos, 1.62f, 1.02f, 1.62f, baseWireColor);

                lb.OctahedronWires(turretPos, 0.35f, 0.63f, spin, turretColor);
                lb.OctahedronWires(turretPos, 0.34f, 0.62f, spin, turretColor);
                lb.OctahedronWires(turretPos, 0.36f, 0.64f, spin, turretColor);
            }
            lb.Flush();

            // Additive glow pass — all towers batched
            Raylib.BeginBlendMode(BlendMode.Additive);
            lb.Begin();
            foreach (var tower in _towerPlacement.PlacedTowers)
            {
                int id = tower.EntityId;
                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(tower.Position.X, tower.Position.Z)
                    : 0f;

                var pos = new Vector3(tower.Position.X, 0.5f + warpY, tower.Position.Z);
                bool selected = _selectedTower == tower;
                bool hovered = _hoveredTower == tower && !selected;

                var baseWireColor = selected
                    ? new Color((byte)0, (byte)255, (byte)200, (byte)255)
                    : hovered
                        ? new Color((byte)0, (byte)230, (byte)255, (byte)240)
                        : new Color((byte)0, (byte)180, (byte)255, (byte)200);

                lb.CubeWires(pos, 1.75f, 1.15f, 1.75f,
                    new Color(baseWireColor.R, baseWireColor.G, baseWireColor.B, (byte)50));

                float bobPhase = _towerBobPhases.GetValueOrDefault(id, 0f);
                float bob = MathF.Sin(time * 2f + bobPhase) * 0.06f;
                var turretPos = new Vector3(tower.Position.X, 1.2f + warpY + bob, tower.Position.Z);

                var turretColor = selected
                    ? new Color((byte)0, (byte)255, (byte)200, (byte)255)
                    : hovered
                        ? new Color((byte)0, (byte)245, (byte)255, (byte)245)
                        : new Color((byte)0, (byte)220, (byte)255, (byte)220);

                float spin = (_towerSpinAngles.GetValueOrDefault(id, 0f)) * MathF.PI / 180f;

                lb.OctahedronWires(turretPos, 0.45f, 0.78f, spin,
                    new Color(turretColor.R, turretColor.G, turretColor.B, (byte)45));
                lb.OctahedronWires(turretPos, 0.12f, 0.12f, spin,
                    new Color(turretColor.R, turretColor.G, turretColor.B, (byte)30));
            }
            lb.Flush();
            Raylib.EndBlendMode();

            // Range indicator for selected/hovered tower
            if ((_selectedTower != null || _hoveredTower != null) && _gameManager.CurrentState == GameState.Preparing)
            {
                var tower = _selectedTower ?? _hoveredTower!;
                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(tower.Position.X, tower.Position.Z)
                    : 0f;
                var pos = new Vector3(tower.Position.X, 0.5f + warpY, tower.Position.Z);
                float range = tower.Data.BaseRange;
                var rangeColor = new Color(0, 255, 200, 50);

                Raylib.BeginBlendMode(BlendMode.Additive);
                lb.Begin();
                const int segments = 64;
                for (int i = 0; i < segments; i++)
                {
                    float a1 = (float)i / segments * MathF.Tau;
                    float a2 = (float)(i + 1) / segments * MathF.Tau;
                    var p1 = new Vector3(pos.X + MathF.Cos(a1) * range, 0.02f, pos.Z + MathF.Sin(a1) * range);
                    var p2 = new Vector3(pos.X + MathF.Cos(a2) * range, 0.02f, pos.Z + MathF.Sin(a2) * range);
                    lb.Line(p1, p2, rangeColor);
                }
                lb.Flush();
                Raylib.EndBlendMode();
            }
        }

        private void DrawEnemies()
        {
            float time = (float)Raylib.GetTime();
            var lb = _lineBatch;

            // Solid pass — pyramids + health bars
            lb.Begin();
            foreach (var enemy in _enemySpawner.ActiveEnemies)
            {
                if (!enemy.IsAlive) continue;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(enemy.Position.X, enemy.Position.Z)
                    : 0f;

                var pos = new Vector3(enemy.Position.X, enemy.Position.Y + warpY, enemy.Position.Z);
                float scale = enemy.Data.Scale.X;

                var baseColor = UintToColor(enemy.Data.Color);
                Color color;
                float hitElapsed = time - enemy.Health.LastHitTime;
                if (enemy.Health.LastHitTime >= 0f && hitElapsed < 0.1f)
                {
                    float t = hitElapsed / 0.1f;
                    byte lr = (byte)(255 + (baseColor.R - 255) * t);
                    byte lg = (byte)(255 + (baseColor.G - 255) * t);
                    byte lbl = (byte)(255 + (baseColor.B - 255) * t);
                    color = new Color(lr, lg, lbl, (byte)255);
                }
                else
                {
                    color = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)255);
                }

                lb.PyramidThick(pos, scale, color);

                float hpPct = enemy.Health.CurrentHP / enemy.Health.MaxHP;
                float barWidth = scale * 1.2f;
                var barPos = new Vector3(pos.X - barWidth * 0.5f, pos.Y + scale * 0.8f, pos.Z);
                var barEnd = new Vector3(barPos.X + barWidth, barPos.Y, barPos.Z);
                var barFilled = new Vector3(barPos.X + barWidth * hpPct, barPos.Y, barPos.Z);

                lb.Line(barPos, barEnd, new Color(60, 0, 0, 200));
                lb.Line(barPos, barFilled, new Color(255, 50, 50, 255));
            }
            lb.Flush();

            // Additive glow pass
            Raylib.BeginBlendMode(BlendMode.Additive);
            lb.Begin();
            foreach (var enemy in _enemySpawner.ActiveEnemies)
            {
                if (!enemy.IsAlive) continue;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(enemy.Position.X, enemy.Position.Z)
                    : 0f;

                var pos = new Vector3(enemy.Position.X, enemy.Position.Y + warpY, enemy.Position.Z);
                float scale = enemy.Data.Scale.X;
                var baseColor = UintToColor(enemy.Data.Color);

                float glowPulse = 0.85f + 0.15f * MathF.Sin(time * 4f + enemy.EntityId * 1.7f);
                lb.PyramidWires(pos, scale * 1.15f * glowPulse,
                    new Color(baseColor.R, baseColor.G, baseColor.B, (byte)40));
            }
            lb.Flush();
            Raylib.EndBlendMode();
        }

        private void DrawProjectiles()
        {
            float time = (float)Raylib.GetTime();
            int count = _projectiles.Count;

            if (count > _projDrawCache.Length)
                _projDrawCache = new ProjectileDrawData[count * 2];
            int cached = 0;

            for (int i = 0; i < count; i++)
            {
                var proj = _projectiles[i];
                if (proj.IsDestroyed) continue;

                float warpY = _warpManager.Initialized
                    ? _warpManager.GetWarpOffset(proj.Position.X, proj.Position.Z)
                    : 0f;

                var pos = new Vector3(proj.Position.X, proj.Position.Y + warpY, proj.Position.Z);
                var color = GetProjectileColor(proj.Context.Tags);

                float baseDamage = proj.Context.Damage;
                float radius = MathF.Max(0.1f, MathF.Min(0.5f, 0.12f + baseDamage / 20f * 0.3f));

                bool hasElement = proj.Context.Tags != ModTags.None;
                float pulse = hasElement
                    ? 0.9f + 0.1f * MathF.Sin(time * 6f + proj.GetHashCode() * 0.3f)
                    : 1f;
                float r = radius * pulse;

                _projDrawCache[cached++] = new ProjectileDrawData { Position = pos, Color = color, Radius = r };
            }

            DrawBillboardBatch(cached, 1f, 255);

            Raylib.BeginBlendMode(BlendMode.Additive);
            DrawBillboardBatch(cached, 1.6f, 30);
            DrawBillboardBatch(cached, 2.4f, 12);
            Raylib.EndBlendMode();
        }

        private const int DiscSegments = 8;
        private static readonly float[] _discCos = new float[DiscSegments];
        private static readonly float[] _discSin = new float[DiscSegments];

        static GameLoop()
        {
            for (int i = 0; i < DiscSegments; i++)
            {
                float angle = i * MathF.Tau / DiscSegments;
                _discCos[i] = MathF.Cos(angle);
                _discSin[i] = MathF.Sin(angle);
            }
        }

        private void DrawBillboardBatch(int count, float scale, byte alpha)
        {
            if (count == 0) return;

            var cam = _camera.Apply();
            var forward = Vector3.Normalize(cam.Target - cam.Position);
            var right = Vector3.Normalize(Vector3.Cross(forward, cam.Up));
            var up = Vector3.Normalize(Vector3.Cross(right, forward));

            Rlgl.Begin(0x0004); // GL_TRIANGLES
            for (int i = 0; i < count; i++)
            {
                ref var d = ref _projDrawCache[i];
                float r = d.Radius * scale;
                var c = alpha == 255 ? d.Color : new Color(d.Color.R, d.Color.G, d.Color.B, alpha);
                var p = d.Position;

                for (int s = 0; s < DiscSegments; s++)
                {
                    int next = (s + 1) & (DiscSegments - 1);
                    var v1 = p + (right * _discCos[s] + up * _discSin[s]) * r;
                    var v2 = p + (right * _discCos[next] + up * _discSin[next]) * r;

                    Rlgl.Color4ub(c.R, c.G, c.B, c.A);
                    Rlgl.Vertex3f(p.X, p.Y, p.Z);
                    Rlgl.Vertex3f(v1.X, v1.Y, v1.Z);
                    Rlgl.Vertex3f(v2.X, v2.Y, v2.Z);
                }
            }
            Rlgl.End();
        }

        private struct ProjectileDrawData
        {
            public Vector3 Position;
            public Color Color;
            public float Radius;
        }
        private ProjectileDrawData[] _projDrawCache = new ProjectileDrawData[64];

        private void DrawPickups()
        {
            float time = (float)Raylib.GetTime();

            foreach (var pickup in _lootDropper.ActivePickups)
            {
                if (pickup.Collected || pickup.Expired) continue;

                var pos = new Vector3(pickup.Position.X, pickup.Position.Y, pickup.Position.Z);
                float s = pickup.Scale;
                var color = GetRarityColor(pickup.Rarity);

                float bob = MathF.Sin(time * 3f + pos.X * 2f + pos.Z * 3f) * 0.05f;
                var drawPos = new Vector3(pos.X, pos.Y + bob, pos.Z);

                Raylib.DrawCube(drawPos, s, s, s, color);
                Raylib.DrawCubeWires(drawPos, s * 1.1f, s * 1.1f, s * 1.1f, new Color(255, 255, 255, 120));

                Raylib.BeginBlendMode(BlendMode.Additive);
                Raylib.DrawCube(drawPos, s * 1.4f, s * 1.4f, s * 1.4f,
                    new Color(color.R, color.G, color.B, (byte)30));
                Raylib.EndBlendMode();
            }
        }

        private void DrawPlacementPreview()
        {
            if (!_towerPlacement.IsPreviewVisible) return;
            if (_modPanel != null && _modPanel.IsOpen) return;

            var pos = _towerPlacement.PreviewPosition;
            float warpY = _warpManager.Initialized
                ? _warpManager.GetWarpOffset(pos.X, pos.Z)
                : 0f;

            var drawPos = new Vector3(pos.X, 0.5f + warpY, pos.Z);

            var color = _towerPlacement.IsPreviewValid
                ? new Color((byte)0, (byte)255, (byte)100, (byte)80)
                : new Color((byte)255, (byte)50, (byte)50, (byte)80);

            var wireColor = _towerPlacement.IsPreviewValid
                ? new Color((byte)0, (byte)255, (byte)100, (byte)180)
                : new Color((byte)255, (byte)50, (byte)50, (byte)180);

            Raylib.DrawCube(drawPos, 1.6f, 1.0f, 1.6f, color);
            Raylib.DrawCubeWires(drawPos, 1.7f, 1.1f, 1.7f, wireColor);

            float pulse = 0.8f + 0.2f * MathF.Sin((float)Raylib.GetTime() * 4f);
            Raylib.BeginBlendMode(BlendMode.Additive);
            Raylib.DrawCubeWires(drawPos, 1.8f * pulse, 1.2f * pulse, 1.8f * pulse,
                new Color(wireColor.R, wireColor.G, wireColor.B, (byte)35));
            Raylib.EndBlendMode();
        }

        private void DrawHUD()
        {
            var state = _gameManager.CurrentState;

            _hud.Render(state,
                _waveManager.CurrentWave, _waveManager.TotalWaves,
                _gameStats.TotalKills,
                _objective.CurrentHP, _objective.MaxHP,
                _waveManager.EnemiesRemaining,
                _towerPlacement.PlacedTowers.Count, 5);

            if (_hud.WaveStartRequested && state == GameState.Preparing)
            {
                _gameManager.SetState(GameState.Wave);
                _gameStats.SetWave(_waveManager.CurrentWave + 1);
            }

            if (_imguiInitialized)
            {
                float dt = Raylib.GetFrameTime();
                rlImGui.Begin(dt);

                if (_modPanel.IsOpen)
                    _modPanel.Render();

                if (state == GameState.GameOver)
                {
                    _gameOverScreen.Render(_waveManager.CurrentWave, _gameStats.TotalKills);

                    if (_gameOverScreen.RestartRequested)
                        ResetGame();
                }

                rlImGui.End();
            }

            _damageText.Render(_lastCamera);

            Raylib.DrawFPS(Raylib.GetScreenWidth() - 100, 10);
        }

        private static void DrawOctahedronWires(Vector3 center, float radiusH, float radiusV, Color color)
        {
            DrawOctahedronWiresRotated(center, radiusH, radiusV, 0f, color);
        }

        private static void DrawOctahedronWiresRotated(Vector3 center, float radiusH, float radiusV, float angleY, Color color)
        {
            float cos = MathF.Cos(angleY);
            float sin = MathF.Sin(angleY);

            var top = center + new Vector3(0, radiusV, 0);
            var bottom = center - new Vector3(0, radiusV, 0);
            var right = center + new Vector3(radiusH * cos, 0, radiusH * sin);
            var left = center - new Vector3(radiusH * cos, 0, radiusH * sin);
            var front = center + new Vector3(-radiusH * sin, 0, radiusH * cos);
            var back = center - new Vector3(-radiusH * sin, 0, radiusH * cos);

            LineBatch.ThickLine3D(top, right, color);
            LineBatch.ThickLine3D(top, left, color);
            LineBatch.ThickLine3D(top, front, color);
            LineBatch.ThickLine3D(top, back, color);
            LineBatch.ThickLine3D(bottom, right, color);
            LineBatch.ThickLine3D(bottom, left, color);
            LineBatch.ThickLine3D(bottom, front, color);
            LineBatch.ThickLine3D(bottom, back, color);
            LineBatch.ThickLine3D(right, front, color);
            LineBatch.ThickLine3D(front, left, color);
            LineBatch.ThickLine3D(left, back, color);
            LineBatch.ThickLine3D(back, right, color);
        }

        private static Color GetProjectileColor(ModTags tags) => ModTagsUtil.GetColor(tags);

        private static Color GetRarityColor(Rarity rarity) => rarity switch
        {
            Rarity.Common => new Color(200, 200, 200, 255),
            Rarity.Uncommon => new Color(100, 255, 100, 255),
            Rarity.Rare => new Color(100, 150, 255, 255),
            Rarity.Epic => new Color(200, 100, 255, 255),
            _ => new Color(200, 200, 200, 255)
        };

        private static Color UintToColor(uint rgba)
        {
            byte r = (byte)((rgba >> 24) & 0xFF);
            byte g = (byte)((rgba >> 16) & 0xFF);
            byte b = (byte)((rgba >> 8) & 0xFF);
            byte a = (byte)(rgba & 0xFF);
            return new Color(r, g, b, a);
        }
    }
}
