#if UNITY_EDITOR
using AIWE.Grid;
using UnityEditor;
using UnityEngine;

public static class CreateTestGrid
{
    [MenuItem("AIWE/Create Test Grid Level")]
    public static void Create()
    {
        int width = 24;
        int height = 14;
        float cellSize = 2f;
        int total = width * height;

        var grid = ScriptableObject.CreateInstance<GridDefinition>();
        var so = new SerializedObject(grid);
        so.FindProperty("width").intValue = width;
        so.FindProperty("height").intValue = height;
        so.FindProperty("cellSize").floatValue = cellSize;
        so.FindProperty("objectiveHP").floatValue = 100f;

        var cellsProp = so.FindProperty("cells");
        cellsProp.arraySize = total;
        for (int i = 0; i < total; i++)
            cellsProp.GetArrayElementAtIndex(i).enumValueIndex = 0;

        // S-shaped path across the grid
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
                cellsProp.GetArrayElementAtIndex(idx).enumValueIndex = 1; // Path
        }

        // Spawn
        cellsProp.GetArrayElementAtIndex(7 * width + 0).enumValueIndex = 4;

        // Objective
        cellsProp.GetArrayElementAtIndex(4 * width + 23).enumValueIndex = 5;

        // Tower slots flanking the path
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
                cellsProp.GetArrayElementAtIndex(idx).enumValueIndex = 2;
        }

        // Add connecting path cells for the turns
        int[] turnX = { 16, 16 };
        int[] turnY = { 5, 6 };
        for (int i = 0; i < turnX.Length; i++)
        {
            int idx = turnY[i] * width + turnX[i];
            if (idx < total)
                cellsProp.GetArrayElementAtIndex(idx).enumValueIndex = 1;
        }

        // Also add spawn-to-first-waypoint path cell
        cellsProp.GetArrayElementAtIndex(7 * width + 0).enumValueIndex = 4; // keep spawn

        // Path definition with all waypoints in order
        var allX = new System.Collections.Generic.List<int>(pathX);
        var allY = new System.Collections.Generic.List<int>(pathY);
        // Insert turn cells
        allX.InsertRange(allX.Count - 7, new[] { 16, 16 });
        allY.InsertRange(allY.Count - 7, new[] { 6, 5 });

        var pathsProp = so.FindProperty("paths");
        pathsProp.arraySize = 1;
        var pathDef = pathsProp.GetArrayElementAtIndex(0);
        pathDef.FindPropertyRelative("routeId").intValue = 0;
        var waypoints = pathDef.FindPropertyRelative("waypoints");
        waypoints.arraySize = allX.Count;
        for (int i = 0; i < allX.Count; i++)
        {
            var wp = waypoints.GetArrayElementAtIndex(i);
            wp.FindPropertyRelative("x").intValue = allX[i];
            wp.FindPropertyRelative("y").intValue = allY[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();

        string dir = "Assets/Data/Levels";
        if (!AssetDatabase.IsValidFolder(dir))
            AssetDatabase.CreateFolder("Assets/Data", "Levels");

        string path = $"{dir}/TestGrid.asset";
        var existing = AssetDatabase.LoadAssetAtPath<GridDefinition>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(grid, path);
        AssetDatabase.SaveAssets();
        Debug.Log($"[CreateTestGrid] Created {width}x{height} grid (cellSize={cellSize}) → {path}");
        Selection.activeObject = grid;
    }
}
#endif
