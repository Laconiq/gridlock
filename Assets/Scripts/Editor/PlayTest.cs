#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Gridlock.Core;
using Gridlock.Enemies;
using Gridlock.Grid;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Gridlock.Towers;
using Gridlock.Visual;
using UnityEditor;
using UnityEngine;

public static class PlayTest
{
    private static string[] _slots1, _slots2, _slots3;

    [MenuItem("Gridlock/Play Test (Default Configs)")]
    public static void Execute()
    {
        ExecuteWithSlots(null, null, null);
    }

    public static void ExecuteWithArgs(string json)
    {
        string[] Parse(string key)
        {
            int idx = json?.IndexOf($"\"{key}\"") ?? -1;
            if (idx < 0) return null;
            int start = json.IndexOf('[', idx);
            int end = json.IndexOf(']', start);
            if (start < 0 || end < 0) return null;
            var inner = json.Substring(start + 1, end - start - 1);
            return inner.Split(',')
                .Select(s => s.Trim().Trim('"'))
                .Where(s => s.Length > 0)
                .ToArray();
        }

        ExecuteWithSlots(Parse("slots1"), Parse("slots2"), Parse("slots3"));
    }

    private static void ExecuteWithSlots(string[] s1, string[] s2, string[] s3)
    {
        _slots1 = s1 ?? new[] { "Pierce", "OnHit", "Homing" };
        _slots2 = s2 ?? new[] { "Split", "Burn" };
        _slots3 = s3 ?? new[] { "Heavy", "OnHit", "Split", "Burn" };

        if (!Application.isPlaying)
        {
            EditorApplication.EnterPlaymode();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return;
        }

        RunTest();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += () => EditorApplication.delayCall += RunTest;
        }
    }

    private static void RunTest()
    {
        Debug.Log("[PlayTest] === STARTING PIPELINE TEST ===");

        var placementSystem = Object.FindAnyObjectByType<TowerPlacementSystem>();
        var gridManager = ServiceLocator.Get<GridManager>();
        if (placementSystem == null || gridManager == null)
        {
            Debug.LogError("[PlayTest] Missing TowerPlacementSystem or GridManager");
            return;
        }

        var prefabField = typeof(TowerPlacementSystem).GetField("towerPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var towerPrefab = prefabField?.GetValue(placementSystem) as GameObject;
        if (towerPrefab == null)
        {
            Debug.LogError("[PlayTest] Tower prefab is null");
            return;
        }

        var cells = FindTowerCells(gridManager);
        if (cells.Count == 0)
        {
            Debug.LogError("[PlayTest] No valid tower cells found");
            return;
        }

        var configs = new[] { _slots1, _slots2, _slots3 };
        for (int i = 0; i < Mathf.Min(configs.Length, cells.Count); i++)
        {
            if (configs[i] == null || configs[i].Length == 0) continue;
            var tower = PlaceTower(towerPrefab, gridManager, cells[i]);
            if (tower == null) continue;

            var executor = tower.GetComponent<ModSlotExecutor>();
            var slots = configs[i].Select(name =>
            {
                if (System.Enum.TryParse<ModType>(name, true, out var mt))
                    return new ModSlotData { modType = mt };
                Debug.LogWarning($"[PlayTest] Unknown mod type: {name}");
                return null;
            }).Where(s => s != null).ToList();

            executor.SetSlots(slots);
            executor.TargetingMode = TargetingMode.First;
            var wp = gridManager.GridToWorld(cells[i]);
            Debug.Log($"[PlayTest] Tower {i + 1} at ({cells[i].x},{cells[i].y}) world=({wp.x:F0},{wp.z:F0}): [{string.Join(", ", configs[i])}]");
        }

        GameManager.Instance?.SetState(GameState.Wave);
        var monitor = new GameObject("_PlayTestMonitor").AddComponent<PlayTestMonitor>();
        Debug.Log("[PlayTest] Wave started — monitoring for 30s");
    }

    private static List<Vector2Int> FindTowerCells(GridManager gridManager)
    {
        var empty = new List<Vector2Int>();
        var path = new List<Vector2Int>();
        var spawns = new List<Vector2Int>();

        for (int y = 0; y < 14; y++)
        {
            for (int x = 0; x < 24; x++)
            {
                var cell = gridManager.GetRuntimeCell(x, y);
                if (cell == CellType.Empty || cell == CellType.TowerSlot) empty.Add(new Vector2Int(x, y));
                if (cell == CellType.Path) path.Add(new Vector2Int(x, y));
                if (cell == CellType.Spawn) spawns.Add(new Vector2Int(x, y));
            }
        }

        var spawn0 = spawns.Count > 0 ? spawns[0] : Vector2Int.zero;
        var adjacent = empty
            .Where(e => path.Any(p => Mathf.Abs(p.x - e.x) + Mathf.Abs(p.y - e.y) == 1))
            .OrderBy(e => (e - spawn0).sqrMagnitude)
            .ToList();

        var selected = new List<Vector2Int>();
        foreach (var c in adjacent)
        {
            if (selected.All(s => Mathf.Abs(s.x - c.x) + Mathf.Abs(s.y - c.y) >= 2))
                selected.Add(c);
            if (selected.Count >= 3) break;
        }

        return selected;
    }

    private static GameObject PlaceTower(GameObject prefab, GridManager gridManager, Vector2Int gridPos)
    {
        var snapPos = gridManager.GridToWorld(gridPos);
        var tower = Object.Instantiate(prefab, snapPos, Quaternion.identity);
        tower.AddComponent<WarpFollower>();
        TowerVisualSetup.Apply(tower);
        gridManager.SetRuntimeCell(gridPos.x, gridPos.y, CellType.Blocked);
        return tower;
    }
}

public class PlayTestMonitor : MonoBehaviour
{
    private float _elapsed;
    private float _nextLog;

    private void Update()
    {
        _elapsed += Time.deltaTime;
        if (_elapsed < _nextLog) return;
        _nextLog = _elapsed + 0.5f;

        var projectiles = FindObjectsByType<ModProjectile>(FindObjectsInactive.Exclude);
        var enemies = EnemyRegistry.Count;
        var executors = FindObjectsByType<ModSlotExecutor>(FindObjectsInactive.Exclude);

        string towerInfo = "";
        foreach (var ex in executors)
        {
            float nearest = float.MaxValue;
            foreach (var entry in EnemyRegistry.All)
            {
                if (!entry.Controller.IsAlive) continue;
                float d = (entry.Controller.Position - ex.transform.position).magnitude;
                if (d < nearest) nearest = d;
            }
            string dist = nearest < float.MaxValue / 2 ? $"{nearest:F1}" : "none";
            towerInfo += $" T(slots={ex.ModSlots.Count},enemy={dist})";
        }

        Debug.Log($"[PlayTest] t={_elapsed:F1}s Proj:{projectiles.Length} Enemies:{enemies}{towerInfo}");

        if (enemies == 0 && _elapsed > 3f)
        {
            Debug.Log("[PlayTest] === ALL ENEMIES DEAD — TEST COMPLETE ===");
            Destroy(gameObject);
        }

        if (_elapsed >= 30f)
        {
            Debug.Log("[PlayTest] === TIMEOUT ===");
            Destroy(gameObject);
        }
    }
}
#endif
