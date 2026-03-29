using System.Collections;
using Gridlock.AI;
using Gridlock.Core;
using Gridlock.Grid;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] Material pathMaterial;
    [SerializeField] float lineWidth = 0.12f;
    [SerializeField] float lineY = 0.15f;
    [SerializeField] float dotRadius = 0.2f;

    [Header("Pulse")]
    [SerializeField] private float pulseDuration = 0.96f;
    [SerializeField] private float pulseWidth = 0.15f;
    [SerializeField] private Color pulseColor = new(0.56f, 0.96f, 1f, 1f);
    [SerializeField] private float dotFlashScale = 1.8f;

    [Header("Ambient Pulse")]
    [SerializeField] private float ambientPulseInterval = 4f;
    [SerializeField] private float ambientPulseIntervalVariance = 1.5f;
    [SerializeField] private Color ambientPulseColor = new(0.35f, 0.6f, 0.65f, 0.7f);

    private LineRenderer[] _lines;
    private Vector3[][] _basePositions;
    private Transform[][] _dots;
    private Color _baseLineColor;
    private Coroutine _pulseRoutine;
    private float _nextAmbientPulse;

    void Start()
    {
        var routeManager = ServiceLocator.Get<RouteManager>();
        if (routeManager == null) return;

        int routeCount = routeManager.RouteCount;
        _lines = new LineRenderer[routeCount];
        _basePositions = new Vector3[routeCount][];
        _dots = new Transform[routeCount][];

        for (int routeId = 0; routeId < routeCount; routeId++)
        {
            var waypoints = routeManager.GetRoute(routeId);
            if (waypoints == null || waypoints.Length < 2) continue;
            CreateRouteLine(waypoints, routeId);
        }

        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnStateChanged += OnGameStateChanged;

        ScheduleNextAmbientPulse();
    }

    void Update()
    {
        if (Time.time < _nextAmbientPulse || _pulseRoutine != null) return;
        TriggerPulse(ambientPulseColor);
        ScheduleNextAmbientPulse();
    }

    private void ScheduleNextAmbientPulse()
    {
        _nextAmbientPulse = Time.time + ambientPulseInterval + Random.Range(-ambientPulseIntervalVariance, ambientPulseIntervalVariance);
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
            gm.OnStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState prev, GameState current)
    {
        if (current == GameState.Wave && prev == GameState.Preparing)
            TriggerPulse(pulseColor);
    }

    public void TriggerPulse(Color color)
    {
        if (_pulseRoutine != null)
            StopCoroutine(_pulseRoutine);
        _pulseRoutine = StartCoroutine(PulseRoutine(color));
    }

    void CreateRouteLine(Vector3[] waypoints, int routeId)
    {
        var routeObj = new GameObject($"Route_{routeId}");
        routeObj.transform.SetParent(transform);

        var line = routeObj.AddComponent<LineRenderer>();
        line.material = pathMaterial;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = waypoints.Length;
        line.useWorldSpace = true;
        line.numCornerVertices = 0;
        line.textureMode = LineTextureMode.Tile;

        _lines[routeId] = line;
        _basePositions[routeId] = new Vector3[waypoints.Length];
        _dots[routeId] = new Transform[waypoints.Length];

        for (int i = 0; i < waypoints.Length; i++)
        {
            var pos = waypoints[i];
            pos.y = lineY;
            _basePositions[routeId][i] = pos;
            line.SetPosition(i, pos);
            _dots[routeId][i] = CreateDot(routeObj.transform, pos);
        }
    }

    Transform CreateDot(Transform parent, Vector3 pos)
    {
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "PathDot";
        dot.transform.SetParent(parent);
        dot.transform.position = pos;
        dot.transform.localScale = Vector3.one * dotRadius;
        dot.GetComponent<MeshRenderer>().material = pathMaterial;
        Destroy(dot.GetComponent<Collider>());
        return dot.transform;
    }

    private IEnumerator PulseRoutine(Color color)
    {
        if (_lines == null) yield break;

        float elapsed = 0f;
        float totalT = 1f + pulseWidth;

        while (elapsed < pulseDuration)
        {
            elapsed += Time.deltaTime;
            float t = (elapsed / pulseDuration) * totalT;

            for (int r = 0; r < _lines.Length; r++)
            {
                if (_lines[r] == null) continue;
                _lines[r].colorGradient = BuildPulseGradient(t, color);
                UpdateDotScales(r, t);
            }

            yield return null;
        }

        for (int r = 0; r < _lines.Length; r++)
        {
            if (_lines[r] == null) continue;
            _lines[r].colorGradient = BuildFlatGradient(_lines[r].startColor);
            ResetDotScales(r);
        }

        _pulseRoutine = null;
    }

    private Gradient BuildPulseGradient(float t, Color color)
    {
        var gradient = new Gradient();
        float head = t;
        float tail = t - pulseWidth;

        float k0 = Mathf.Clamp01(tail);
        float k1 = Mathf.Clamp01(Mathf.Lerp(tail, head, 0.5f));
        float k2 = Mathf.Clamp01(head);

        var baseColor = pathMaterial != null ? pathMaterial.color : Color.white;
        var dimColor = baseColor;
        dimColor.a = baseColor.a * 0.4f;

        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(dimColor, 0f),
                new GradientColorKey(dimColor, Mathf.Max(0.001f, k0)),
                new GradientColorKey(color, k1),
                new GradientColorKey(dimColor, Mathf.Min(0.999f, k2)),
                new GradientColorKey(dimColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(dimColor.a, 0f),
                new GradientAlphaKey(dimColor.a, Mathf.Max(0.001f, k0)),
                new GradientAlphaKey(color.a, k1),
                new GradientAlphaKey(dimColor.a, Mathf.Min(0.999f, k2)),
                new GradientAlphaKey(dimColor.a, 1f)
            }
        );

        return gradient;
    }

    private Gradient BuildFlatGradient(Color color)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(color.a, 0f), new GradientAlphaKey(color.a, 1f) }
        );
        return gradient;
    }

    private void UpdateDotScales(int routeId, float t)
    {
        if (_dots[routeId] == null) return;
        int count = _dots[routeId].Length;

        for (int i = 0; i < count; i++)
        {
            if (_dots[routeId][i] == null) continue;
            float pos = (float)i / Mathf.Max(1, count - 1);
            float dist = Mathf.Abs(pos - t + pulseWidth * 0.5f);
            float influence = 1f - Mathf.Clamp01(dist / (pulseWidth * 0.8f));
            float scale = Mathf.Lerp(1f, dotFlashScale, influence);
            _dots[routeId][i].localScale = Vector3.one * dotRadius * scale;
        }
    }

    private void ResetDotScales(int routeId)
    {
        if (_dots[routeId] == null) return;
        foreach (var dot in _dots[routeId])
        {
            if (dot != null)
                dot.localScale = Vector3.one * dotRadius;
        }
    }

    void LateUpdate()
    {
        var warp = GridWarpManager.Instance;
        if (warp == null || _lines == null) return;

        for (int r = 0; r < _lines.Length; r++)
        {
            if (_lines[r] == null || _basePositions[r] == null) continue;

            for (int i = 0; i < _basePositions[r].Length; i++)
            {
                var basePos = _basePositions[r][i];
                float offset = warp.GetWarpOffset(basePos.x, basePos.z);
                var warped = new Vector3(basePos.x, lineY + offset, basePos.z);

                _lines[r].SetPosition(i, warped);

                if (_dots[r][i] != null)
                    _dots[r][i].position = warped;
            }
        }
    }
}
