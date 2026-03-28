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

    private LineRenderer[] _lines;
    private Vector3[][] _basePositions;
    private Transform[][] _dots;

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
