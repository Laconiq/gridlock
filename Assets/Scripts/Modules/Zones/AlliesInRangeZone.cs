using System;
using System.Collections.Generic;
using AIWE.Interfaces;
using AIWE.Player;
using UnityEngine;

namespace AIWE.Modules.Zones
{
    [Serializable]
    public class AlliesInRangeZone : ZoneInstance
    {
        public override List<ITargetable> SelectTargets(Vector3 origin, float range)
        {
            var result = new List<ITargetable>();
            var players = UnityEngine.Object.FindObjectsByType<PlayerController>();

            foreach (var player in players)
            {
                if (player == null) continue;
                if (Vector3.Distance(origin, player.transform.position) > range) continue;

                result.Add(new AllyTarget(player));
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
            public bool IsAlive => _player != null;
            public Transform Transform { get; }

            private readonly PlayerController _player;

            public AllyTarget(PlayerController player)
            {
                _player = player;
                Position = player.transform.position;
                Transform = player.transform;
            }
        }
    }
}
