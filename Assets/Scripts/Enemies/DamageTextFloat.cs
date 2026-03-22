using UnityEngine;

namespace AIWE.Enemies
{
    public class DamageTextFloat : MonoBehaviour
    {
        [SerializeField] private float riseSpeed = 1.5f;
        [SerializeField] private float fadeSpeed = 2f;

        private TMPro.TextMeshPro _tmp;
        private Color _startColor;
        private float _elapsed;
        private Camera _cachedCamera;

        private void Awake()
        {
            _tmp = GetComponent<TMPro.TextMeshPro>();
            if (_tmp != null)
                _startColor = _tmp.color;
        }

        private void Update()
        {
            transform.position += Vector3.up * (riseSpeed * Time.deltaTime);

            if (_cachedCamera == null) _cachedCamera = Camera.main;
            if (_cachedCamera != null)
                transform.forward = _cachedCamera.transform.forward;

            _elapsed += Time.deltaTime;
            if (_tmp != null)
            {
                var c = _startColor;
                c.a = Mathf.Lerp(1f, 0f, _elapsed * fadeSpeed);
                _tmp.color = c;
            }
        }
    }
}
