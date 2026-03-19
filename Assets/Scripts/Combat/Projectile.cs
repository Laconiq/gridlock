using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Combat
{
    public class Projectile : NetworkBehaviour
    {
        [SerializeField] private float maxLifetime = 5f;

        private Vector3 _direction;
        private float _speed;
        private float _damage;
        private ulong _sourceId;
        private float _lifetime;
        private bool _initialized;

        public void Initialize(Vector3 direction, float speed, float damage, ulong sourceId)
        {
            _direction = direction;
            _speed = speed;
            _damage = damage;
            _sourceId = sourceId;
            _initialized = true;
        }

        private void Update()
        {
            if (!IsServer || !_initialized) return;

            transform.position += _direction * (_speed * Time.deltaTime);
            _lifetime += Time.deltaTime;

            if (_lifetime >= maxLifetime)
            {
                NetworkObject.Despawn();
                return;
            }

            if (Physics.SphereCast(transform.position - _direction * 0.1f, 0.15f, _direction, out var hit, _speed * Time.deltaTime + 0.2f))
            {
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(new DamageInfo(_damage, _sourceId, DamageType.Projectile));
                }

                NetworkObject.Despawn();
            }
        }
    }
}
