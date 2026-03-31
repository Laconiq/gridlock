#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Gridlock.Core;
using Gridlock.Grid;
using Gridlock.Mods;
using Gridlock.Visual;
using UnityEditor;
using UnityEngine;

public static class QuickShot
{
    public static void Execute()
    {
        if (!Application.isPlaying)
        {
            EditorApplication.EnterPlaymode();
            EditorApplication.playModeStateChanged += s =>
            {
                if (s == PlayModeStateChange.EnteredPlayMode)
                    EditorApplication.delayCall += () => EditorApplication.delayCall += Run;
            };
            return;
        }
        Run();
    }

    static void Run()
    {
        var ps = Object.FindFirstObjectByType<TowerPlacementSystem>();
        var gm = ServiceLocator.Get<GridManager>();
        var prefab = typeof(TowerPlacementSystem)
            .GetField("towerPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(ps) as GameObject;

        var spawns = new List<Vector2Int>();
        var paths = new List<Vector2Int>();
        var empty = new List<Vector2Int>();
        for (int y = 0; y < 14; y++)
            for (int x = 0; x < 24; x++)
            {
                var c = gm.GetRuntimeCell(x, y);
                if (c == CellType.Spawn) spawns.Add(new Vector2Int(x, y));
                if (c == CellType.Path) paths.Add(new Vector2Int(x, y));
                if (c == CellType.Empty || c == CellType.TowerSlot) empty.Add(new Vector2Int(x, y));
            }

        var s0 = spawns.Count > 0 ? spawns[0] : Vector2Int.zero;
        var near = empty
            .Where(e => paths.Any(p => Mathf.Abs(p.x - e.x) + Mathf.Abs(p.y - e.y) == 1))
            .OrderBy(e => (e - s0).sqrMagnitude)
            .First();

        var t = Object.Instantiate(prefab, gm.GridToWorld(near), Quaternion.identity);
        t.AddComponent<WarpFollower>();
        gm.SetRuntimeCell(near.x, near.y, CellType.Blocked);
        t.GetComponent<ModSlotExecutor>().SetSlots(new List<ModSlotData>
        {
            new() { modType = ModType.Burn },
            new() { modType = ModType.Frost },
            new() { modType = ModType.Pierce }
        });

        GameManager.Instance?.SetState(GameState.Wave);

        var mon = new GameObject("_Mon").AddComponent<ShotMon>();
    }
}

public class ShotMon : MonoBehaviour
{
    float _t; int _n;
    void Update()
    {
        _t += Time.deltaTime;
        if (_t > 0.8f && _n == 0) { ScreenCapture.CaptureScreenshot("Screenshots/neon_v2_a.png"); _n++; }
        if (_t > 1.5f && _n == 1) { ScreenCapture.CaptureScreenshot("Screenshots/neon_v2_b.png"); _n++; }
        if (_t > 2.5f && _n == 2) { ScreenCapture.CaptureScreenshot("Screenshots/neon_v2_c.png"); _n++; Destroy(gameObject); }
    }
}
#endif
