using System.Collections.Generic;
using AIWE.Core;
using AIWE.Interfaces;
using AIWE.NodeEditor.Data;
using AIWE.Towers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace AIWE.Grid
{
    public class TowerPlacementSystem : MonoBehaviour
    {
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private int maxTowers = 5;

        private GridManager _gridManager;
        private Camera _camera;
        private Controls _controls;
        private GameObject _preview;
        private MeshRenderer _previewRenderer;
        private bool _isActive;
        private Vector2Int _lastGridPos = new(-1, -1);
        private readonly List<GameObject> _placedTowers = new();

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

        private void Update()
        {
            if (!_isActive || _gridManager == null || _camera == null) return;

            var pointerPos = _controls.Player.PointerPosition.ReadValue<Vector2>();
            var ray = _camera.ScreenPointToRay(pointerPos);

            if (_controls.Player.Interact.WasPressedThisFrame() && !IsPointerOverUI())
            {
                if (TryClickTower(ray))
                    return;

                TryPlaceTower(ray);
            }

            if (RemainingTowers > 0)
                UpdatePreview(ray);
            else if (_preview.activeSelf)
                _preview.SetActive(false);
        }

        private bool TryClickTower(Ray ray)
        {
            if (!Physics.Raycast(ray, out var hit, 200f)) return false;

            var interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable == null) return false;
            if (!interactable.CanInteract()) return false;

            interactable.Interact();
            return true;
        }

        private void UpdatePreview(Ray ray)
        {
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float distance)) return;

            var worldPos = ray.GetPoint(distance);
            var gridPos = _gridManager.WorldToGrid(worldPos);

            if (gridPos == _lastGridPos) return;
            _lastGridPos = gridPos;

            bool valid = CanPlaceAt(gridPos);

            _preview.SetActive(true);
            var snapPos = _gridManager.GridToWorld(gridPos);
            snapPos.y = 0.3f;
            _preview.transform.position = snapPos;

            var color = valid ? new Color(0.2f, 1f, 0.5f, 1f) : new Color(1f, 0.2f, 0.2f, 1f);
            _previewRenderer.material.color = color;
        }

        private void TryPlaceTower(Ray ray)
        {
            if (RemainingTowers <= 0) return;
            if (towerPrefab == null) return;

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float distance)) return;

            var worldPos = ray.GetPoint(distance);
            var gridPos = _gridManager.WorldToGrid(worldPos);

            if (!CanPlaceAt(gridPos)) return;

            var snapPos = _gridManager.GridToWorld(gridPos);
            var tower = Instantiate(towerPrefab, snapPos, Quaternion.identity);
            _placedTowers.Add(tower);

            _gridManager.Definition.SetCell(gridPos.x, gridPos.y, CellType.Blocked);

            var chassis = tower.GetComponent<TowerChassis>();
            if (chassis != null)
                chassis.SetNodeGraph(CreateDefaultGraph());

            if (RemainingTowers <= 0)
                _preview.SetActive(false);
        }

        private bool IsPointerOverUI()
        {
            var pointer = _controls.Player.PointerPosition.ReadValue<Vector2>();
            var pointerData = new PointerEventData(EventSystem.current) { position = pointer };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);
            return results.Count > 0;
        }

        private bool CanPlaceAt(Vector2Int gridPos)
        {
            var cell = _gridManager.Definition.GetCell(gridPos.x, gridPos.y);
            return cell == CellType.Empty || cell == CellType.TowerSlot;
        }

        private static NodeGraphData CreateDefaultGraph()
        {
            var graph = new NodeGraphData();

            var trigger = new NodeData
            {
                moduleDefId = "on_timer",
                category = ModuleCategory.Trigger,
                editorPosition = new Vector2(100, 200)
            };

            var zone = new NodeData
            {
                moduleDefId = "nearest_enemy",
                category = ModuleCategory.Zone,
                editorPosition = new Vector2(350, 200)
            };

            var effect = new NodeData
            {
                moduleDefId = "projectile",
                category = ModuleCategory.Effect,
                editorPosition = new Vector2(600, 200)
            };

            graph.nodes.Add(trigger);
            graph.nodes.Add(zone);
            graph.nodes.Add(effect);

            graph.connections.Add(new ConnectionData
            {
                fromNodeId = trigger.nodeId,
                toNodeId = zone.nodeId,
                fromPort = 0,
                toPort = 0
            });

            graph.connections.Add(new ConnectionData
            {
                fromNodeId = zone.nodeId,
                toNodeId = effect.nodeId,
                fromPort = 0,
                toPort = 0
            });

            return graph;
        }
    }
}
