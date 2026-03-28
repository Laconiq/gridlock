using AIWE.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace AIWE.Grid
{
    public class TowerPlacementSystem : MonoBehaviour
    {
        [SerializeField] private GameObject towerPrefab;
        [SerializeField] private Material previewValidMaterial;
        [SerializeField] private Material previewInvalidMaterial;

        private GridManager _gridManager;
        private Camera _camera;
        private GameObject _preview;
        private MeshRenderer _previewRenderer;
        private bool _isActive;
        private Vector2Int _lastGridPos = new(-1, -1);

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            _camera = Camera.main;

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

            if (_preview != null)
                Destroy(_preview);
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            _isActive = current == GameState.Preparing;
            if (_preview != null) _preview.SetActive(false);
        }

        private void CreatePreview()
        {
            _preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _preview.name = "TowerPreview";
            _preview.transform.localScale = new Vector3(1.6f, 0.15f, 1.6f);
            Destroy(_preview.GetComponent<Collider>());
            _previewRenderer = _preview.GetComponent<MeshRenderer>();
            _preview.SetActive(false);
        }

        private void Update()
        {
            if (!_isActive || _gridManager == null || _camera == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            var mousePos = mouse.position.ReadValue();
            var ray = _camera.ScreenPointToRay(mousePos);

            var plane = new Plane(Vector3.up, Vector3.zero);
            if (!plane.Raycast(ray, out float distance)) return;

            var worldPos = ray.GetPoint(distance);
            var gridPos = _gridManager.WorldToGrid(worldPos);

            if (gridPos != _lastGridPos)
            {
                _lastGridPos = gridPos;
                UpdatePreview(gridPos);
            }

            if (mouse.leftButton.wasPressedThisFrame)
                TryPlaceTower(gridPos);
        }

        private void UpdatePreview(Vector2Int gridPos)
        {
            var cellType = _gridManager.Definition.GetCell(gridPos.x, gridPos.y);
            bool valid = cellType == CellType.TowerSlot;

            _preview.SetActive(true);
            var worldPos = _gridManager.GridToWorld(gridPos);
            worldPos.y = 0.08f;
            _preview.transform.position = worldPos;

            var mat = valid ? previewValidMaterial : previewInvalidMaterial;
            if (mat != null) _previewRenderer.material = mat;
        }

        private void TryPlaceTower(Vector2Int gridPos)
        {
            var cellType = _gridManager.Definition.GetCell(gridPos.x, gridPos.y);
            if (cellType != CellType.TowerSlot) return;

            if (towerPrefab == null) return;

            var worldPos = _gridManager.GridToWorld(gridPos);
            var tower = Instantiate(towerPrefab, worldPos, Quaternion.identity);

            _gridManager.Definition.SetCell(gridPos.x, gridPos.y, CellType.Blocked);
        }
    }
}
