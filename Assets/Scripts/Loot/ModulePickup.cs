using Gridlock.Audio;
using Gridlock.Grid;
using Gridlock.Mods;
using Gridlock.Visual;
using UnityEngine;

namespace Gridlock.Loot
{
    public class ModulePickup : MonoBehaviour
    {
        [Header("Idle Animation")]
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobAmplitude = 0.15f;
        [SerializeField] private float bobFrequency = 2f;

        [Header("Lifetime")]
        [SerializeField] private float lifetime = 30f;

        [Header("Magnet")]
        [SerializeField] private float magnetDelay = 1f;
        [SerializeField] private float magnetSpeed = 15f;
        [SerializeField] private float magnetAcceleration = 20f;
        [SerializeField] private float arcHeight = 3f;

        [Header("Collection Feedback")]
        [SerializeField] private float collectShakeIntensity = 0.06f;
        [SerializeField] private float collectShakeDuration = 0.04f;
        [SerializeField] private float collectWarpForce = 2f;
        [SerializeField] private float collectWarpRadius = 2.5f;

        private ModType _modType;
        private Rarity _rarity;
        private Color _rarityColor;
        private float _age;
        private float _baseY;
        private Vector3 _spawnPos;

        private bool _magnetActive;
        private float _flightProgress;
        private float _currentSpeed;
        private Vector3 _flightStart;
        private Vector3 _controlPoint;

        private MeshRenderer _modelRenderer;

        public void Initialize(ModType modType)
        {
            _modType = modType;
            _rarity = ModRarity.GetRarity(modType);
            _rarityColor = ModRarity.GetRarityColor(_rarity);
            _spawnPos = transform.position;
            _baseY = transform.position.y;

            _modelRenderer = GetComponentInChildren<MeshRenderer>();
            if (_modelRenderer != null)
            {
                var mat = _modelRenderer.material;
                mat.SetColor("_Color", _rarityColor);
                mat.SetColor("_EmissionColor", _rarityColor);
                mat.SetFloat("_EmissionIntensity", ModRarity.GetEmissionIntensity(_rarity));
            }
        }

        private void Update()
        {
            _age += Time.deltaTime;

            if (_age >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (!_magnetActive)
            {
                UpdateIdle();
                if (_age >= magnetDelay) BeginMagnet();
            }
            else
            {
                UpdateMagnet();
            }
        }

        private void UpdateIdle()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

            var pos = transform.position;
            float warpY = 0f;
            if (GridWarpManager.Instance != null)
                warpY = GridWarpManager.Instance.GetWarpOffset(pos.x, pos.z);

            pos.y = _baseY + warpY + Mathf.Sin(_age * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            transform.position = pos;
        }

        private void BeginMagnet()
        {
            _magnetActive = true;
            _flightStart = transform.position;
            _flightProgress = 0f;
            _currentSpeed = magnetSpeed * 0.3f;
            UpdateControlPoint();
        }

        private void UpdateControlPoint()
        {
            var target = GetMagnetTarget();
            _controlPoint = (_flightStart + target) * 0.5f + Vector3.up * arcHeight;
        }

        private void UpdateMagnet()
        {
            _currentSpeed += magnetAcceleration * Time.deltaTime;
            _flightProgress += _currentSpeed * Time.deltaTime * 0.1f;

            if (_flightProgress >= 1f)
            {
                Collect();
                return;
            }

            var target = GetMagnetTarget();
            float t = _flightProgress;
            float u = 1f - t;
            Vector3 pos = u * u * _flightStart + 2f * u * t * _controlPoint + t * t * target;
            transform.position = pos;

            float scale = 1f - t * t * 0.5f;
            transform.localScale = Vector3.one * 0.3f * scale;
        }

        private Vector3 GetMagnetTarget()
        {
            var panel = InventoryPanel.Instance;
            if (panel != null)
            {
                var screenPos = panel.GetTabScreenPosition();
                var cam = Camera.main;
                if (cam != null)
                    return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));
            }

            var camera = Camera.main;
            if (camera != null)
            {
                var screenEdge = new Vector3(Screen.width - 40f, Screen.height * 0.5f, 10f);
                return camera.ScreenToWorldPoint(screenEdge);
            }

            return transform.position + Vector3.right * 20f;
        }

        private void Collect()
        {
            PlayerInventory.Instance?.AddMod(_modType);

            ImpactFlash.Spawn(transform.position, _rarityColor, 0.3f, 0.12f);

            GridWarpManager.Instance?.DropStone(_spawnPos, collectWarpForce, collectWarpRadius, _rarityColor);

            GameJuice.Instance?.Shake(collectShakeDuration, collectShakeIntensity);

            InventoryPanel.Instance?.OnPickupCollected(_modType);

            SoundManager.Instance?.Play(SoundType.LootCollect, transform.position);

            Destroy(gameObject);
        }
    }
}
