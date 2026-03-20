using LDtkUnity;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public class ObjectiveMarker : MonoBehaviour, ILDtkImportedFields
    {
        [SerializeField] private int health = 100;
        public int Health => health;
        public void OnLDtkImportFields(LDtkFields fields) { health = fields.GetInt("health"); }
    }
}
