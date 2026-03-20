using AIWE.LevelDesign;
using LDtkUnity;
using LDtkUnity.Editor;
using UnityEditor;
using UnityEngine;

namespace AIWE.LevelDesign.EditorScripts
{
    public class LDtkLevelPostprocessor : LDtkPostprocessor
    {
        private const string TerrainLayerName = "Terrain";
        private const string TerrainMappingPath = "Assets/Data/LevelDesign/TerrainMapping.asset";

        protected override void OnPostprocessProject(GameObject root)
        {
            var mapping = AssetDatabase.LoadAssetAtPath<TerrainMapping>(TerrainMappingPath);

            // Flatten entire LDtk hierarchy — zero positions and rotations
            // so we can reposition everything ourselves in XZ.
            FlattenHierarchy(root);

            foreach (var level in root.GetComponentsInChildren<LDtkComponentLevel>())
            {
                if (mapping != null)
                    BuildTerrain(level, mapping);

                SwizzleEntities(level);
                CleanUpVisuals(level);
            }
        }

        private void FlattenHierarchy(GameObject root)
        {
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = Quaternion.identity;

            foreach (Transform child in root.transform)
            {
                child.localPosition = Vector3.zero;
                child.localRotation = Quaternion.identity;
            }
            foreach (var level in root.GetComponentsInChildren<LDtkComponentLevel>())
            {
                level.transform.localPosition = Vector3.zero;
                level.transform.localRotation = Quaternion.identity;
            }
            foreach (var layer in root.GetComponentsInChildren<LDtkComponentLayer>())
            {
                layer.transform.localPosition = Vector3.zero;
                layer.transform.localRotation = Quaternion.identity;
            }
        }

        private void BuildTerrain(LDtkComponentLevel level, TerrainMapping mapping)
        {
            var existing = level.transform.Find("Environment_3D");
            if (existing != null)
                Object.DestroyImmediate(existing.gameObject);

            var envParent = new GameObject("Environment_3D");
            envParent.transform.SetParent(level.transform, false);

            foreach (var layer in level.GetComponentsInChildren<LDtkComponentLayer>())
            {
                if (layer.Identifier != TerrainLayerName) continue;

                var intGrid = layer.IntGrid;
                if (intGrid == null) continue;

                int gridW = layer.CSize.x;
                int gridH = layer.CSize.y;
                int count = 0;

                for (int gy = 0; gy < gridH; gy++)
                {
                    for (int gx = 0; gx < gridW; gx++)
                    {
                        int value = intGrid.GetValue(new Vector3Int(gx, gy, 0));
                        if (value <= 0) continue;

                        var entry = mapping.GetEntry(value);
                        if (entry == null) continue;

                        float posX = (gx + 0.5f) * mapping.cellSize;
                        float posZ = (gy + 0.5f) * mapping.cellSize;

                        if (value == 3)
                        {
                            BuildRamp(envParent.transform, posX, posZ, mapping.cellSize, entry.Value.material, gx, gy, intGrid, gridW, gridH);
                            count++;
                        }
                        else
                        {
                            float posY = entry.Value.yOffset + entry.Value.scale.y * 0.5f;
                            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            block.transform.SetParent(envParent.transform, false);
                            block.transform.localScale = entry.Value.scale;
                            block.transform.localPosition = new Vector3(posX, posY, posZ);
                            block.name = $"{entry.Value.label}_{gx}_{gy}";

                            if (entry.Value.material != null)
                            {
                                var renderer = block.GetComponent<Renderer>();
                                if (renderer != null)
                                    renderer.sharedMaterial = entry.Value.material;
                            }

                            block.isStatic = true;
                            count++;
                        }
                    }
                }

                Debug.Log($"[LDtkPostprocessor] Built {count} blocks for '{level.gameObject.name}' ({gridW}x{gridH})");
            }
        }

        private int GetVal(LDtkComponentLayerIntGridValues intGrid, int x, int y, int w, int h)
        {
            if (x < 0 || x >= w || y < 0 || y >= h) return 0;
            return intGrid.GetValue(new Vector3Int(x, y, 0));
        }

