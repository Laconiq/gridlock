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
        private bool _dealsDamage;
        private ITargetable _target;

        public void Initialize(Vector3 direction, float speed, float damage, bool dealsDamage,
            ITargetable target = null)
        {
            _direction = direction;
            _speed = speed;
            _damage = damage;
            _dealsDamage = dealsDamage;
            _target = target;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;

            if (_target != null && _target.IsAlive && _target.Transform != null)
            {
                var toTarget = _target.Position - transform.position;
                if (toTarget.sqrMagnitude > 0.001f)
                {
                    _direction = toTarget.normalized;
                    transform.rotation = Quaternion.LookRotation(_direction);
                }
            }

            transform.position += _direction * (_speed * Time.deltaTime);
            _lifetime += Time.deltaTime;

            if (_lifetime >= maxLifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (!_dealsDamage) return;

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
