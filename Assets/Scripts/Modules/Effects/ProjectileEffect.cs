using System;
using System.Collections.Generic;
using AIWE.Combat;
using AIWE.Interfaces;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AIWE.Modules.Effects
{
    [Serializable]
    public class ProjectileEffect : EffectInstance
    {
        [SerializeField] private float damage = 15f;
        [SerializeField] private float speed = 20f;
        [SerializeField] private GameObject projectilePrefab;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[ProjectileEffect] No projectile prefab assigned");
                return;
            }

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var direction = (target.Position - origin).normalized;
                var go = Object.Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));

                var netObj = go.GetComponent<NetworkObject>();
                if (netObj != null && NetworkManager.Singleton.IsServer)
                    netObj.Spawn();

                var projectile = go.GetComponent<Projectile>();
                if (projectile != null)
                    projectile.Initialize(direction, speed, damage, 0);
            }
        }

        public override EffectInstance CreateInstance()
        {
            return new ProjectileEffect
            {
                damage = damage,
                speed = speed,
                projectilePrefab = projectilePrefab
            };
        }
    }
}
