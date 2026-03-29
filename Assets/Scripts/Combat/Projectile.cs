using Gridlock.Audio;
using Gridlock.Interfaces;
using UnityEngine;

namespace Gridlock.Combat
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

            gameObject.AddComponent<Visual.WarpFollower>();

            var juice = Visual.GameJuice.Instance;
            if (juice != null)
                juice.OnTowerFired(transform.position);
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
                    damageable.TakeDamage(new DamageInfo(_damage, DamageType.Projectile));

                Visual.ImpactFlash.Spawn(hit.point, new Color(0f, 1f, 1f));
                SoundManager.Instance?.Play(SoundType.ProjectileImpact, hit.point);
                Destroy(gameObject);
            }
        }
    }
}
