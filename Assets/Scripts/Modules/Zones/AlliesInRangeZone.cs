using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Player;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class AlliesInRangeZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var nm = NetworkManager.Singleton;
            if (nm == null) return result;

            foreach (var client in nm.ConnectedClientsList)
            {
                var playerObj = client.PlayerObject;
                if (playerObj == null) continue;

                var health = playerObj.GetComponent<PlayerHealth>();
                if (health == null || !health.IsAlive) continue;

                if (Vector3.Distance(origin, playerObj.transform.position) > range) continue;

                result.Add(new AllyTarget(health));
            }

            return result;
        }

        public override ZoneInstance CreateInstance()
        {
            return new AlliesInRangeZone { cooldown = cooldown };
        }

        private class AllyTarget : ITargetable
        {
            public Vector3 Position { get; }
            public bool IsAlive => _health != null && _health.IsAlive;
            public Transform Transform { get; }

            private readonly PlayerHealth _health;

            public AllyTarget(PlayerHealth health)
            {
                _health = health;
                Position = health.transform.position;
                Transform = health.transform;
            }
        }
    }
}
