using AIWE.AI;
using AIWE.Core;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] Material pathMaterial;
    [SerializeField] float lineWidth = 0.12f;
    [SerializeField] float dotRadius = 0.2f;

    void Start()
    {
        var routeManager = ServiceLocator.Get<RouteManager>();
        if (routeManager == null) return;

        for (int routeId = 0; routeId < routeManager.RouteCount; routeId++)
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

        for (int i = 0; i < waypoints.Length; i++)
        {
            var pos = waypoints[i];
            pos.y = 0.15f;
            line.SetPosition(i, pos);
            CreateDot(routeObj.transform, pos);
        }
    }

    void CreateDot(Transform parent, Vector3 pos)
    {
        var dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dot.name = "PathDot";
        dot.transform.SetParent(parent);
        dot.transform.position = pos;
        dot.transform.localScale = Vector3.one * dotRadius;
        dot.GetComponent<MeshRenderer>().material = pathMaterial;
        Destroy(dot.GetComponent<Collider>());
    }
}
