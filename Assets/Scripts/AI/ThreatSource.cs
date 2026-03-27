using System.Collections.Generic;
using UnityEngine;

namespace AIWE.AI
{
    public class ThreatSource : MonoBehaviour
    {
        private static readonly HashSet<ThreatSource> _all = new();
        private static readonly Dictionary<ulong, ThreatSource> _registry = new();
        public static IReadOnlyCollection<ThreatSource> All => _all;

        [SerializeField] private float decayFactor = 0.85f;

        private float _accumulated;
        private ulong _sourceId;

        public float RecentDPS { get; private set; }
        public ulong SourceId => _sourceId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _all.Clear();
            _registry.Clear();
        }

        private void OnEnable()
        {
            _all.Add(this);
            _sourceId = (ulong)gameObject.GetInstanceID();
            _registry[_sourceId] = this;
        }

        private void OnDisable()
        {
            _all.Remove(this);
            _registry.Remove(_sourceId);
        }

        private void Update()
        {
            RecentDPS = RecentDPS * Mathf.Pow(decayFactor, Time.deltaTime) + _accumulated;
            _accumulated = 0f;
        }

        public void ReportDamage(float amount)
        {
            _accumulated += amount;
        }

        public static void ReportDamageFromSource(ulong sourceId, float amount)
        {
            if (_registry.TryGetValue(sourceId, out var ts))
                ts.ReportDamage(amount);
        }
    }
}