        private void BuildRamp(Transform parent, float startX, float startZ, float cellSize,
            Material mat, int gx, int gy, LDtkComponentLayerIntGridValues intGrid, int gridW, int gridH)
        {
            // Find direction toward HighGround (value 5)
            int dirX = 0, dirY = 0;
            if (GetVal(intGrid, gx, gy + 1, gridW, gridH) == 5) dirY =  1;
            if (GetVal(intGrid, gx, gy - 1, gridW, gridH) == 5) dirY = -1;
            if (GetVal(intGrid, gx + 1, gy, gridW, gridH) == 5) dirX =  1;
            if (GetVal(intGrid, gx - 1, gy, gridW, gridH) == 5) dirX = -1;

            // Also check if a neighbor ramp leads to highground (chain detection)
            if (dirX == 0 && dirY == 0)
            {
                // Check if adjacent ramp knows the direction
                if (GetVal(intGrid, gx, gy + 1, gridW, gridH) == 3 && GetVal(intGrid, gx, gy + 2, gridW, gridH) == 5) dirY =  1;
                if (GetVal(intGrid, gx, gy - 1, gridW, gridH) == 3 && GetVal(intGrid, gx, gy - 2, gridW, gridH) == 5) dirY = -1;
                if (GetVal(intGrid, gx + 1, gy, gridW, gridH) == 3 && GetVal(intGrid, gx + 2, gy, gridW, gridH) == 5) dirX =  1;
                if (GetVal(intGrid, gx - 1, gy, gridW, gridH) == 3 && GetVal(intGrid, gx - 2, gy, gridW, gridH) == 5) dirX = -1;
            }

            if (dirX == 0 && dirY == 0) dirY = 1; // fallback

            // Check if this is the START of the ramp chain (the cell farthest from highground).
            // If the cell BEHIND us (opposite direction) is also a ramp, skip — that cell will build us.
            int behindX = gx - dirX;
            int behindY = gy - dirY;
            if (GetVal(intGrid, behindX, behindY, gridW, gridH) == 3)
                return; // Not the start of the chain, skip

            // Count consecutive ramp cells in the direction toward highground
            int chainLength = 1;
            int cx = gx + dirX, cy = gy + dirY;
            while (GetVal(intGrid, cx, cy, gridW, gridH) == 3)
            {
                chainLength++;
                cx += dirX;
                cy += dirY;
            }

            // Build steps across the entire chain
            float totalLength = chainLength * cellSize;
            int totalSteps = chainLength * 4;
            float highgroundHeight = 2f;
            float stepSize = totalLength / totalSteps;

            for (int i = 0; i < totalSteps; i++)
            {
                float stepHeight = (highgroundHeight / totalSteps) * (i + 1);
                float t = (i + 0.5f) * stepSize; // distance from start of chain

                // Start position = center of first ramp cell, shifted to its edge
                float edgeX = startX - dirX * cellSize * 0.5f;
                float edgeZ = startZ - dirY * cellSize * 0.5f;

                float stepX = edgeX + dirX * t;
                float stepZ = edgeZ + dirY * t;

                float scaleX = (dirX != 0) ? stepSize : cellSize;
                float scaleZ = (dirY != 0) ? stepSize : cellSize;

                var step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                step.transform.SetParent(parent, false);
                step.transform.localScale = new Vector3(scaleX, stepHeight, scaleZ);
                step.transform.localPosition = new Vector3(stepX, stepHeight * 0.5f, stepZ);
                step.name = $"Ramp_{gx}_{gy}_step{i}";

                if (mat != null)
                {
                    var renderer = step.GetComponent<Renderer>();
                    if (renderer != null)
                        renderer.sharedMaterial = mat;
                }
                step.isStatic = true;
            }
        }

        private void SwizzleEntities(LDtkComponentLevel level)
        {
            int count = 0;
            foreach (var entity in level.GetComponentsInChildren<LDtkComponentEntity>())
            {
                // Entities were placed by LDtkToUnity in 2D: (x, y, 0).
                // We've flattened the hierarchy, so localPosition = the 2D position.
                // Transpose: X→X, Y→Z, set Y to floor height.
                // Snap to grid center (nearest integer + 0.5) for clean alignment.
                var pos = entity.transform.localPosition;
                float snappedX = Mathf.Floor(pos.x) + 0.5f;
                float snappedZ = Mathf.Floor(pos.y) + 0.5f;
                entity.transform.localPosition = new Vector3(snappedX, 0.5f, snappedZ);
                count++;
            }

            if (count > 0)
                Debug.Log($"[LDtkPostprocessor] Swizzled {count} entities XY→XZ");
        }

        private void CleanUpVisuals(LDtkComponentLevel level)
        {
            for (int i = level.transform.childCount - 1; i >= 0; i--)
            {
                var child = level.transform.GetChild(i);
                if (child.name.EndsWith("_BgColor"))
                    Object.DestroyImmediate(child.gameObject);
            }

            foreach (var tr in level.GetComponentsInChildren<UnityEngine.Tilemaps.TilemapRenderer>(true))
                tr.enabled = false;
            foreach (var sr in level.GetComponentsInChildren<SpriteRenderer>(true))
                sr.enabled = false;
        }
    }
}
