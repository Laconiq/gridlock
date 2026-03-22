using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AIWE.AI
{
    [CreateAssetMenu(menuName = "AIWE/AI/Threat Calculator Config")]
    public class ThreatCalculatorConfig : ScriptableObject
    {
        [TitleGroup("Factor Weights")]
        [SerializeField, LabelWidth(140), PropertyRange(0f, 2f)]
        [OnValueChanged(nameof(NotifyChanged))]
        public float distanceWeight = 1f;

        [TitleGroup("Factor Weights")]
        [SerializeField, LabelWidth(140), PropertyRange(0f, 2f)]
        [OnValueChanged(nameof(NotifyChanged))]
        public float lineOfSightWeight = 0.5f;

        [TitleGroup("Factor Weights")]
        [SerializeField, LabelWidth(140), PropertyRange(0f, 2f)]
        [OnValueChanged(nameof(NotifyChanged))]
        public float dpsWeight = 0.8f;

        [TitleGroup("Factor Weights")]
        [SerializeField, LabelWidth(140), PropertyRange(0f, 2f)]
        [OnValueChanged(nameof(NotifyChanged))]
        public float crowdWeight = 0.3f;

        [TitleGroup("Thresholds")]
        [SerializeField, LabelWidth(140), PropertyRange(0f, 1f)]
        [InfoBox("Minimum threat score required to trigger aggro")]
        [OnValueChanged(nameof(NotifyChanged))]
        public float aggroThreshold = 0.3f;

        [TitleGroup("Normalization")]
        [SerializeField, LabelWidth(140), MinValue(1f)]
        [InfoBox("DPS value that maps to a factor of 1.0")]
        [OnValueChanged(nameof(NotifyChanged))]
        public float maxDPSReference = 50f;

        [TitleGroup("Normalization")]
        [SerializeField, LabelWidth(140), MinValue(1)]
        [OnValueChanged(nameof(NotifyChanged))]
        public int maxCrowdCount = 3;

        [TitleGroup("Line of Sight")]
        [SerializeField, LabelWidth(140)]
        [OnValueChanged(nameof(NotifyChanged))]
        public LayerMask losObstacleMask = 1;

        private void NotifyChanged()
        {
#if UNITY_EDITOR
            OnConfigChanged?.Invoke();
#endif
        }

#if UNITY_EDITOR
        public event System.Action OnConfigChanged;
#endif
    }
}
