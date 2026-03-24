using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace AIWE.Network
{
    public class LobbyManager : MonoBehaviour
    {
        private Lobby _currentLobby;
        private float _heartbeatTimer;
        private const float HeartbeatInterval = 15f;
        private bool _isHost;

        public Lobby CurrentLobby => _currentLobby;
        public string JoinCode => _currentLobby?.Data?["RelayJoinCode"]?.Value;

        public async Task<Lobby> CreateLobby(string lobbyName, int maxPlayers, string relayJoinCode)
        {
            var options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayJoinCode", new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            };

            _currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            _isHost = true;
            Debug.Log($"[LobbyManager] Lobby created: {_currentLobby.LobbyCode}");
            return _currentLobby;
        }

        public async Task<Lobby> JoinLobbyByCode(string lobbyCode)
        {
            _currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            Debug.Log($"[LobbyManager] Joined lobby: {lobbyCode}");
            return _currentLobby;
        }

        public string GetRelayJoinCode()
        {
            if (_currentLobby?.Data != null &&
                _currentLobby.Data.TryGetValue("RelayJoinCode", out var data))
            {
                return data.Value;
            }
            return null;
        }

        private void Update()
        {
            if (_currentLobby == null || !_isHost) return;

            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= HeartbeatInterval)
            {
                _heartbeatTimer = 0;
                SendHeartbeat();
            }
        }

        private async void SendHeartbeat()
        {
            if (_currentLobby == null) return;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[LobbyManager] Heartbeat failed: {e.Message}");
                _currentLobby = null;
            }
        }

        public async Task LeaveLobby()
        {
            if (_currentLobby == null) return;
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    _currentLobby.Id,
                    Unity.Services.Authentication.AuthenticationService.Instance.PlayerId
                );
            }
            catch (LobbyServiceException e)
            {
                Debug.LogWarning($"[LobbyManager] Leave failed: {e.Message}");
            }
            _currentLobby = null;
            _isHost = false;
        }

        private void OnDestroy()
        {
            if (_currentLobby != null)
            {
                _ = LeaveLobby();
            }
        }
    }
}
