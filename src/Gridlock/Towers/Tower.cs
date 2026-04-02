using System.Numerics;
using Gridlock.Mods;

namespace Gridlock.Towers
{
    public sealed class Tower
    {
        private static int _nextId;

        public int EntityId { get; }
        public Vector3 Position { get; set; }
        public TowerData Data { get; }
        public ModSlotExecutor Executor { get; }

        public Vector3 FirePoint => Position + new Vector3(0f, 0.5f, 0f);

        public Tower(TowerData data, Vector3 position)
        {
            EntityId = _nextId++;
            Data = data;
            Position = position;
            Executor = new ModSlotExecutor(this);
        }

        public void Update(float dt)
        {
            Executor.Update(dt);
        }

        public static void ResetIdCounter() => _nextId = 0;
    }
}
