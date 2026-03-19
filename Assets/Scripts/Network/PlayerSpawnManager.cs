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
        }

        private void OnClientConnected(ulong clientId)
        {
            if (!NetworkManager.Singleton.IsServer) return;

            var spawnPos = GetNextSpawnPosition();
            var player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (player != null)
            {
                player.transform.position = spawnPos;
            }
        }

        private Vector3 GetNextSpawnPosition()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
                return Vector3.zero;

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
