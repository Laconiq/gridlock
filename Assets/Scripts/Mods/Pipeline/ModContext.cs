using System.Collections.Generic;
using UnityEngine;
using Gridlock.Interfaces;

namespace Gridlock.Mods.Pipeline
{
    public struct ModContext
    {
        public Vector3 Position;
        public Vector3 Direction;
        public float Damage;
        public float Speed;
        public float Size;
        public float MaxLifetime;
        public float Lifetime;
        public float WideRadius;
        public ITargetable Target;
        public GameObject HitObject;
        public ModTags Tags;
        public List<SpawnRequest> SpawnRequests;
        public List<SynergyEffect> Synergies;
        public HashSet<int> HitInstances;
        public bool Consumed;
        public bool KilledThisHit;
        public float OverkillAmount;
        public int PierceRemaining;
        public int BounceRemaining;
        public float PulseTimer;
        public bool DelayFired;
        public ModPipeline OwnerPipeline;

        public static ModContext Create(float damage, float speed, float size, float lifetime)
        {
            return new ModContext
            {
                Damage = damage,
                Speed = speed,
                Size = size,
                MaxLifetime = lifetime,
                Lifetime = 0f,
                WideRadius = 2f,
                SpawnRequests = new List<SpawnRequest>(),
                Synergies = new List<SynergyEffect>(),
                HitInstances = new HashSet<int>(),
            };
        }

        public ModContext CloneForSub(float damageScale)
        {
            var copy = this;
            copy.Damage *= damageScale;
            copy.SpawnRequests = new List<SpawnRequest>();
            copy.Synergies = new List<SynergyEffect>(Synergies);
            copy.HitInstances = new HashSet<int>(HitInstances);
            copy.Consumed = false;
            copy.KilledThisHit = false;
            copy.OverkillAmount = 0f;
            return copy;
        }
    }
}
