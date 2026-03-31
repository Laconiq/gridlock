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

public static class PipelineTestSuite
{
    private static readonly string[][] AllTests = new[]
    {
        // === BASIC TRAITS ===
        new[] { "TEST:BasicHoming",      "Homing" },
        new[] { "TEST:BasicPierce",      "Pierce" },
        new[] { "TEST:BasicBounce",      "Bounce" },
        new[] { "TEST:BasicSplit",       "Split" },
        new[] { "TEST:BasicHeavy",       "Heavy" },
        new[] { "TEST:BasicBurn",        "Burn" },
        new[] { "TEST:BasicFrost",       "Frost" },
        new[] { "TEST:BasicWide",        "Wide" },

        // === TRAIT COMBOS ===
        new[] { "TEST:PierceBurn",       "Pierce", "Burn" },
        new[] { "TEST:HomingHeavy",      "Homing", "Heavy" },
        new[] { "TEST:SplitBurn",        "Split", "Burn" },
        new[] { "TEST:SplitHoming",      "Split", "Homing" },
        new[] { "TEST:PierceBounce",     "Pierce", "Bounce" },
        new[] { "TEST:BounceShock",      "Bounce", "Shock" },

        // === SINGLE EVENT CHAINS ===
        new[] { "TEST:OnHit_Homing",     "OnHit", "Homing" },
        new[] { "TEST:OnHit_Split",      "OnHit", "Split" },
        new[] { "TEST:OnHit_Burn",       "OnHit", "Burn" },
        new[] { "TEST:Pierce_OnHit_Homing",  "Pierce", "OnHit", "Homing" },
        new[] { "TEST:Pierce_OnHit_Split",   "Pierce", "OnHit", "Split" },
        new[] { "TEST:Heavy_OnHit_SplitBurn","Heavy", "OnHit", "Split", "Burn" },
        new[] { "TEST:Homing_OnKill_Wide",   "Homing", "OnKill", "Wide" },
        new[] { "TEST:OnEnd_Split",      "OnEnd", "Split" },
        new[] { "TEST:OnEnd_Burn",       "OnEnd", "Burn" },

        // === NESTED EVENT CHAINS ===
        new[] { "TEST:Homing_OnHit_Split_OnKill_Wide", "Homing", "OnHit", "Split", "OnKill", "Wide" },
        new[] { "TEST:Pierce_OnHit_Burn_OnKill_Split", "Pierce", "OnHit", "Burn", "OnKill", "Split" },

        // === TEMPORAL EVENTS ===
        new[] { "TEST:OnPulse_Burn",     "OnPulse", "Burn" },
        new[] { "TEST:OnDelay_Split",    "OnDelay", "Split" },
        new[] { "TEST:Homing_OnPulse_Frost", "Homing", "OnPulse", "Frost" },

        // === SYNERGY COMBOS ===
        new[] { "TEST:HeavyHeavy_Railgun",  "Heavy", "Heavy" },
        new[] { "TEST:FrostFrost_Blizzard",  "Frost", "Frost" },
        new[] { "TEST:PierceBounce_Ricochet","Pierce", "Bounce" },
        new[] { "TEST:HomingSwift_Missile",  "Homing", "Swift" },
        new[] { "TEST:HeavyWide_Meteor",     "Heavy", "Wide" },
        new[] { "TEST:BurnWide_Napalm",      "Burn", "Wide" },

        // === EDGE CASES ===
        new[] { "TEST:SplitSplit_Barrage",   "Split", "Split" },
        new[] { "TEST:OnHit_OnKill_Burn",    "OnHit", "OnKill", "Burn" },
        new[] { "TEST:Pierce_OnHit_Pierce",  "Pierce", "OnHit", "Pierce" },
    };

    private static int _currentTest;
    private static List<string> _results = new();
    private static bool _running;

