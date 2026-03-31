using TMPro;
using UnityEngine;

namespace Gridlock.Enemies
{
    public class EnemyHitFeedback : MonoBehaviour
    {
        [Header("Flash")]
        [SerializeField] private float flashDuration = 0.1f;
        [SerializeField] private float flashIntensity = 5f;

        [Header("Damage Text")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private float textOffsetZ = 0.5f;
        [SerializeField] private float textLifetime = 0.8f;

        private EnemyHealth _health;
        private MeshRenderer _renderer;
        private Material _materialInstance;
        private Color _baseEmission;
        private float _flashTimer;
        private Camera _cachedCamera;

        private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _renderer = GetComponentInChildren<MeshRenderer>();
            _cachedCamera = Camera.main;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.CurrentHPChanged += OnHPChanged;
                _health.OnDeath += OnDeath;
            }

            if (_renderer != null)
            {
                _materialInstance = _renderer.material;
                if (_materialInstance.HasColor(EmissionColor))
                    _baseEmission = _materialInstance.GetColor(EmissionColor);
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.CurrentHPChanged -= OnHPChanged;
                _health.OnDeath -= OnDeath;
            }

            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
                _materialInstance = null;
            }
        }

        private void Update()
        {
            if (_flashTimer <= 0f) return;

            _flashTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(_flashTimer / flashDuration);

            if (_materialInstance != null && _materialInstance.HasColor(EmissionColor))
            {
                var col = Color.Lerp(_baseEmission, Color.white * flashIntensity, t);
                _materialInstance.SetColor(EmissionColor, col);
            }
        }

        private void OnHPChanged(float damage)
        {
            _flashTimer = flashDuration;
            SpawnDamageText(damage);
        }

        private void OnDeath()
        {
            if (_materialInstance != null && _materialInstance.HasColor(EmissionColor))
                _materialInstance.SetColor(EmissionColor, Color.white * flashIntensity * 2f);
        }

        private void SpawnDamageText(float damage)
        {
            if (damageTextPrefab == null) return;

            var pos = transform.position + Vector3.up * textOffsetZ;
            pos += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));

            var rot = _cachedCamera != null ? _cachedCamera.transform.rotation : Quaternion.identity;
            var go = Instantiate(damageTextPrefab, pos, rot);
            var tmp = go.GetComponent<TextMeshPro>();
            if (tmp != null)
                tmp.text = Mathf.RoundToInt(damage).ToString();

            Destroy(go, textLifetime);
        }
    }
}
