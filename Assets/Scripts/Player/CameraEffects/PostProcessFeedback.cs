using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Post-Process Feedback")]
    public class PostProcessFeedback : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Volume volume;

        [Header("Speed Vignette")]
        [SerializeField] private float vignetteSpeedThreshold = 0.6f;
        [SerializeField] private float maxVignetteIntensity = 0.35f;
        [SerializeField] private float vignetteSmoothTime = 0.2f;

        [Header("Landing Chromatic Aberration")]
        [SerializeField] private float maxChromaticIntensity = 0.6f;
        [SerializeField] private float chromaticDecaySpeed = 4f;

        [Header("Damage Chromatic Aberration")]
        [SerializeField] private float damageChromaticIntensity = 0.5f;

        private PlayerController _player;
        private Vignette _vignette;
        private ChromaticAberration _chromatic;
        private float _vignetteVelocity;
        private float _currentChromatic;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();

            if (volume == null)
                volume = FindAnyObjectByType<Volume>();

            if (volume != null)
            {
                if (volume.profile == null)
                {
                    volume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
                }

                if (!volume.profile.TryGet(out _vignette))
                {
                    _vignette = volume.profile.Add<Vignette>();
                    _vignette.intensity.overrideState = true;
                    _vignette.intensity.value = 0f;
                }

                if (!volume.profile.TryGet(out _chromatic))
                {
                    _chromatic = volume.profile.Add<ChromaticAberration>();
                    _chromatic.intensity.overrideState = true;
                    _chromatic.intensity.value = 0f;
                }
            }
        }

        private void OnEnable()
        {
            if (_player != null)
                _player.OnLanded += HandleLanding;
        }

        private void OnDisable()
        {
            if (_player != null)
                _player.OnLanded -= HandleLanding;
        }

        private void Update()
        {
            HandleSpeedVignette();
            HandleChromaticDecay();
        }

        private void HandleSpeedVignette()
        {
            if (_vignette == null || _player == null) return;

            float speed = _player.CurrentSpeedNormalized;
            float targetIntensity = speed > vignetteSpeedThreshold
                ? maxVignetteIntensity * Mathf.InverseLerp(vignetteSpeedThreshold, 1f, speed)
                : 0f;

            float current = _vignette.intensity.value;
            float smoothed = Mathf.SmoothDamp(current, targetIntensity, ref _vignetteVelocity, vignetteSmoothTime);
            _vignette.intensity.Override(smoothed);
        }

        private void HandleChromaticDecay()
        {
            if (_chromatic == null) return;

            _currentChromatic = Mathf.MoveTowards(_currentChromatic, 0f, chromaticDecaySpeed * Time.deltaTime);
            _chromatic.intensity.Override(_currentChromatic);
        }

        private void HandleLanding(float fallSpeed)
        {
            if (_chromatic == null) return;

            float t = Mathf.InverseLerp(4f, 20f, fallSpeed);
            _currentChromatic = Mathf.Max(_currentChromatic, Mathf.Lerp(0.15f, maxChromaticIntensity, t));
        }

        public void PulseDamage()
        {
            _currentChromatic = Mathf.Max(_currentChromatic, damageChromaticIntensity);
        }
    }
}
