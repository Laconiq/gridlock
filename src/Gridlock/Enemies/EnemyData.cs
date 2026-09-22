using System.Numerics;

namespace Gridlock.Enemies
{
    public sealed class EnemyData
    {
        public string Name { get; set; } = "Enemy";
        public float MaxHP { get; set; } = 100f;
        public float MoveSpeed { get; set; } = 3f;
        public float ObjectiveDamage { get; set; } = 10f;
        public Vector3 Scale { get; set; } = Vector3.One;
        public uint Color { get; set; } = 0xFF0000FF; // RGBA red
    }
}
