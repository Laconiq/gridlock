using System.Collections.Generic;

namespace Gridlock.Grid
{
    public sealed class GridDefinition
    {
        public int Width { get; set; } = 20;
        public int Height { get; set; } = 12;
        public float CellSize { get; set; } = 1f;
        public CellType[] Cells { get; set; } = System.Array.Empty<CellType>();
        public List<PathDefinition> Paths { get; set; } = new();
        public float ObjectiveHP { get; set; } = 100f;

        public CellType GetCell(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return CellType.Blocked;
            return Cells[y * Width + x];
        }

        public void SetCell(int x, int y, CellType type)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            Cells[y * Width + x] = type;
        }

        public CellType[] CloneCells()
        {
            if (Cells == null || Cells.Length == 0) return new CellType[Width * Height];
            return (CellType[])Cells.Clone();
        }

        public List<Vector2Int> GetCellsOfType(CellType type)
        {
            var result = new List<Vector2Int>();
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    if (GetCell(x, y) == type)
                        result.Add(new Vector2Int(x, y));
            return result;
        }

        public static GridDefinition CreateTestGrid()
        {
            int width = 24;
            int height = 14;
            float cellSize = 2f;
            int total = width * height;

            var grid = new GridDefinition
            {
                Width = width,
                Height = height,
                CellSize = cellSize,
                ObjectiveHP = 100f,
                Cells = new CellType[total]
            };

            int[] pathX = {
                1, 2, 3, 4, 5, 6, 7, 8,
                8, 8, 8,
                8, 9, 10, 11, 12, 13, 14, 15, 16,
                16, 16, 16,
                16, 17, 18, 19, 20, 21, 22
            };
            int[] pathY = {
                7, 7, 7, 7, 7, 7, 7, 7,
                8, 9, 10,
                10, 10, 10, 10, 10, 10, 10, 10, 10,
                9, 8, 7,
                4, 4, 4, 4, 4, 4, 4
            };

            for (int i = 0; i < pathX.Length; i++)
            {
                int idx = pathY[i] * width + pathX[i];
                if (idx < total)
                    grid.Cells[idx] = CellType.Path;
            }

            // Turn cells connecting the two horizontal segments
            int[] turnX = { 16, 16 };
            int[] turnY = { 5, 6 };
            for (int i = 0; i < turnX.Length; i++)
            {
                int idx = turnY[i] * width + turnX[i];
                if (idx < total)
                    grid.Cells[idx] = CellType.Path;
            }

            grid.Cells[7 * width + 0] = CellType.Spawn;
            grid.Cells[4 * width + 23] = CellType.Objective;

            int[,] slots = {
                {3, 5}, {3, 9}, {6, 5}, {6, 9},
                {10, 8}, {10, 12}, {13, 8}, {13, 12},
                {18, 2}, {18, 6}, {21, 2}, {21, 6},
                {8, 5}, {16, 12}, {20, 8}, {11, 5}
            };

            for (int i = 0; i < slots.GetLength(0); i++)
            {
                int x = slots[i, 0];
                int y = slots[i, 1];
                int idx = y * width + x;
                if (idx < total)
                    grid.Cells[idx] = CellType.TowerSlot;
            }

            // Build path waypoints including turn cells
            var allX = new List<int>(pathX);
            var allY = new List<int>(pathY);
            allX.InsertRange(allX.Count - 7, new[] { 16, 16 });
            allY.InsertRange(allY.Count - 7, new[] { 6, 5 });

            var path = new PathDefinition { RouteId = 0 };
            for (int i = 0; i < allX.Count; i++)
                path.Waypoints.Add(new Vector2Int(allX[i], allY[i]));

            grid.Paths.Add(path);

            return grid;
        }
    }
}
