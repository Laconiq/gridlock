using UnityEngine;

namespace Gridlock.Loot
{
    public class LootDropper : MonoBehaviour
    {
        public static LootDropper Instance { get; private set; }

        [SerializeField] private LootTable lootTable;
        [SerializeField] private GameObject pickupPrefab;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void OnEnemyDied(Vector3 position)
        {
            if (lootTable == null || pickupPrefab == null) return;
            if (Random.value > lootTable.DropChance) return;

            var modType = lootTable.Roll();
            var go = Instantiate(pickupPrefab, position, Quaternion.identity);
            var pickup = go.GetComponent<ModulePickup>();
            if (pickup != null) pickup.Initialize(modType);
        }
    }
}
