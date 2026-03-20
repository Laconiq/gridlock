using LDtkUnity;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public enum SpawnType { Ground, Air, Boss }

    public class EnemySpawnerMarker : MonoBehaviour, ILDtkImportedFields
    {
        [SerializeField] private SpawnType spawnType;
        [SerializeField] private int waveGroup;
        public SpawnType SpawnTypeValue => spawnType;
        public int WaveGroup => waveGroup;
        public void OnLDtkImportFields(LDtkFields fields) { spawnType = fields.GetEnum<SpawnType>("spawn_type"); waveGroup = fields.GetInt("wave_group"); }
    }
}
