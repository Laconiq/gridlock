using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Audio/Movement Audio")]
    public class MovementAudio : MonoBehaviour
    {
        [Header("Footsteps")]
        [SerializeField] private AudioSource footstepSource;
        [SerializeField] private AudioClip[] footstepClips;
        [SerializeField] private float baseStepInterval = 0.5f;
        [SerializeField] private float minStepInterval = 0.3f;
        [SerializeField] private float footstepVolume = 0.4f;
        [SerializeField] private float pitchVariation = 0.1f;

        [Header("Wind")]
        [SerializeField] private AudioSource windSource;
        [SerializeField] private float windSpeedThreshold = 0.5f;
        [SerializeField] private float maxWindVolume = 0.3f;
        [SerializeField] private float windPitchMin = 0.8f;
        [SerializeField] private float windPitchMax = 1.3f;
        [SerializeField] private float windSmoothTime = 0.3f;

        [Header("Landing")]
        [SerializeField] private AudioSource impactSource;
        [SerializeField] private AudioClip landingClip;
        [SerializeField] private float landingVolume = 0.5f;

        private PlayerController _player;
        private float _stepTimer;
        private float _windVolumeVelocity;
        private float _windPitchVelocity;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();

            if (windSource != null)
            {
                windSource.loop = true;
                windSource.volume = 0f;
                windSource.Play();
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
            if (_player == null) return;

            HandleFootsteps();
            HandleWind();
        }

        private void HandleFootsteps()
        {
            if (footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;

            float speed = _player.CurrentSpeedNormalized;
            if (speed < 0.1f || !_player.IsGrounded)
            {
                _stepTimer = 0f;
                return;
            }

            float interval = Mathf.Lerp(baseStepInterval, minStepInterval, speed);
            _stepTimer += Time.deltaTime;

            if (_stepTimer >= interval)
            {
                _stepTimer = 0f;
                var clip = footstepClips[Random.Range(0, footstepClips.Length)];
                footstepSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
                footstepSource.PlayOneShot(clip, footstepVolume * speed);
            }
        }

        private void HandleWind()
        {
            if (windSource == null || windSource.clip == null) return;

            float speed = _player.CurrentSpeedNormalized;
            float targetVolume = speed > windSpeedThreshold
                ? maxWindVolume * Mathf.InverseLerp(windSpeedThreshold, 1f, speed)
                : 0f;

            float targetPitch = Mathf.Lerp(windPitchMin, windPitchMax, speed);

            windSource.volume = Mathf.SmoothDamp(windSource.volume, targetVolume, ref _windVolumeVelocity, windSmoothTime);
            windSource.pitch = Mathf.SmoothDamp(windSource.pitch, targetPitch, ref _windPitchVelocity, windSmoothTime);
        }

        private void HandleLanding(float fallSpeed)
        {
            if (impactSource == null || landingClip == null) return;

            float t = Mathf.InverseLerp(3f, 20f, fallSpeed);
            impactSource.pitch = Mathf.Lerp(1.2f, 0.8f, t);
            impactSource.PlayOneShot(landingClip, landingVolume * (0.3f + 0.7f * t));
        }
    }
}
