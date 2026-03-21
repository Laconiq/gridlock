using MoreMountains.Feedbacks;
using TMPro;
using UnityEngine;

namespace AIWE.Enemies
{
    public class EnemyHitFeedback : MonoBehaviour
    {
        [Header("Feedbacks")]
        [SerializeField] private MMF_Player hitFeedback;
        [SerializeField] private MMF_Player deathFeedback;

        [Header("Damage Text")]
        [SerializeField] private GameObject damageTextPrefab;
        [SerializeField] private float textOffsetY = 1.5f;
        [SerializeField] private float textLifetime = 0.8f;

        private EnemyHealth _health;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health._currentHPChanged += OnHPChanged;
                _health.OnDeath += OnDeath;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health._currentHPChanged -= OnHPChanged;
                _health.OnDeath -= OnDeath;
            }
        }

        private void OnHPChanged(float damage)
        {
            hitFeedback?.PlayFeedbacks();
            SpawnDamageText(damage);
        }

        private void OnDeath()
        {
            deathFeedback?.PlayFeedbacks();
        }

        private void SpawnDamageText(float damage)
        {
            if (damageTextPrefab == null) return;

            var pos = transform.position + Vector3.up * textOffsetY;
            pos += Random.insideUnitSphere * 0.3f;
            pos.y = transform.position.y + textOffsetY;

            var go = Instantiate(damageTextPrefab, pos, Quaternion.identity);
            var tmp = go.GetComponent<TextMeshPro>();
            if (tmp != null)
                tmp.text = Mathf.RoundToInt(damage).ToString();

            Destroy(go, textLifetime);
        }
    }
}
