using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Mods
{
    public struct ProjectileConfig
    {
        // Base stats (modified by traits)
        public float damage;
        public float speed;
        public float size;
        public float lifetime;

        // Behavior flags
        public bool homing;
        public bool pierce;
        public int pierceCount;
        public bool bounce;
        public int bounceCount;
        public bool split;
        public int splitCount;
        public bool wide;
        public float wideRadius;

        // Elemental flags
        public bool burn;
        public bool frost;
        public bool shock;
        public bool isVoid;
        public bool leech;

        // Synergy-driven flags
        public bool napalm;
        public bool avalanche;
        public bool ricochet;
        public bool thermalShock;
        public bool siphon;

        // Sub-projectile configs (from events)
        public List<EventStage> eventStages;

        public static ProjectileConfig Default(float baseDamage)
        {
            return new ProjectileConfig
            {
                damage = baseDamage,
                speed = 20f,
                size = 0.2f,
                lifetime = 5f,
                pierceCount = 3,
                bounceCount = 3,
                splitCount = 3,
                wideRadius = 2f,
            };
        }

        public ProjectileConfig DeepCopy()
        {
            var copy = this;
            if (eventStages != null)
            {
                copy.eventStages = new List<EventStage>(eventStages.Count);
                foreach (var stage in eventStages)
                {
                    copy.eventStages.Add(new EventStage
                    {
                        eventType = stage.eventType,
                        subProjectile = stage.subProjectile.DeepCopy()
                    });
                }
            }
            return copy;
        }
    }

    public struct EventStage
    {
        public ModType eventType;
        public ProjectileConfig subProjectile;
    }
}
