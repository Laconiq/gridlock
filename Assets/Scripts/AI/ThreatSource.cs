using Unity.Netcode;
using UnityEngine;

namespace AIWE.AI
{
    public class ThreatSource : MonoBehaviour
    {
        [SerializeField] private float decayFactor = 0.85f;

        private float _accumulated;

        public float RecentDPS { get; private set; }

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
            if (NetworkManager.Singleton?.SpawnManager?.SpawnedObjects
                    .TryGetValue(sourceId, out var netObj) == true)
            {
                var ts = netObj.GetComponent<ThreatSource>();
                ts?.ReportDamage(amount);
            }
        }
    }
}
