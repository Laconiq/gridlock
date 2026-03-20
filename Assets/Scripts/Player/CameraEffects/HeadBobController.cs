using Unity.Cinemachine;
using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Head Bob Controller")]
    public class HeadBobController : MonoBehaviour
    {
        [SerializeField] private CinemachineBasicMultiChannelPerlin noise;
        [SerializeField] private float amplitudeSmooth = 8f;
        [SerializeField] private float minSpeedThreshold = 0.1f;

        private PlayerController _player;
        private float _currentAmplitude;

        private void Awake()
        {
            _player = GetComponentInParent<PlayerController>();
            if (noise == null)
                noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
        }

        private void Update()
        {
            if (noise == null || _player == null) return;

            float speed = _player.CurrentSpeedNormalized;
            bool isMoving = speed > minSpeedThreshold && _player.IsGrounded;

            float targetAmplitude = isMoving ? speed : 0f;
            _currentAmplitude = Mathf.Lerp(_currentAmplitude, targetAmplitude, amplitudeSmooth * Time.deltaTime);

            noise.AmplitudeGain = _currentAmplitude;
            noise.FrequencyGain = 0.8f + speed * 0.4f;
        }
    }
}
