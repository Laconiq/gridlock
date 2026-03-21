using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Combat
{
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float maxLifetime = 5f;

        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private float _lifetime;
        private bool _initialized;
        private bool _serverSide;

        public void Initialize(Vector3 direction, float speed, float damage, bool serverSide)
        {
            _direction = direction;
            _speed = speed;
            _damage = damage;
            _serverSide = serverSide;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            transform.position += _direction * (_speed * Time.deltaTime);
            _lifetime += Time.deltaTime;

            if (_lifetime >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (!_serverSide) return;

            if (Physics.SphereCast(transform.position - _direction * 0.1f, 0.15f, _direction, out var hit, _speed * Time.deltaTime + 0.2f))
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                    damageable.TakeDamage(new DamageInfo(_damage, 0, DamageType.Projectile));

                Destroy(gameObject);
            }
        }
    }
}
