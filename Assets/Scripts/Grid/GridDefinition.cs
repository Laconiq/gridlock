using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AIWE.Grid
{
    [CreateAssetMenu(menuName = "AIWE/Grid Definition")]
    public class GridDefinition : ScriptableObject
    {
        [Header("Grid")]
        [SerializeField] private int width = 20;
        [SerializeField] private int height = 12;
        [SerializeField] private float cellSize = 1f;

        [Header("Cells")]
        [SerializeField] private CellType[] cells;

        [Header("Paths")]
        [SerializeField] private List<PathDefinition> paths = new();

        [Header("Objective")]
        [SerializeField] private float objectiveHP = 100f;

        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;
        public float ObjectiveHP => objectiveHP;
        public IReadOnlyList<PathDefinition> Paths => paths;

        public CellType GetCell(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return CellType.Blocked;
            return cells[y * width + x];
        }

        public void SetCell(int x, int y, CellType type)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return;
            cells[y * width + x] = type;
        }

        [Button("Initialize Grid")]
        private void InitializeGrid()
        {
            cells = new CellType[width * height];
        }

        public List<Vector2Int> GetCellsOfType(CellType type)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (GetCell(x, y) == type)
                        result.Add(new Vector2Int(x, y));
            return result;
        }
    }
}
