using System.Collections.Generic;
using Gridlock.Core;
using UnityEngine;

namespace Gridlock.Grid
{
    public class GridManager : MonoBehaviour
    {
        [SerializeField] private GridDefinition gridDefinition;

        private Dictionary<int, Vector3[]> _worldRoutes = new();
        private List<Vector3> _spawnPositions = new();
        private Vector3 _objectivePosition;
        private Vector3 _gridOrigin;
        private CellType[] _runtimeCells;

        public GridDefinition Definition => gridDefinition;
        public Vector3 ObjectivePosition => _objectivePosition;
        public IReadOnlyList<Vector3> SpawnPositions => _spawnPositions;
        public int RouteCount => _worldRoutes.Count;

        private void Awake()
        {
            ServiceLocator.Register(this);
            BuildGrid();
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<GridManager>();
        }

        private void BuildGrid()
        {
            if (gridDefinition == null)
            {
                Debug.LogError("[GridManager] No GridDefinition assigned");
                return;
            }

            _runtimeCells = gridDefinition.CloneCells();

            _gridOrigin = new Vector3(
                -gridDefinition.Width * gridDefinition.CellSize * 0.5f,
                0f,
                -gridDefinition.Height * gridDefinition.CellSize * 0.5f
            );

            var spawnCells = gridDefinition.GetCellsOfType(CellType.Spawn);
            foreach (var cell in spawnCells)
                _spawnPositions.Add(GridToWorld(cell));

            var objectiveCells = gridDefinition.GetCellsOfType(CellType.Objective);
            if (objectiveCells.Count > 0)
                _objectivePosition = GridToWorld(objectiveCells[0]);

            foreach (var path in gridDefinition.Paths)
            {
                var worldPoints = new List<Vector3>();
                foreach (var wp in path.waypoints)
                    worldPoints.Add(GridToWorld(wp));

                worldPoints.Add(_objectivePosition);
                _worldRoutes[path.routeId] = worldPoints.ToArray();
            }

            Debug.Log($"[GridManager] Grid {gridDefinition.Width}x{gridDefinition.Height}, " +
                      $"{_worldRoutes.Count} route(s), {_spawnPositions.Count} spawn(s)");
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                _gridOrigin.x + (gridPos.x + 0.5f) * gridDefinition.CellSize,
                0f,
                _gridOrigin.z + (gridPos.y + 0.5f) * gridDefinition.CellSize
            );
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - _gridOrigin.x) / gridDefinition.CellSize);
            int y = Mathf.FloorToInt((worldPos.z - _gridOrigin.z) / gridDefinition.CellSize);
            return new Vector2Int(
                Mathf.Clamp(x, 0, gridDefinition.Width - 1),
                Mathf.Clamp(y, 0, gridDefinition.Height - 1)
            );
        }

        public bool TryWorldToGrid(Vector3 worldPos, out Vector2Int gridPos)
        {
            int x = Mathf.FloorToInt((worldPos.x - _gridOrigin.x) / gridDefinition.CellSize);
            int y = Mathf.FloorToInt((worldPos.z - _gridOrigin.z) / gridDefinition.CellSize);
            gridPos = new Vector2Int(x, y);
            return x >= 0 && x < gridDefinition.Width && y >= 0 && y < gridDefinition.Height;
        }

        public CellType GetRuntimeCell(int x, int y)
        {
            if (x < 0 || x >= gridDefinition.Width || y < 0 || y >= gridDefinition.Height)
                return CellType.Blocked;
            return _runtimeCells[y * gridDefinition.Width + x];
        }

        public event System.Action<int, int, CellType> OnCellChanged;

        public void SetRuntimeCell(int x, int y, CellType type)
        {
            if (x < 0 || x >= gridDefinition.Width || y < 0 || y >= gridDefinition.Height) return;
            _runtimeCells[y * gridDefinition.Width + x] = type;
            OnCellChanged?.Invoke(x, y, type);
        }

        public CellType GetCellAt(Vector3 worldPos)
        {
            var grid = WorldToGrid(worldPos);
            return GetRuntimeCell(grid.x, grid.y);
        }

        public Vector3[] GetRoute(int routeId)
        {
            return _worldRoutes.TryGetValue(routeId, out var route) ? route : null;
        }

        public int GetNearestWaypointIndex(int routeId, Vector3 position)
        {
            if (!_worldRoutes.TryGetValue(routeId, out var route)) return 0;

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
            if (!_worldRoutes.TryGetValue(routeId, out var route) || route.Length == 0)
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
