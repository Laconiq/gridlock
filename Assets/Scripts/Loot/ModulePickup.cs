using AIWE.Player;
using UnityEngine;

namespace AIWE.Loot
{
    public class ModulePickup : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2f;
        [SerializeField] private float lifetime = 30f;
        [SerializeField] private float magnetDelay = 1f;
        [SerializeField] private float magnetSpeed = 15f;

        private string _moduleId;
        private Vector3 _startPos;
        private float _spawnTime;
        private bool _magnetActive;
        private Camera _mainCamera;

        public void Initialize(string moduleId, Vector3 position)
        {
            _moduleId = moduleId;
            transform.position = position + Vector3.up * 0.5f;
        }

        private void Start()
        {
            _startPos = transform.position;
            _spawnTime = Time.time;
            _mainCamera = Camera.main;
        }

        private void Update()
        {
            float elapsed = Time.time - _spawnTime;

            if (elapsed > lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (!_magnetActive && elapsed >= magnetDelay)
            {
                _magnetActive = true;
            }

            if (_magnetActive)
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    var target = _mainCamera.transform.position;
                    transform.position = Vector3.MoveTowards(transform.position, target, magnetSpeed * Time.deltaTime);

                    if (Vector3.Distance(transform.position, target) < 0.5f)
                    {
                        var inventory = FindAnyObjectByType<PlayerInventory>();
                        if (inventory != null)
                        {
                            inventory.AddModule(_moduleId);
                            Debug.Log($"[ModulePickup] Auto-collected {_moduleId}");
                        }
                        Destroy(gameObject);
                        return;
                    }
                }
            }
            else
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
                var bobOffset = Mathf.Sin((Time.time - _spawnTime) * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
                transform.position = _startPos + Vector3.up * bobOffset;
            }
        }
    }
}
