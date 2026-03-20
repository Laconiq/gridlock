using LDtkUnity;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public class TowerSlotMarker : MonoBehaviour, ILDtkImportedFields
    {
        [SerializeField] private string towerId;
        [SerializeField] private int maxTriggers = 3;
        public string TowerId => towerId;
        public int MaxTriggers => maxTriggers;
        public void OnLDtkImportFields(LDtkFields fields) { towerId = fields.GetString("tower_id"); maxTriggers = fields.GetInt("max_triggers"); }
    }
}
