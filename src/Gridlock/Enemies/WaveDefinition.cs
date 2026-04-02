using System.Collections.Generic;

namespace Gridlock.Enemies
{
    public sealed class SpawnEntry
    {
        public EnemyData Enemy { get; set; } = new();
        public int Count { get; set; } = 5;
        public float SpawnInterval { get; set; } = 0.5f;
        public float DelayBeforeGroup { get; set; }
    }

    public sealed class WaveDefinition
    {
        public List<SpawnEntry> Entries { get; set; } = new();
    }
}
