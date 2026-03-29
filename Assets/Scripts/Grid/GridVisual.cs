using Gridlock.Core;
using UnityEngine;

namespace Gridlock.Grid
{
    public class GridVisual : MonoBehaviour
    {
        [SerializeField] private Material gridMaterial;
        [SerializeField] private float verticesPerUnit = 1.5f;

        [Header("Cell Colors (RGB = line tint, A = tint strength)")]
        [SerializeField] private Color pathColor = new(1f, 0f, 0.7f, 0.8f);
        [SerializeField] private Color blockedColor = new(1f, 0.2f, 0.2f, 0.9f);
        [SerializeField] private Color spawnColor = new(1f, 0f, 0.7f, 1f);
        [SerializeField] private Color objectiveColor = new(0f, 1f, 0.8f, 1f);

        private Texture2D _cellMap;
        private Material _instanceMaterial;
        private GridManager _gridManager;
        private bool _cellMapDirty;

        private static readonly int CellMapId = Shader.PropertyToID("_CellMap");
        private static readonly int GridOriginId = Shader.PropertyToID("_GridOrigin");
        private static readonly int GridExtentId = Shader.PropertyToID("_GridExtent");
        private static readonly int GridSizeId = Shader.PropertyToID("_GridSize");
        private static readonly int CellFillId = Shader.PropertyToID("_CellFill");

        private void Start()
        {
            _gridManager = ServiceLocator.Get<GridManager>();
            if (_gridManager == null || _gridManager.Definition == null) return;

            var def = _gridManager.Definition;
            float w = def.Width * def.CellSize;
            float h = def.Height * def.CellSize;

            int resX = Mathf.CeilToInt(w * verticesPerUnit);
            int resZ = Mathf.CeilToInt(h * verticesPerUnit);

            var gridObj = new GameObject("GridPlane");
            gridObj.transform.SetParent(transform);
            gridObj.transform.position = new Vector3(0f, -0.01f, 0f);

            var mf = gridObj.AddComponent<MeshFilter>();
            var mesh = CreateSubdividedPlane(w, h, resX, resZ);
            mf.mesh = mesh;

            var mr = gridObj.AddComponent<MeshRenderer>();
            if (gridMaterial != null)
            {
                mr.material = gridMaterial;
                _instanceMaterial = mr.material;

                _instanceMaterial.SetFloat(GridSizeId, def.CellSize);
                _instanceMaterial.SetVector(GridOriginId, new Vector4(-w * 0.5f, -h * 0.5f, 0, 0));
                _instanceMaterial.SetVector(GridExtentId, new Vector4(w, h, 0, 0));
                _instanceMaterial.SetFloat(CellFillId, 0.03f);

                BuildCellMap();
                _instanceMaterial.SetTexture(CellMapId, _cellMap);
            }

            // Initialize warp physics
            var warp = GridWarpManager.Instance;
            if (warp != null)
                warp.Initialize(mesh, resX, resZ, w, h);

            _gridManager.OnCellChanged += OnCellChanged;
        }

        private void OnDestroy()
        {
            if (_gridManager != null)
                _gridManager.OnCellChanged -= OnCellChanged;

            if (_cellMap != null)
                Destroy(_cellMap);
        }

        private void BuildCellMap()
        {
            var def = _gridManager.Definition;
            _cellMap = new Texture2D(def.Width, def.Height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            int coloredCount = 0;
            for (int y = 0; y < def.Height; y++)
            for (int x = 0; x < def.Width; x++)
            {
                var cell = _gridManager.GetRuntimeCell(x, y);
                var color = GetCellColor(cell);
                _cellMap.SetPixel(x, y, color);
                if (color.a > 0f) coloredCount++;
            }

            _cellMap.Apply();
            Debug.Log($"[GridVisual] Cell map: {coloredCount} colored / {def.Width * def.Height} total");
        }

        private void OnCellChanged(int x, int y, CellType type)
        {
            if (_cellMap == null) return;
            _cellMap.SetPixel(x, y, GetCellColor(type));
            _cellMapDirty = true;
        }

        private void LateUpdate()
        {
            if (_cellMapDirty && _cellMap != null)
            {
                _cellMap.Apply();
                _cellMapDirty = false;
            }
        }

        private Color GetCellColor(CellType type) => type switch
        {
            CellType.Path => pathColor,
            CellType.Blocked => blockedColor,
            CellType.Spawn => spawnColor,
            CellType.Objective => objectiveColor,
            _ => Color.clear
        };

        private static Mesh CreateSubdividedPlane(float width, float height, int resX, int resZ)
        {
            var mesh = new Mesh { name = "GridPlane" };

            int vx = resX + 1;
            int vz = resZ + 1;
            int vertCount = vx * vz;
            var vertices = new Vector3[vertCount];
            var colors = new Color[vertCount];

            for (int z = 0; z <= resZ; z++)
            for (int x = 0; x <= resX; x++)
            {
                int i = z * vx + x;
                vertices[i] = new Vector3(
                    ((float)x / resX - 0.5f) * width,
                    0f,
                    ((float)z / resZ - 0.5f) * height
                );
                colors[i] = Color.clear;
            }

            var triangles = new int[resX * resZ * 6];
            int t = 0;
            for (int z = 0; z < resZ; z++)
            for (int x = 0; x < resX; x++)
            {
                int bl = z * vx + x;
                int br = bl + 1;
                int tl = bl + vx;
                int tr = tl + 1;
                triangles[t++] = bl;
                triangles[t++] = tl;
                triangles[t++] = br;
                triangles[t++] = br;
                triangles[t++] = tl;
                triangles[t++] = tr;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
