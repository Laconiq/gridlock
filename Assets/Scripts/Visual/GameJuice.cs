using Gridlock.Audio;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Gridlock.Visual
{
    [DefaultExecutionOrder(200)]
    public class GameJuice : MonoBehaviour
    {
        public static GameJuice Instance { get; private set; }

        [Header("Screen Shake")]
        [SerializeField] private float killShakeIntensity = 0.25f;
        [SerializeField] private float killShakeDuration = 0.12f;
        [SerializeField] private float hitShakeIntensity = 0.08f;
        [SerializeField] private float hitShakeDuration = 0.05f;

        [Header("Freeze Frame")]
        [SerializeField] private float killFreezeDuration = 0.04f;

        [Header("Chromatic Aberration Pulse")]
        [SerializeField] private float killChromaticIntensity = 0.6f;
        [SerializeField] private float chromaticDecaySpeed = 4f;

        [Header("Bloom Pulse")]
        [SerializeField] private float killBloomBoost = 2f;
        [SerializeField] private float bloomDecaySpeed = 6f;

        private Camera _camera;
        private Volume _volume;
        private ChromaticAberration _chromatic;
        private Bloom _bloom;
        private Vignette _vignette;

        private float _shakeTimer;
        private float _shakeDuration;
        private float _shakeIntensity;

        private float _freezeTimer;
        private float _savedTimeScale = 1f;

        private float _chromaticTarget;
        private float _baseBloomIntensity;

        private void Awake()
        {
            Instance = this;
            _camera = Camera.main;

            _volume = FindAnyObjectByType<Volume>();
            if (_volume != null && _volume.profile != null)
            {
                if (!_volume.profile.TryGet(out _chromatic))
                {
                    _chromatic = _volume.profile.Add<ChromaticAberration>();
                    _chromatic.intensity.overrideState = true;
                }

                if (!_volume.profile.TryGet(out _bloom))
                {
                    _bloom = _volume.profile.Add<Bloom>();
                    _bloom.intensity.overrideState = true;
                    _bloom.threshold.overrideState = true;
                    _bloom.threshold.value = 0.8f;
                    _bloom.intensity.value = 1.5f;
                }
                _baseBloomIntensity = _bloom.intensity.value;

                if (!_volume.profile.TryGet(out _vignette))
                {
                    _vignette = _volume.profile.Add<Vignette>();
                    _vignette.intensity.overrideState = true;
                    _vignette.color.overrideState = true;
                    _vignette.color.value = new Color(0f, 0.05f, 0.1f);
                    _vignette.intensity.value = 0.3f;
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (_chromatic != null) _chromatic.intensity.value = 0f;
            if (_bloom != null) _bloom.intensity.value = _baseBloomIntensity;
        }

        public void OnEnemyHit(Vector3 position)
        {
            Shake(hitShakeDuration, hitShakeIntensity);
            SoundManager.Instance?.Play(SoundType.EnemyHit, position);

            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warp.DropStone(position, 3f, 3f, new Color(1f, 0.4f, 0.1f));
        }

        public void OnEnemyKilled(Vector3 position)
        {
            Shake(killShakeDuration, killShakeIntensity);
            FreezeFrame(killFreezeDuration);
            _chromaticTarget = killChromaticIntensity;
            if (_bloom != null)
                _bloom.intensity.value = _baseBloomIntensity + killBloomBoost;
            SoundManager.Instance?.Play(SoundType.EnemyDeath, position);

            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warp.Shockwave(position, 5.5f, 6f, new Color(1f, 0.15f, 0.15f));
        }

        public void OnTowerFired(Vector3 position)
        {
            SoundManager.Instance?.Play(SoundType.TowerFire, position);

            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warp.DropStone(position, 1.5f, 2f, new Color(0f, 1f, 1f));
        }

        public void OnTowerPlaced(Vector3 position)
        {
            SoundManager.Instance?.Play(SoundType.TowerPlace, position);

            var warp = Grid.GridWarpManager.Instance;
            if (warp != null)
                warp.Shockwave(position, 5f, 3f, new Color(0f, 1f, 0.7f));
        }

        public void Shake(float duration, float intensity)
        {
            if (intensity > _shakeIntensity * (_shakeTimer / Mathf.Max(_shakeDuration, 0.001f)))
            {
                _shakeDuration = duration;
                _shakeIntensity = intensity;
                _shakeTimer = duration;
            }
        }

        private void FreezeFrame(float duration)
        {
            if (_freezeTimer > 0f) return;
            _savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _freezeTimer = duration;
        }

        private void LateUpdate()
        {
            // Freeze frame (unscaled)
            if (_freezeTimer > 0f)
            {
                _freezeTimer -= Time.unscaledDeltaTime;
                if (_freezeTimer <= 0f)
                    Time.timeScale = _savedTimeScale;
            }

            // Screen shake
            if (_shakeTimer > 0f && _camera != null)
            {
                _shakeTimer -= Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_shakeTimer / _shakeDuration);
                float magnitude = _shakeIntensity * t * t;
                _camera.transform.position += Random.insideUnitSphere * magnitude;
            }

            // Chromatic aberration decay
            if (_chromatic != null && _chromaticTarget > 0f)
            {
                _chromaticTarget = Mathf.MoveTowards(_chromaticTarget, 0f, chromaticDecaySpeed * Time.unscaledDeltaTime);
                _chromatic.intensity.value = _chromaticTarget;
            }

            // Bloom decay
            if (_bloom != null && _bloom.intensity.value > _baseBloomIntensity)
            {
                _bloom.intensity.value = Mathf.MoveTowards(_bloom.intensity.value, _baseBloomIntensity,
                    bloomDecaySpeed * Time.unscaledDeltaTime);
            }
        }
    }
}
