using System;
using System.Threading.Tasks;
using AIWE.Core;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace AIWE.Network
{
    public class NetworkBootstrap : MonoBehaviour
    {
        [SerializeField] private int maxPlayers = 4;

        private LobbyManager _lobbyManager;

        public string LobbyCode => _lobbyManager?.CurrentLobby?.LobbyCode;
        public string RelayJoinCode { get; private set; }

        public event Action OnHostStarted;
        public event Action OnClientStarted;
        public event Action<string> OnError;

        private void Awake()
        {
            _lobbyManager = GetComponent<LobbyManager>();
            if (_lobbyManager == null)
                _lobbyManager = gameObject.AddComponent<LobbyManager>();

            ServiceLocator.Register(this);
        }

        private async Task InitializeServices()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"[NetworkBootstrap] Signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
        }

        public async void HostGame()
        {
            try
            {
                await InitializeServices();

                RelayJoinCode = await RelayManager.CreateRelay(maxPlayers - 1);

                await _lobbyManager.CreateLobby("AIWE Game", maxPlayers, RelayJoinCode);

                NetworkManager.Singleton.StartHost();
                Debug.Log($"[NetworkBootstrap] Host started. Lobby code: {LobbyCode}");

                OnHostStarted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkBootstrap] Host failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
        }

        public async void JoinGame(string lobbyCode)
        {
            try
            {
                await InitializeServices();

                var lobby = await _lobbyManager.JoinLobbyByCode(lobbyCode);
                var relayCode = _lobbyManager.GetRelayJoinCode();

                if (string.IsNullOrEmpty(relayCode))
                {
                    throw new Exception("Relay join code not found in lobby data");
                }

                await RelayManager.JoinRelay(relayCode);

                NetworkManager.Singleton.StartClient();
                Debug.Log($"[NetworkBootstrap] Client started. Joined lobby: {lobbyCode}");

                OnClientStarted?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkBootstrap] Join failed: {e.Message}");
                OnError?.Invoke(e.Message);
            }
        }

        public async void Disconnect()
        {
            NetworkManager.Singleton.Shutdown();
            await _lobbyManager.LeaveLobby();
            Debug.Log("[NetworkBootstrap] Disconnected");
        }

        private void OnDestroy()
        {
            ServiceLocator.Unregister<NetworkBootstrap>();
        }
    }
}
