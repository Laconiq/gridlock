using LDtkUnity;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public class EnemyPathMarker : MonoBehaviour, ILDtkImportedFields
    {
        [SerializeField] private int routeId;
        [SerializeField] private int order;

        public int RouteId => routeId;
        public int Order => order;

        public void OnLDtkImportFields(LDtkFields fields)
        {
            routeId = fields.GetInt("route_id");
            order = fields.GetInt("order");
        }
    }
}
