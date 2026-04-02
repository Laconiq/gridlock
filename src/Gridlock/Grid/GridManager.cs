using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Core;

namespace Gridlock.Grid
{
    public sealed class GridManager
    {
        private readonly GridDefinition _definition;
        private readonly Dictionary<int, Vector3[]> _worldRoutes = new();
        private readonly List<Vector3> _spawnPositions = new();
        private Vector3 _objectivePosition;
        private Vector3 _gridOrigin;
        private CellType[]? _runtimeCells;

        public GridDefinition Definition => _definition;
        public Vector3 ObjectivePosition => _objectivePosition;
        public IReadOnlyList<Vector3> SpawnPositions => _spawnPositions;
        public int RouteCount => _worldRoutes.Count;
        public Vector3 GridOrigin => _gridOrigin;

        public event Action<int, int, CellType>? OnCellChanged;

        public GridManager(GridDefinition definition)
        {
            _definition = definition;
        }

        public void Init()
        {
            ServiceLocator.Register(this);
            BuildGrid();
        }

        public void ResetCells()
        {
            _runtimeCells = _definition.CloneCells();
        }

        public void Shutdown()
        {
            ServiceLocator.Unregister<GridManager>();
        }

        private void BuildGrid()
        {
            _runtimeCells = _definition.CloneCells();

            _gridOrigin = new Vector3(
                -_definition.Width * _definition.CellSize * 0.5f,
                0f,
                -_definition.Height * _definition.CellSize * 0.5f
            );

            var spawnCells = _definition.GetCellsOfType(CellType.Spawn);
            foreach (var cell in spawnCells)
                _spawnPositions.Add(GridToWorld(cell));

            var objectiveCells = _definition.GetCellsOfType(CellType.Objective);
            if (objectiveCells.Count > 0)
                _objectivePosition = GridToWorld(objectiveCells[0]);

            foreach (var path in _definition.Paths)
            {
                var worldPoints = new List<Vector3>();
                foreach (var wp in path.Waypoints)
                    worldPoints.Add(GridToWorld(wp));

                worldPoints.Add(_objectivePosition);
                _worldRoutes[path.RouteId] = worldPoints.ToArray();
            }

            Console.WriteLine($"[GridManager] Grid {_definition.Width}x{_definition.Height}, " +
                              $"{_worldRoutes.Count} route(s), {_spawnPositions.Count} spawn(s)");
        }

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                _gridOrigin.X + (gridPos.X + 0.5f) * _definition.CellSize,
                0f,
                _gridOrigin.Z + (gridPos.Y + 0.5f) * _definition.CellSize
            );
        }

        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            int x = (int)MathF.Floor((worldPos.X - _gridOrigin.X) / _definition.CellSize);
            int y = (int)MathF.Floor((worldPos.Z - _gridOrigin.Z) / _definition.CellSize);
            return new Vector2Int(
                Math.Clamp(x, 0, _definition.Width - 1),
                Math.Clamp(y, 0, _definition.Height - 1)
            );
        }

        public bool TryWorldToGrid(Vector3 worldPos, out Vector2Int gridPos)
        {
            int x = (int)MathF.Floor((worldPos.X - _gridOrigin.X) / _definition.CellSize);
            int y = (int)MathF.Floor((worldPos.Z - _gridOrigin.Z) / _definition.CellSize);
            gridPos = new Vector2Int(x, y);
            return x >= 0 && x < _definition.Width && y >= 0 && y < _definition.Height;
        }

        public CellType GetRuntimeCell(int x, int y)
        {
            if (_runtimeCells == null || x < 0 || x >= _definition.Width || y < 0 || y >= _definition.Height)
                return CellType.Blocked;
            return _runtimeCells[y * _definition.Width + x];
        }

        public void SetRuntimeCell(int x, int y, CellType type)
        {
            if (x < 0 || x >= _definition.Width || y < 0 || y >= _definition.Height) return;
            _runtimeCells![y * _definition.Width + x] = type;
            OnCellChanged?.Invoke(x, y, type);
        }

        public CellType GetCellAt(Vector3 worldPos)
        {
            var grid = WorldToGrid(worldPos);
            return GetRuntimeCell(grid.X, grid.Y);
        }

        public Vector3[]? GetRoute(int routeId)
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
            float denom = Vector3.Dot(ab, ab);
            if (denom < 0.0001f) return Vector3.Distance(point, a);
            float t = Math.Clamp(Vector3.Dot(ap, ab) / denom, 0f, 1f);
            var closest = a + ab * t;
            return Vector3.Distance(point, closest);
        }
    }
}
