using LDtkUnity;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public class EnemyPathMarker : MonoBehaviour, ILDtkImportedFields
    {
        [SerializeField] private int pathIndex;
        public int PathIndex => pathIndex;
        public void OnLDtkImportFields(LDtkFields fields) { pathIndex = fields.GetInt("path_index"); }
    }
}
