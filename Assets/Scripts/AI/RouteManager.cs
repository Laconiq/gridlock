using AIWE.Core;
using AIWE.Grid;
using UnityEngine;

namespace AIWE.AI
{
    public class RouteManager : MonoBehaviour
    {
        private GridManager _gridManager;

        private void Awake()
        {
            ServiceLocator.Register(this);
        }

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<RouteManager>();
        }

        public Vector3[] GetRoute(int routeId)
        {
            return _gridManager != null ? _gridManager.GetRoute(routeId) : null;
        }

        public int RouteCount => _gridManager != null ? _gridManager.RouteCount : 0;

        public int GetNearestWaypointIndex(int routeId, Vector3 position)
        {
            return _gridManager != null ? _gridManager.GetNearestWaypointIndex(routeId, position) : 0;
        }

        public float GetDistanceToRoute(int routeId, Vector3 position)
        {
            return _gridManager != null ? _gridManager.GetDistanceToRoute(routeId, position) : float.MaxValue;
        }
    }
}
