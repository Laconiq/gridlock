using System.Linq;
using AIWE.LevelDesign;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Network
{
    public class PlayerSpawnManager : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;

        private int _nextSpawnIndex;

        private void Start()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }

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

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

            var spawnPos = GetNextSpawnPosition();
            if (client.PlayerObject != null)
            {
                client.PlayerObject.transform.position = spawnPos;
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return new Vector3(16, 1, 4);

            var pos = spawnPoints[_nextSpawnIndex].position;
            _nextSpawnIndex = (_nextSpawnIndex + 1) % spawnPoints.Length;
            return pos;
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            }
        }
    }
}
