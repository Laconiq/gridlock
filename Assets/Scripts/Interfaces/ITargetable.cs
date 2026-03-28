using UnityEngine;

namespace Gridlock.Interfaces
{
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
        Transform Transform { get; }
    }
}
