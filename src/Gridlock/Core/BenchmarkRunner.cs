using System.Collections.Generic;
using System.Numerics;
using Gridlock.Grid;
using Gridlock.Mods;
using Gridlock.Towers;

namespace Gridlock.Core
{
    public static class BenchmarkRunner
    {
        public static void Setup(
            GridManager gridManager,
            TowerPlacement towerPlacement,
            GameManager gameManager,
            GameStats gameStats)
        {
            var def = gridManager.Definition;
            var spawnPositions = gridManager.SpawnPositions;
            Vector3 spawnCenter = spawnPositions.Count > 0 ? spawnPositions[0] : Vector3.Zero;

            var candidates = new List<(int x, int y, float dist)>();
            for (int y = 0; y < def.Height; y++)
                for (int x = 0; x < def.Width; x++)
                {
                    if (gridManager.GetRuntimeCell(x, y) != CellType.TowerSlot) continue;

                    var worldPos = gridManager.GridToWorld(new Vector2Int(x, y));
                    float dist = Vector3.DistanceSquared(worldPos, spawnCenter);
                    candidates.Add((x, y, dist));
                }

            candidates.Sort((a, b) => a.dist.CompareTo(b.dist));

            var benchPreset = new ModSlotPreset
            {
                TargetingMode = TargetingMode.First,
                Slots = new List<ModType> { ModType.Heavy, ModType.Split, ModType.Swift, ModType.Pierce }
            };

            int placed = 0;
            foreach (var (cx, cy, _) in candidates)
            {
                if (placed >= 8) break;
                var worldPos = gridManager.GridToWorld(new Vector2Int(cx, cy));
                var tower = towerPlacement.TryPlace(worldPos, isOverUI: false);
                if (tower != null)
                {
                    tower.Executor.ApplyPreset(benchPreset);
                    placed++;
                }
            }

            gameManager.SetState(GameState.Wave);
            gameStats.SetWave(1);
        }
    }
}