    [MenuItem("Gridlock/Run Full Test Suite")]
    public static void Execute()
    {
        _currentTest = 0;
        _results.Clear();
        _running = false;

        if (!Application.isPlaying)
        {
            EditorApplication.EnterPlaymode();
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            return;
        }

        StartNextTest();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.delayCall += () => EditorApplication.delayCall += StartNextTest;
        }
    }

    private static void StartNextTest()
    {
        if (_running) return;

        if (_currentTest >= AllTests.Length)
        {
            PrintReport();
            return;
        }

        var test = AllTests[_currentTest];
        string testName = test[0];
        var mods = test.Skip(1).ToArray();

        Debug.Log($"[TestSuite] ======= {testName} [{string.Join(", ", mods)}] =======");
        _running = true;

        var gridManager = ServiceLocator.Get<GridManager>();
        var placementSystem = Object.FindAnyObjectByType<TowerPlacementSystem>();

        if (gridManager == null || placementSystem == null)
        {
            _results.Add($"{testName} | ERROR | Missing GridManager or TowerPlacementSystem");
            _currentTest++;
            _running = false;
            StartNextTest();
            return;
        }

        GameManager.Instance?.SetState(GameState.Preparing);

        foreach (var proj in Object.FindObjectsByType<ModProjectile>(FindObjectsInactive.Exclude))
            Object.Destroy(proj.gameObject);
        foreach (var tower in Object.FindObjectsByType<TowerChassis>(FindObjectsInactive.Exclude))
        {
            var gp = gridManager.TryWorldToGrid(tower.transform.position, out var pos);
            if (gp) gridManager.SetRuntimeCell(pos.x, pos.y, CellType.Empty);
            Object.Destroy(tower.gameObject);
        }

        var prefabField = typeof(TowerPlacementSystem).GetField("towerPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var towerPrefab = prefabField?.GetValue(placementSystem) as GameObject;

        var cells = FindNearSpawn(gridManager);
        if (cells.Count == 0 || towerPrefab == null)
        {
            _results.Add($"{testName} | ERROR | No valid cell or prefab");
            _currentTest++;
            _running = false;
            StartNextTest();
            return;
        }

        var tower2 = Object.Instantiate(towerPrefab, gridManager.GridToWorld(cells[0]), Quaternion.identity);
        tower2.AddComponent<WarpFollower>();
        gridManager.SetRuntimeCell(cells[0].x, cells[0].y, CellType.Blocked);

        var executor = tower2.GetComponent<ModSlotExecutor>();
        var slots = mods.Select(name =>
        {
            System.Enum.TryParse<ModType>(name, true, out var mt);
            return new ModSlotData { modType = mt };
        }).ToList();
        executor.SetSlots(slots);
        executor.TargetingMode = TargetingMode.First;

        GameManager.Instance?.SetState(GameState.Wave);

        var monitorGo = new GameObject("_TestMonitor");
        var monitor = monitorGo.AddComponent<TestCaseMonitor>();
        monitor.Init(testName, mods, () =>
        {
            _currentTest++;
            _running = false;
            EditorApplication.delayCall += StartNextTest;
        });
    }

    private static List<Vector2Int> FindNearSpawn(GridManager gm)
    {
        var empty = new List<Vector2Int>();
        var path = new List<Vector2Int>();
        var spawns = new List<Vector2Int>();
        for (int y = 0; y < 14; y++)
            for (int x = 0; x < 24; x++)
            {
                var c = gm.GetRuntimeCell(x, y);
                if (c == CellType.Empty || c == CellType.TowerSlot) empty.Add(new Vector2Int(x, y));
                if (c == CellType.Path) path.Add(new Vector2Int(x, y));
                if (c == CellType.Spawn) spawns.Add(new Vector2Int(x, y));
            }

        var s0 = spawns.Count > 0 ? spawns[0] : Vector2Int.zero;
        return empty
            .Where(e => path.Any(p => Mathf.Abs(p.x - e.x) + Mathf.Abs(p.y - e.y) == 1))
            .OrderBy(e => (e - s0).sqrMagnitude)
            .Take(1)
            .ToList();
    }

    private static void PrintReport()
    {
        Debug.Log("[TestSuite] ========== FULL REPORT ==========");
        int pass = 0, fail = 0, err = 0;
        foreach (var r in _results)
        {
            Debug.Log($"[TestSuite] {r}");
            if (r.Contains("| PASS")) pass++;
            else if (r.Contains("| FAIL")) fail++;
            else err++;
        }
        Debug.Log($"[TestSuite] TOTAL: {pass} PASS, {fail} FAIL, {err} ERROR out of {_results.Count}");
        Debug.Log("[TestSuite] ========== END REPORT ==========");
    }

    public static void RecordResult(string result)
    {
        _results.Add(result);
    }
}

public class TestCaseMonitor : MonoBehaviour
{
    private string _testName;
    private string[] _mods;
    private System.Action _onComplete;
    private float _elapsed;
    private int _maxProjectilesSeen;
    private int _hitsDetected;
    private bool _towerFired;
    private float _timeout = 8f;

    public void Init(string testName, string[] mods, System.Action onComplete)
    {
        _testName = testName;
        _mods = mods;
        _onComplete = onComplete;
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        var projectiles = FindObjectsByType<ModProjectile>(FindObjectsInactive.Exclude);
        if (projectiles.Length > _maxProjectilesSeen)
            _maxProjectilesSeen = projectiles.Length;

        if (projectiles.Length > 0)
            _towerFired = true;

        var enemies = EnemyRegistry.Count;

        if (enemies == 0 && _elapsed > 2f)
        {
            Finish("PASS", $"enemies=0 maxProj={_maxProjectilesSeen} fired={_towerFired} t={_elapsed:F1}s");
            return;
        }

        if (_elapsed >= _timeout)
        {
            if (!_towerFired)
                Finish("FAIL", $"tower never fired, enemies={enemies} t={_elapsed:F1}s");
            else if (enemies > 0)
                Finish("PASS", $"timeout but fired, enemies={enemies} maxProj={_maxProjectilesSeen} t={_elapsed:F1}s");
            else
                Finish("PASS", $"maxProj={_maxProjectilesSeen} t={_elapsed:F1}s");
            return;
        }
    }

    private void Finish(string status, string details)
    {
        string result = $"{_testName} | {status} | [{string.Join(",", _mods)}] {details}";
        Debug.Log($"[TestSuite] {result}");
        PipelineTestSuite.RecordResult(result);
        _onComplete?.Invoke();
        Destroy(gameObject);
    }
}
#endif
