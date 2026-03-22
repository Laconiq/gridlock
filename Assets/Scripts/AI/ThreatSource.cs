using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.AI
{
    public class ThreatSource : MonoBehaviour
    {
        private static readonly HashSet<ThreatSource> _all = new();
        public static IReadOnlyCollection<ThreatSource> All => _all;

        [SerializeField] private float decayFactor = 0.85f;

        private float _accumulated;

        public float RecentDPS { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _all.Clear();

        private void OnEnable() => _all.Add(this);
        private void OnDisable() => _all.Remove(this);

        private void Update()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && !nm.IsServer) return;

            RecentDPS = RecentDPS * Mathf.Pow(decayFactor, Time.deltaTime) + _accumulated;
            _accumulated = 0f;
        }

        public void ReportDamage(float amount)
        {
            _accumulated += amount;
        }

        public static void ReportDamageFromSource(ulong sourceId, float amount)
        {
            if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                    .TryGetValue(sourceId, out var netObj) == true)
            {
                var ts = netObj.GetComponent<ThreatSource>();
                ts?.ReportDamage(amount);
            }
        }
    }
}
