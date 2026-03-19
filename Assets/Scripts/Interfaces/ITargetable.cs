using UnityEngine;

namespace AIWE.Interfaces
{
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
        Transform Transform { get; }
    }
}
