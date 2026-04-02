using System;
using System.Collections.Generic;
using System.Numerics;
using Gridlock.Core;
using Gridlock.Grid;
using Gridlock.Mods;

namespace Gridlock.Towers
{
    public sealed class TowerPlacement
    {
        private readonly GridManager _gridManager;
        private readonly TowerData _defaultTowerData;
        private readonly ModSlotPreset? _defaultPreset;
        private readonly int _maxTowers;
        private readonly List<Tower> _placedTowers = new();
        private bool _isActive;

        private Vector2Int _lastPreviewGridPos = new(-1, -1);
        private bool _previewValid;
        private Vector3 _previewWorldPos;
        private bool _previewVisible;

        public int RemainingTowers => _maxTowers - _placedTowers.Count;
        public IReadOnlyList<Tower> PlacedTowers => _placedTowers;
        public bool IsPreviewVisible => _previewVisible && _isActive && RemainingTowers > 0;
        public bool IsPreviewValid => _previewValid;
        public Vector3 PreviewPosition => _previewWorldPos;

        public event Action<Tower>? OnTowerPlaced;

        public TowerPlacement(GridManager gridManager, TowerData defaultTowerData,
            ModSlotPreset? defaultPreset = null, int maxTowers = 5)
        {
            _gridManager = gridManager;
            _defaultTowerData = defaultTowerData;
            _defaultPreset = defaultPreset;
            _maxTowers = maxTowers;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.OnStateChanged += OnGameStateChanged;
                _isActive = gm.CurrentState == GameState.Preparing;
            }
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (prev == GameState.GameOver && current == GameState.Preparing)
                _placedTowers.Clear();

            _isActive = current == GameState.Preparing;
        }

        public void UpdatePreview(Vector3 worldPos)
        {
            if (!_isActive || RemainingTowers <= 0)
            {
                _previewVisible = false;
                return;
            }

            if (!_gridManager.TryWorldToGrid(worldPos, out var gridPos))
            {
                _previewVisible = false;
                _lastPreviewGridPos = new Vector2Int(-1, -1);
                return;
            }

            if (gridPos == _lastPreviewGridPos) return;
            _lastPreviewGridPos = gridPos;

            _previewValid = CanPlaceAt(gridPos);
            _previewWorldPos = _gridManager.GridToWorld(gridPos);
            _previewWorldPos.Y = 0.3f;
            _previewVisible = true;
        }

        public Tower? TryPlace(Vector3 worldPos, bool isOverUI)
        {
            if (!_isActive || isOverUI) return null;
            if (RemainingTowers <= 0) return null;

            if (!_gridManager.TryWorldToGrid(worldPos, out var gridPos)) return null;
            if (!CanPlaceAt(gridPos)) return null;

            var snapPos = _gridManager.GridToWorld(gridPos);
            var tower = new Tower(_defaultTowerData, snapPos);
            _placedTowers.Add(tower);

            _gridManager.SetRuntimeCell(gridPos.X, gridPos.Y, CellType.Blocked);

            if (_defaultPreset != null)
                tower.Executor.ApplyPreset(_defaultPreset);

            OnTowerPlaced?.Invoke(tower);
            return tower;
        }

        public Tower? TryClickTower(Vector3 worldPos, float clickRadius = 1.5f)
        {
            float bestDistSq = clickRadius * clickRadius;
            Tower? best = null;

            foreach (var tower in _placedTowers)
            {
                float distSq = Vector3.DistanceSquared(tower.Position, worldPos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    best = tower;
                }
            }

            return best;
        }

        private bool CanPlaceAt(Vector2Int gridPos)
        {
            var cell = _gridManager.GetRuntimeCell(gridPos.X, gridPos.Y);
            return cell == CellType.Empty || cell == CellType.TowerSlot;
        }

        public void Update(float dt)
        {
            foreach (var tower in _placedTowers)
                tower.Update(dt);
        }

        public void Shutdown()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnGameStateChanged;
        }
    }
}
