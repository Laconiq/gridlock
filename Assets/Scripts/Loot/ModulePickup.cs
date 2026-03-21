using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using AIWE.Player;

namespace AIWE.Loot
{
    public class ModulePickup : NetworkBehaviour
    {
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float lifetime = 30f;

        private NetworkVariable<FixedString64Bytes> _moduleId = new();
        private Vector3 _startPos;
        private float _spawnTime;

        private string _pendingModuleId;
        private Vector3 _pendingPosition;

        public void Initialize(string moduleId, Vector3 position)
        {
            _pendingModuleId = moduleId;
            _pendingPosition = position + Vector3.up * 0.5f;
            transform.position = _pendingPosition;
        }

        public override void OnNetworkSpawn()
        {
            if (IsServer && _pendingModuleId != null)
                _moduleId.Value = new FixedString64Bytes(_pendingModuleId);

            _startPos = transform.position;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            var bobOffset = Mathf.Sin((Time.time - _spawnTime) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.position = _startPos + Vector3.up * bobOffset;

            if (IsServer && Time.time - _spawnTime > lifetime)
                NetworkObject.Despawn();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer) return;

            var inventory = other.GetComponentInParent<PlayerInventory>();
            if (inventory == null) return;

            inventory.AddModule(_moduleId.Value.ToString());
            Debug.Log($"[ModulePickup] {other.name} picked up {_moduleId.Value}");
            NetworkObject.Despawn();
        }
    }
}
