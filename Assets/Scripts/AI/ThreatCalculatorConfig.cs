using UnityEngine;

namespace AIWE.AI
{
    [CreateAssetMenu(menuName = "AIWE/AI/Threat Calculator Config")]
    public class ThreatCalculatorConfig : ScriptableObject
    {
        [Header("Factor Weights")]
        [SerializeField] public float distanceWeight = 1f;
        [SerializeField] public float lineOfSightWeight = 0.5f;
        [SerializeField] public float dpsWeight = 0.8f;
        [SerializeField] public float crowdWeight = 0.3f;

        [Header("Thresholds")]
        [SerializeField] public float aggroThreshold = 0.3f;

        [Header("Normalization")]
        [SerializeField] public float maxDPSReference = 50f;
        [SerializeField] public int maxCrowdCount = 3;
    }
}
