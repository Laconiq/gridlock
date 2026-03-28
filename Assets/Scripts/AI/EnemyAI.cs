using Gridlock.Core;
using Gridlock.Enemies;
using UnityEngine;

namespace Gridlock.AI
{
    [RequireComponent(typeof(EnemyController))]
    public class EnemyAI : MonoBehaviour
    {
        [SerializeField] private float waypointReachDistance = 0.3f;

        private EnemyController _controller;
        private RouteManager _routeManager;
        private Vector3[] _route;
        private int _routeId;
        private int _currentWaypointIndex;
        private bool _initialized;

        public float RouteProgress
        {
            get
            {
                if (_route == null || _route.Length == 0) return 0f;
                if (_currentWaypointIndex >= _route.Length) return _route.Length;

                float baseProg = _currentWaypointIndex;
                if (_currentWaypointIndex < _route.Length)
                {
                    float segDist = Vector3.Distance(
                        _currentWaypointIndex > 0 ? _route[_currentWaypointIndex - 1] : transform.position,
                        _route[_currentWaypointIndex]);
                    if (segDist > 0.01f)
                    {
                        float toDest = Vector3.Distance(transform.position, _route[_currentWaypointIndex]);
                        baseProg += 1f - Mathf.Clamp01(toDest / segDist);
                    }
                }

                return baseProg;
            }
        }

        private void Awake()
        {
            _controller = GetComponent<EnemyController>();
        }

        public void Setup(int routeId)
        {
            _routeId = routeId;

            _routeManager = ServiceLocator.Get<RouteManager>();
            if (_routeManager != null)
            {
                _route = _routeManager.GetRoute(routeId);
                _currentWaypointIndex = _routeManager.GetNearestWaypointIndex(routeId, transform.position);

                if (_route != null && _currentWaypointIndex < _route.Length)
                {
                    float distToNearest = Vector3.Distance(transform.position, _route[_currentWaypointIndex]);
                    if (distToNearest < waypointReachDistance && _currentWaypointIndex < _route.Length - 1)
                        _currentWaypointIndex++;
                }

                _controller.AssignRoute(_route, _currentWaypointIndex);
            }

            _controller.SetAIState(EnemyAIState.FollowRoute);
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized || !_controller.IsAlive) return;
            _currentWaypointIndex = _controller.RouteIndex;
        }
    }
}
