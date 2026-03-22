using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public abstract class ZoneInstance
    {
        protected static readonly Collider[] SharedOverlapBuffer = new Collider[64];

        [NonSerialized] public ZoneDefinition Definition;
        [NonSerialized] public IChassis Owner;

        [SerializeField] protected float cooldown;
        [NonSerialized] private float _cooldownTimer;

        public float Cooldown => cooldown;
        public bool IsReady => _cooldownTimer <= 0f;

        public void TickCooldown(float dt)
        {
            if (_cooldownTimer > 0f) _cooldownTimer -= dt;
        }

        public void StartCooldown()
        {
            _cooldownTimer = cooldown;
        }

        public abstract List<ITargetable> SelectTargets(Vector3 origin, float range);

        public abstract ZoneInstance CreateInstance();
    }
}
