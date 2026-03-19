using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Modules.Effects
{
    public class ProjectileEffect : EffectInstance
    {
        private readonly float _damage;
        private readonly float _speed;
        private readonly GameObject _projectilePrefab;

        public ProjectileEffect(float damage, float speed, GameObject projectilePrefab)
        {
            _damage = damage;
            _speed = speed;
            _projectilePrefab = projectilePrefab;
        }

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            if (_projectilePrefab == null) return;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive) continue;

                var direction = (target.Position - origin).normalized;
                var go = Object.Instantiate(_projectilePrefab, origin, Quaternion.LookRotation(direction));

                var projectile = go.GetComponent<Projectile>();
                if (projectile != null)
                {
                    projectile.Initialize(direction, _speed, _damage, 0);
                }

                var netObj = go.GetComponent<NetworkObject>();
                if (netObj != null && NetworkManager.Singleton.IsServer)
                {
                    netObj.Spawn();
                }
            }
        }
    }
}
