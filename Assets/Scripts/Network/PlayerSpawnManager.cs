using System.Linq;
using AIWE.LevelDesign;
using UnityEngine;

namespace AIWE.Network
{
    public class PlayerSpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Vector3 fallbackSpawnPosition = new(16, 1, 4);

        private int _nextSpawnIndex;

        private void Awake()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                FindLDtkSpawnPoints();
        }

        private void FindLDtkSpawnPoints()
        {
            var markers = FindObjectsByType<PlayerSpawnMarker>(FindObjectsSortMode.None)
                .OrderBy(m => m.PlayerIndex)
                .ToArray();

            if (markers.Length > 0)
            {
                spawnPoints = markers.Select(m => m.transform).ToArray();
                Debug.Log($"[PlayerSpawnManager] Found {spawnPoints.Length} LDtk spawn points");
            }
        }

        public Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                FindLDtkSpawnPoints();

            if (spawnPoints == null || spawnPoints.Length == 0)
                return fallbackSpawnPosition;

            var pos = spawnPoints[_nextSpawnIndex].position;
            _nextSpawnIndex = (_nextSpawnIndex + 1) % spawnPoints.Length;
            return pos;
        }
    }
}
