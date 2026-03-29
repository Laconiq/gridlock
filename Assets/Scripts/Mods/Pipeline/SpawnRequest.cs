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
    }
}
