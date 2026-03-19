using UnityEngine;

namespace AIWE.Towers
{
    [CreateAssetMenu(menuName = "AIWE/Chassis Definition")]
    public class ChassisDefinition : ScriptableObject
    {
        public string chassisId;
        public string displayName;
        public int maxTriggers = 3;
        public float baseRange = 10f;
        [Range(0f, 360f)]
        public float rotationArc = 360f;
        public bool isPlayerChassis;
    }
}
