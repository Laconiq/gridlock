using UnityEngine;
using UnityEngine.UI;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/UI/Hit Feedback")]
    public class HitFeedback : MonoBehaviour
    {
        [Header("Hit Marker")]
        [SerializeField] private RectTransform hitMarkerRoot;
        [SerializeField] private float hitMarkerScale = 1.5f;
        [SerializeField] private float hitMarkerDuration = 0.12f;
        [SerializeField] private Color normalHitColor = Color.white;
        [SerializeField] private Color killColor = Color.red;

        [Header("Kill Flash")]
        [SerializeField] private Image killFlashImage;
        [SerializeField] private float killFlashAlpha = 0.3f;
        [SerializeField] private float killFlashDuration = 0.1f;

        [Header("Audio")]
        [SerializeField] private AudioSource feedbackAudioSource;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private AudioClip killSound;

        private float _hitMarkerTimer;
        private float _killFlashTimer;
        private Vector3 _hitMarkerBaseScale;
        private Image[] _hitMarkerImages;

        private void Awake()
        {
            if (hitMarkerRoot != null)
            {
                _hitMarkerBaseScale = hitMarkerRoot.localScale;
                _hitMarkerImages = hitMarkerRoot.GetComponentsInChildren<Image>();
                hitMarkerRoot.gameObject.SetActive(false);
            }

            if (killFlashImage != null)
            {
                var c = killFlashImage.color;
                c.a = 0f;
                killFlashImage.color = c;
            }
        }

        private void Update()
        {
            UpdateHitMarker();
            UpdateKillFlash();
        }

        public void ShowHitMarker(bool isKill = false)
        {
            if (hitMarkerRoot == null) return;

            hitMarkerRoot.gameObject.SetActive(true);
            hitMarkerRoot.localScale = _hitMarkerBaseScale * hitMarkerScale;
            _hitMarkerTimer = hitMarkerDuration;

            var color = isKill ? killColor : normalHitColor;
            if (_hitMarkerImages != null)
            {
                foreach (var img in _hitMarkerImages)
                    img.color = color;
            }

            if (feedbackAudioSource != null)
            {
                var clip = isKill ? killSound : hitSound;
                if (clip != null)
                    feedbackAudioSource.PlayOneShot(clip, 0.5f);
            }

            if (isKill)
                TriggerKillFlash();
        }

        private void TriggerKillFlash()
        {
            _killFlashTimer = killFlashDuration;
        }

        private void UpdateHitMarker()
        {
            if (hitMarkerRoot == null) return;

            if (_hitMarkerTimer > 0f)
            {
                _hitMarkerTimer -= Time.deltaTime;
                float t = _hitMarkerTimer / hitMarkerDuration;

                hitMarkerRoot.localScale = Vector3.Lerp(_hitMarkerBaseScale, _hitMarkerBaseScale * hitMarkerScale, t);

                if (_hitMarkerImages != null)
                {
                    foreach (var img in _hitMarkerImages)
                    {
                        var c = img.color;
                        c.a = t;
                        img.color = c;
                    }
                }

                if (_hitMarkerTimer <= 0f)
                    hitMarkerRoot.gameObject.SetActive(false);
            }
        }

        private void UpdateKillFlash()
        {
            if (killFlashImage == null) return;

            if (_killFlashTimer > 0f)
            {
                _killFlashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(_killFlashTimer / killFlashDuration);
                var c = killFlashImage.color;
                c.a = killFlashAlpha * t;
                killFlashImage.color = c;
            }
        }
    }
}
