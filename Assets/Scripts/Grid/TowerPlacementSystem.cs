using System.Collections;
using System.Collections.Generic;
using Gridlock.Core;
using Gridlock.Interfaces;
using Gridlock.Mods;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Gridlock.Grid
{
    public class TowerPlacementSystem : MonoBehaviour
    {
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private int maxTowers = 5;
        [SerializeField] private LayerMask towerLayerMask = ~0;
        [SerializeField] private Color validPlacementColor = new(0.2f, 1f, 0.5f, 1f);
        [SerializeField] private Color invalidPlacementColor = new(1f, 0.2f, 0.2f, 1f);
        [SerializeField] private ModSlotPreset defaultPreset;

        private GridManager _gridManager;
        private Camera _camera;
        private Controls _controls;
        private GameObject _preview;
        private MeshRenderer _previewRenderer;
        private bool _isActive;
        private Vector2Int _lastGridPos = new(-1, -1);
        private readonly List<GameObject> _placedTowers = new();

        private static readonly Plane GroundPlane = new(Vector3.up, Vector3.zero);
        private readonly List<RaycastResult> _uiRaycastResults = new();

        public int RemainingTowers => maxTowers - _placedTowers.Count;

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _camera = Camera.main;

            _controls = new Controls();
            _controls.Player.Enable();

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged += OnGameStateChanged;

            CreatePreview();
        }

        private void OnDestroy()
        {
            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnGameStateChanged;

            _controls?.Dispose();

            if (_preview != null)
                Destroy(_preview);
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (prev == GameState.GameOver && current == GameState.Preparing)
                _placedTowers.Clear();

            _isActive = current == GameState.Preparing;
            if (_preview != null)
                _preview.SetActive(_isActive && RemainingTowers > 0);
        }

        private void CreatePreview()
        {
            _preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _preview.name = "TowerPreview";
            _preview.transform.localScale = new Vector3(1.5f, 0.6f, 1.5f);
            Destroy(_preview.GetComponent<Collider>());
            _previewRenderer = _preview.GetComponent<MeshRenderer>();
            _preview.SetActive(false);
        }

        private void LateUpdate()
        {
            if (!_isActive || _gridManager == null || _camera == null) return;

            var pointerPos = _controls.Player.PointerPosition.ReadValue<Vector2>();
            var ray = _camera.ScreenPointToRay(pointerPos);

            if (_controls.Player.Interact.WasPressedThisFrame() && !IsPointerOverUI(pointerPos))
            {
                if (!TryClickTower(ray))
                    TryPlaceTower(ray);
            }

            if (RemainingTowers > 0)
                UpdatePreview(ray);
            else if (_preview.activeSelf)
                _preview.SetActive(false);
        }

        private bool TryClickTower(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hit, 200f, towerLayerMask)) return false;

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null || !interactable.CanInteract()) return false;

            interactable.Interact();
            return true;
        }

        private void UpdatePreview(Ray ray)
        {
            if (!GroundPlane.Raycast(ray, out float distance))
            {
                _preview.SetActive(false);
                return;
            }

            var worldPos = ray.GetPoint(distance);

            if (!_gridManager.TryWorldToGrid(worldPos, out var gridPos))
            {
                _preview.SetActive(false);
                _lastGridPos = new Vector2Int(-1, -1);
                return;
            }

            if (gridPos == _lastGridPos) return;
            _lastGridPos = gridPos;

            bool valid = CanPlaceAt(gridPos);

            _preview.SetActive(true);
            var snapPos = _gridManager.GridToWorld(gridPos);
            snapPos.y = 0.3f;
            _preview.transform.position = snapPos;

            _previewRenderer.material.color = valid ? validPlacementColor : invalidPlacementColor;
        }

        private void TryPlaceTower(Ray ray)
        {
            if (RemainingTowers <= 0) return;
            if (towerPrefab == null) return;

            if (!GroundPlane.Raycast(ray, out float distance)) return;

            var worldPos = ray.GetPoint(distance);

            if (!_gridManager.TryWorldToGrid(worldPos, out var gridPos)) return;
            if (!CanPlaceAt(gridPos)) return;

            var snapPos = _gridManager.GridToWorld(gridPos);
            var tower = Instantiate(towerPrefab, snapPos, Quaternion.identity);
            _placedTowers.Add(tower);
            tower.AddComponent<Visual.WarpFollower>();
            Visual.TowerVisualSetup.Apply(tower);
            StartCoroutine(TowerPopIn(tower.transform));

            _gridManager.SetRuntimeCell(gridPos.x, gridPos.y, CellType.Blocked);

            var juice = Visual.GameJuice.Instance;
            if (juice != null)
                juice.OnTowerPlaced(snapPos);

            if (defaultPreset != null)
            {
                var executor = tower.GetComponent<ModSlotExecutor>();
                if (executor != null)
                    executor.ApplyPreset(defaultPreset);
            }

            if (RemainingTowers <= 0)
                _preview.SetActive(false);
        }

        private bool IsPointerOverUI(Vector2 pointerPos)
        {
            if (EventSystem.current == null) return false;

            var pointerData = new PointerEventData(EventSystem.current) { position = pointerPos };
            _uiRaycastResults.Clear();
            EventSystem.current.RaycastAll(pointerData, _uiRaycastResults);
            return _uiRaycastResults.Count > 0;
        }

        private bool CanPlaceAt(Vector2Int gridPos)
        {
            var cell = _gridManager.GetRuntimeCell(gridPos.x, gridPos.y);
            return cell == CellType.Empty || cell == CellType.TowerSlot;
        }

        private static System.Collections.IEnumerator TowerPopIn(Transform t)
        {
            var targetScale = t.localScale;
            t.localScale = Vector3.zero;
            float elapsed = 0f;
            const float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / duration;
                float overshoot = 1f + Mathf.Sin(p * Mathf.PI) * 0.3f;
                t.localScale = targetScale * Mathf.Min(overshoot, 1f + (1f - p) * 0.3f);
                yield return null;
            }

            t.localScale = targetScale;
        }

    }
}
