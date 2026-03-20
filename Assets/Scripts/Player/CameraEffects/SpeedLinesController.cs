using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Speed Lines")]
    public class SpeedLinesController : MonoBehaviour
    {
        [SerializeField] private ParticleSystem speedLinesParticles;
        [SerializeField] private float speedThreshold = 0.7f;
        [SerializeField] private float maxEmissionRate = 40f;

        private PlayerController _player;
        private ParticleSystem.EmissionModule _emission;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();

            if (speedLinesParticles != null)
            {
                _emission = speedLinesParticles.emission;
                _emission.rateOverTime = 0f;
            }
        }

        private void Update()
        {
            if (speedLinesParticles == null || _player == null) return;

            float speed = _player.CurrentSpeedNormalized;
            float t = Mathf.InverseLerp(speedThreshold, 1f, speed);

            _emission.rateOverTime = t * maxEmissionRate;
        }
    }
}
