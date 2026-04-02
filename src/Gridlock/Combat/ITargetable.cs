using System.Numerics;

namespace Gridlock.Combat
{
    public interface ITargetable
    {
        Vector3 Position { get; }
        bool IsAlive { get; }
        int EntityId { get; }
        StatusEffectManager? StatusEffects { get; }
        IDamageable? Damageable { get; }
        float CurrentHP { get; }
        float MaxHP { get; }
    }
}
