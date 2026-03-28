using System;
using System.Collections.Generic;
using AIWE.Interfaces;
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

        public float Speed => speed;
        public GameObject ProjectilePrefab => projectilePrefab;

        public override void Execute(List<ITargetable> targets, Vector3 origin)
        {
            if (projectilePrefab == null) return;

            foreach (var target in targets)
            {
                if (target == null || !target.IsAlive || target.Transform == null) continue;

                var direction = (target.Position - origin).normalized;

                var go = Object.Instantiate(projectilePrefab, origin, Quaternion.LookRotation(direction));

                var projectile = go.GetComponent<Combat.Projectile>();
                if (projectile != null)
                    projectile.Initialize(direction, speed, damage, dealsDamage: true);
            }
        }

        public static void SpawnVisual(GameObject prefab, Vector3 origin, Vector3 direction, float speed)
        {
            if (prefab == null) return;

            var go = Object.Instantiate(prefab, origin, Quaternion.LookRotation(direction));

            var projectile = go.GetComponent<Combat.Projectile>();
            if (projectile != null)
                projectile.Initialize(direction, speed, 0f, dealsDamage: false);
        }

        public override EffectInstance CreateInstance()
        {
            return new ProjectileEffect
            {
                damage = damage,
                speed = speed,
                projectilePrefab = projectilePrefab,
                cooldown = cooldown
            };
        }
    }
}
