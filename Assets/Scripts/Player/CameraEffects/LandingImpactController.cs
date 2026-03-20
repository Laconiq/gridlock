using Unity.Cinemachine;
using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Landing Impact")]
    public class LandingImpactController : MonoBehaviour
    {
        [SerializeField] private CinemachineImpulseSource impulseSource;
        [SerializeField] private float minFallSpeed = 4f;
        [SerializeField] private float maxFallSpeed = 20f;
        [SerializeField] private float maxImpulseForce = 1f;

        private PlayerController _player;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            if (impulseSource == null)
                impulseSource = GetComponent<CinemachineImpulseSource>();
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

        private void HandleLanding(float fallSpeed)
        {
            if (impulseSource == null) return;

            float t = Mathf.InverseLerp(minFallSpeed, maxFallSpeed, fallSpeed);
            float force = Mathf.Lerp(0.2f, maxImpulseForce, t);

            impulseSource.GenerateImpulseWithForce(force);
        }
    }
}
