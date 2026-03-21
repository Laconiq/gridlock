using AIWE.Core;
using AIWE.Network;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.UI.Lobby
{
    [RequireComponent(typeof(UIDocument))]
    public class LobbyScreen : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;

        private VisualElement _connectSection;
        private VisualElement _lobbySection;

        private Button _hostBtn;
        private Button _joinBtn;
        private TextField _joinCodeInput;
        private Label _statusText;

        private Label _lobbyCodeDisplay;
        private Label _playerCount;
        private Button _startBtn;
        private Button _disconnectBtn;

        private VisualElement _connectingOverlay;
        private Label _connectingStatus;
        private IVisualElementScheduledItem _connectingDotSchedule;
        private string _connectingBaseText;

        private NetworkBootstrap _bootstrap;
        private bool _subscribedToGameState;

        private EventCallback<ClickEvent> _onHostClick;
        private EventCallback<ClickEvent> _onJoinClick;
        private EventCallback<ClickEvent> _onStartClick;
        private EventCallback<ClickEvent> _onDisconnectClick;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _connectSection = _root.Q("connect-section");
            _lobbySection = _root.Q("lobby-section");

            _hostBtn = _root.Q<Button>("host-btn");
            _joinBtn = _root.Q<Button>("join-btn");
            _joinCodeInput = _root.Q<TextField>("join-code-input");
            _statusText = _root.Q<Label>("status-text");

            _lobbyCodeDisplay = _root.Q<Label>("lobby-code-display");
            _playerCount = _root.Q<Label>("player-count");
            _startBtn = _root.Q<Button>("start-btn");
            _disconnectBtn = _root.Q<Button>("disconnect-btn");

            _connectingOverlay = _root.Q("connecting-overlay");
            _connectingStatus = _root.Q<Label>("connecting-status");

            _onHostClick = _ => OnHostClicked();
            _onJoinClick = _ => OnJoinClicked();
            _onStartClick = _ => OnStartClicked();
            _onDisconnectClick = _ => OnDisconnectClicked();

            _hostBtn?.RegisterCallback(_onHostClick);
            _joinBtn?.RegisterCallback(_onJoinClick);
            _startBtn?.RegisterCallback(_onStartClick);
            _disconnectBtn?.RegisterCallback(_onDisconnectClick);

            InjectGridBackground();
            ShowConnect();
        }

        private void OnDisable()
        {
            _hostBtn?.UnregisterCallback(_onHostClick);
            _joinBtn?.UnregisterCallback(_onJoinClick);
            _startBtn?.UnregisterCallback(_onStartClick);
            _disconnectBtn?.UnregisterCallback(_onDisconnectClick);
        }

        private void InjectGridBackground()
        {
            var gridOverlay = _root.Q("grid-overlay");
            if (gridOverlay == null) return;

            var gridColor = NodeEditor.UI.DesignConstants.Primary;
            gridColor.a = 0.04f;
            var grid = new GridBackground(color: gridColor);
            gridOverlay.Add(grid);
        }

        private void Start()
        {
            _bootstrap = ServiceLocator.Get<NetworkBootstrap>();
            if (_bootstrap == null) return;

            _bootstrap.OnHostStarted += OnConnected;
            _bootstrap.OnClientStarted += OnConnected;
            _bootstrap.OnError += OnConnectionError;
        }

        private void Update()
        {
            if (!_subscribedToGameState)
                TrySubscribeToGameState();

            if (_lobbySection != null
                && _lobbySection.resolvedStyle.display == DisplayStyle.Flex)
            {
                UpdatePlayerCount();
            }
        }

        private void TrySubscribeToGameState()
        {
            if (_subscribedToGameState || GameManager.Instance == null) return;
            GameManager.Instance.CurrentState.OnValueChanged += OnGameStateChanged;
            _subscribedToGameState = true;

            if (GameManager.Instance.CurrentState.Value != GameState.Lobby)
                Hide();
        }

        private void OnGameStateChanged(GameState prev, GameState current)
        {
            if (current != GameState.Lobby)
                Hide();
        }

        private void OnHostClicked()
        {
            if (_bootstrap == null) return;
            ShowConnecting("ESTABLISHING_NODE");
            _bootstrap.HostGame();
        }

        private void OnJoinClicked()
        {
            if (_bootstrap == null) return;

            var code = _joinCodeInput?.value?.Trim().ToUpperInvariant();
            if (string.IsNullOrEmpty(code))
            {
                SetStatus("ENTER_LOBBY_CODE", true);
                return;
            }

            ShowConnecting("CONNECTING");
            _bootstrap.JoinGame(code);
        }

        private void OnConnected()
        {
            ShowLobby();
        }

        private void OnConnectionError(string error)
        {
            ShowConnect();
            SetStatus($"ERROR: {error}", true);
        }

        private void OnStartClicked()
        {
            GameManager.Instance?.SetState(GameState.Preparing);
        }

        private void OnDisconnectClicked()
        {
            _bootstrap?.Disconnect();
            ShowConnect();
        }

        private void ShowConnecting(string message)
        {
            _connectingBaseText = message;

            if (_connectingOverlay != null)
                _connectingOverlay.AddToClassList("lobby__connecting-overlay--visible");

            if (_connectingStatus != null)
                _connectingStatus.text = message + ".";

            int dotCount = 0;
            _connectingDotSchedule = _root.schedule.Execute(() =>
            {
                dotCount = (dotCount % 3) + 1;
                if (_connectingStatus != null)
                    _connectingStatus.text = _connectingBaseText + new string('.', dotCount);
            }).Every(400);
        }

        private void HideConnecting()
        {
            _connectingOverlay?.RemoveFromClassList("lobby__connecting-overlay--visible");
            _connectingDotSchedule?.Pause();
            _connectingDotSchedule = null;
        }

        private void ShowConnect()
        {
            HideConnecting();

            if (_connectSection != null)
                _connectSection.style.display = DisplayStyle.Flex;
            if (_lobbySection != null)
                _lobbySection.RemoveFromClassList("lobby__connected-section--visible");

            SetButtonsEnabled(true);
            SetStatus("", false);
        }

        private void ShowLobby()
        {
            HideConnecting();

            if (_connectSection != null)
                _connectSection.style.display = DisplayStyle.None;

            if (_lobbySection != null)
                _lobbySection.AddToClassList("lobby__connected-section--visible");

            if (_lobbyCodeDisplay != null)
                _lobbyCodeDisplay.text = _bootstrap?.LobbyCode ?? "------";

            var isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
            if (_startBtn != null)
                _startBtn.style.display = isHost ? DisplayStyle.Flex : DisplayStyle.None;

            UpdatePlayerCount();
        }

        private void Hide()
        {
            if (_root != null)
                _root.style.display = DisplayStyle.None;
        }

        private void UpdatePlayerCount()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || !nm.IsListening || _playerCount == null) return;
            _playerCount.text = $"OPERATORS: {nm.ConnectedClientsList.Count} / 4";
        }

        private void SetStatus(string text, bool isError)
        {
            if (_statusText == null) return;
            _statusText.text = text;

            _statusText.RemoveFromClassList("lobby__status--visible");
            _statusText.RemoveFromClassList("lobby__status--error");
            _statusText.RemoveFromClassList("lobby__status--info");

            if (!string.IsNullOrEmpty(text))
            {
                _statusText.AddToClassList("lobby__status--visible");
                _statusText.AddToClassList(isError ? "lobby__status--error" : "lobby__status--info");
            }
        }

        private void SetButtonsEnabled(bool enabled)
        {
            if (_hostBtn != null) _hostBtn.SetEnabled(enabled);
            if (_joinBtn != null) _joinBtn.SetEnabled(enabled);
            if (_joinCodeInput != null) _joinCodeInput.SetEnabled(enabled);
        }

        private void OnDestroy()
        {
            _connectingDotSchedule?.Pause();

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
