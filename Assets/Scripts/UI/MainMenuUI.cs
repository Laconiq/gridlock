using AIWE.Network;
using AIWE.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AIWE.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject lobbyPanel;

        [Header("Main Menu")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private TMP_InputField joinCodeInput;
        [SerializeField] private TextMeshProUGUI statusText;

        [Header("Lobby")]
        [SerializeField] private TextMeshProUGUI lobbyCodeText;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button disconnectButton;

        private NetworkBootstrap _bootstrap;

        private void Start()
        {
            _bootstrap = ServiceLocator.Get<NetworkBootstrap>();

            joinCodeInput.onValueChanged.AddListener(v => joinCodeInput.text = v.ToUpperInvariant());
            hostButton.onClick.AddListener(OnHostClicked);
            joinButton.onClick.AddListener(OnJoinClicked);
            startGameButton.onClick.AddListener(OnStartGameClicked);
            disconnectButton.onClick.AddListener(OnDisconnectClicked);

            _bootstrap.OnHostStarted += OnConnected;
            _bootstrap.OnClientStarted += OnConnected;
            _bootstrap.OnError += OnConnectionError;

            ShowMainMenu();
        }

        private bool _subscribedToGameState;

        private void SubscribeToGameState()
        {
            if (_subscribedToGameState || GameManager.Instance == null) return;
            GameManager.Instance.CurrentState.OnValueChanged += OnGameStateChanged;
            _subscribedToGameState = true;

            if (GameManager.Instance.CurrentState.Value != GameState.Lobby)
            {
                mainMenuPanel.SetActive(false);
                lobbyPanel.SetActive(false);
            }
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (current != GameState.Lobby)
            {
                mainMenuPanel.SetActive(false);
                lobbyPanel.SetActive(false);
            }
        }

        private void OnHostClicked()
        {
            SetStatus("Creating game...");
            SetButtonsInteractable(false);
            _bootstrap.HostGame();
        }

        private void OnJoinClicked()
        {
            var code = joinCodeInput.text.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("Enter a lobby code");
                return;
            }
            SetStatus("Joining...");
            SetButtonsInteractable(false);
            _bootstrap.JoinGame(code);
        }

        private void OnConnected()
        {
            ShowLobby();
        }

        private void OnConnectionError(string error)
        {
            SetStatus($"Error: {error}");
            SetButtonsInteractable(true);
        }

        private void OnStartGameClicked()
        {
            GameManager.Instance?.SetState(GameState.Preparing);
            lobbyPanel.SetActive(false);
        }

        private void OnDisconnectClicked()
        {
            _bootstrap.Disconnect();
            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            mainMenuPanel.SetActive(true);
            lobbyPanel.SetActive(false);
            SetButtonsInteractable(true);
            SetStatus("");
        }

        private void ShowLobby()
        {
            mainMenuPanel.SetActive(false);
            lobbyPanel.SetActive(true);

            lobbyCodeText.text = $"Code: {_bootstrap.LobbyCode}";

            var isHost = Unity.Netcode.NetworkManager.Singleton.IsHost;
            startGameButton.gameObject.SetActive(isHost);
        }

        private void Update()
        {
            if (!_subscribedToGameState)
                SubscribeToGameState();

            if (lobbyPanel.activeSelf)
            {
                var nm = Unity.Netcode.NetworkManager.Singleton;
                if (nm != null && nm.IsListening)
                {
                    playerCountText.text = $"Players: {nm.ConnectedClientsList.Count} / 4";
                }
            }
        }

        private void SetStatus(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }

        private void SetButtonsInteractable(bool interactable)
        {
            hostButton.interactable = interactable;
            joinButton.interactable = interactable;
        }

        private void OnDestroy()
        {
            if (_bootstrap != null)
            {
                _bootstrap.OnHostStarted -= OnConnected;
                _bootstrap.OnClientStarted -= OnConnected;
                _bootstrap.OnError -= OnConnectionError;
            }
            if (_subscribedToGameState && GameManager.Instance != null)
                GameManager.Instance.CurrentState.OnValueChanged -= OnGameStateChanged;
        }
    }
}
