using System.Collections.Generic;
using System.Linq;
using AIWE.Core;
using AIWE.LevelDesign;
using UnityEngine;

namespace AIWE.AI
{
    public class RouteManager : MonoBehaviour
    {
        private Dictionary<int, Vector3[]> _routes = new();

        private void Awake()
        {
            ServiceLocator.Register(this);
            BuildRoutes();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<RouteManager>();
        }

        private void BuildRoutes()
        {
            var markers = FindObjectsByType<EnemyPathMarker>();
            var grouped = markers.GroupBy(m => m.RouteId);

            foreach (var group in grouped)
            {
                var waypoints = group
                    .OrderBy(m => m.Order)
                    .Select(m => m.transform.position)
                    .ToList();

                _routes[group.Key] = waypoints.ToArray();
            }

            var objective = FindAnyObjectByType<ObjectiveMarker>();
            if (objective != null)
            {
                var keys = _routes.Keys.ToArray();
                foreach (var key in keys)
                {
                    var list = _routes[key].ToList();
                    list.Add(objective.transform.position);
                    _routes[key] = list.ToArray();
                }
            }

            Debug.Log($"[RouteManager] Built {_routes.Count} route(s). " +
                      string.Join(", ", _routes.Select(r => $"Route {r.Key}: {r.Value.Length} waypoints")));
        }

        public Vector3[] GetRoute(int routeId)
        {
            return _routes.TryGetValue(routeId, out var route) ? route : null;
        }

        public int RouteCount => _routes.Count;

        public int GetNearestWaypointIndex(int routeId, Vector3 position)
        {
            if (!_routes.TryGetValue(routeId, out var route)) return 0;

            int nearest = 0;
            float minDist = float.MaxValue;

            for (int i = 0; i < route.Length; i++)
            {
                float dist = Vector3.Distance(position, route[i]);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = i;
                }
            }

            return nearest;
        }

        public float GetDistanceToRoute(int routeId, Vector3 position)
        {
            if (!_routes.TryGetValue(routeId, out var route) || route.Length == 0)
                return float.MaxValue;

            float minDist = float.MaxValue;

            for (int i = 0; i < route.Length - 1; i++)
            {
                float dist = DistanceToSegment(position, route[i], route[i + 1]);
                if (dist < minDist)
                    minDist = dist;
            }

            return minDist;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            var ab = b - a;
            var ap = point - a;
            float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Vector3.Dot(ab, ab));
            var closest = a + ab * t;
            return Vector3.Distance(point, closest);
        }
    }
}
