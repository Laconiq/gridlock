using UnityEngine;
using Gridlock.Interfaces;

namespace Gridlock.Mods.Pipeline
{
    public struct SpawnRequest
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public ModPipeline Pipeline;
        public float DamageScale;
        public ITargetable Target;

        public static Vector3 RandomDirectionExcluding(Vector3 parentDir, float excludeHalfAngle = 10f)
        {
            float parentAngle = Mathf.Atan2(parentDir.z, parentDir.x) * Mathf.Rad2Deg;
            float offset = Random.Range(excludeHalfAngle, 360f - excludeHalfAngle);
            float angle = (parentAngle + offset) * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }
}
