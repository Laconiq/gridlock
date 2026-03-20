using Unity.Cinemachine;
using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Camera/Weapon Fire Feedback")]
    public class WeaponFireFeedback : MonoBehaviour
    {
        [Header("Camera Recoil Punch")]
        [SerializeField] private float recoilPitch = 2f;
        [SerializeField] private float recoilYawRandom = 0.5f;
        [SerializeField] private float recoilRecoverySpeed = 12f;

        [Header("Screen Shake")]
        [SerializeField] private CinemachineImpulseSource fireImpulseSource;
        [SerializeField] private float fireShakeForce = 0.15f;

        [Header("Muzzle Flash")]
        [SerializeField] private ParticleSystem muzzleFlashParticles;
        [SerializeField] private Light muzzleFlashLight;
        [SerializeField] private float lightIntensity = 800f;
        [SerializeField] private float lightRange = 5f;

        private PlayerCamera _camera;
        private float _currentRecoilPitch;
        private float _currentRecoilYaw;
        private float _lightTimer;

        private void Awake()
        {
            _camera = GetComponentInParent<PlayerCamera>();

            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.intensity = 0f;
                muzzleFlashLight.range = lightRange;
                muzzleFlashLight.color = new Color(1f, 0.85f, 0.4f);
            }
        }

        private void LateUpdate()
        {
            RecoverRecoil();
            HandleMuzzleLight();
        }

        public void Fire()
        {
            ApplyRecoilPunch();
            FireScreenShake();
            FireMuzzleFlash();
        }

        private void ApplyRecoilPunch()
        {
            _currentRecoilPitch += recoilPitch;
            _currentRecoilYaw += Random.Range(-recoilYawRandom, recoilYawRandom);
        }

        private void RecoverRecoil()
        {
            if (_camera == null) return;

            _currentRecoilPitch = Mathf.Lerp(_currentRecoilPitch, 0f, recoilRecoverySpeed * Time.deltaTime);
            _currentRecoilYaw = Mathf.Lerp(_currentRecoilYaw, 0f, recoilRecoverySpeed * Time.deltaTime);

            _camera.ApplyRecoil(_currentRecoilPitch, _currentRecoilYaw);
        }

        private void FireScreenShake()
        {
            if (fireImpulseSource != null)
                fireImpulseSource.GenerateImpulseWithForce(fireShakeForce);
        }

        private void FireMuzzleFlash()
        {
            if (muzzleFlashParticles != null)
                muzzleFlashParticles.Play();

            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.intensity = lightIntensity;
                _lightTimer = 0.05f;
            }
        }

        private void HandleMuzzleLight()
        {
            if (muzzleFlashLight == null) return;

            if (_lightTimer > 0f)
            {
                _lightTimer -= Time.deltaTime;
                if (_lightTimer <= 0f)
                    muzzleFlashLight.intensity = 0f;
            }
        }
    }
}
