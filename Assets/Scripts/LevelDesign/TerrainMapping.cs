using System;
using UnityEngine;

namespace AIWE.LevelDesign
{
    [CreateAssetMenu(menuName = "AIWE/Level Design/Terrain Mapping")]
    public class TerrainMapping : ScriptableObject
    {
        [Serializable]
        public struct TileEntry
        {
            public int intGridValue;
            public string label;
            public Material material;
            public Vector3 scale;
            public float yOffset;
        }

        public float cellSize = 1f;
        public TileEntry[] entries;

        public TileEntry? GetEntry(int value)
        {
            if (entries == null) return null;
            foreach (var entry in entries)
            {
                if (entry.intGridValue == value)
                    return entry;
            }
            return null;
        }
    }
}
