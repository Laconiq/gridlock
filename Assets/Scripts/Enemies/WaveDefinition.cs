using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Enemies
{
    [CreateAssetMenu(menuName = "Gridlock/Wave Definition")]
    public class WaveDefinition : ScriptableObject
    {
        [Serializable]
        public class SpawnEntry
        {
            public EnemyDefinition enemy;
            public int count = 5;
            public float spawnInterval = 0.5f;
            public float delayBeforeGroup;
        }

        public List<SpawnEntry> entries = new();
    }
}
