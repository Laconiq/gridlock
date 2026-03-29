using Gridlock.Core;
using Gridlock.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.HUD
{
    [AddComponentMenu("Gridlock/UI/Game Over Screen")]
    [RequireComponent(typeof(UIDocument))]
    public class GameOverScreen : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        private Label _waveLabel;
        private Label _killsLabel;
        private Label _timeLabel;
        private Label _logIdLabel;
        private Button _rebootButton;
        private Button _disconnectButton;
        private float _sessionStartTime;
        private Coroutine _waitCoroutine;
        private bool _subscribedToGameManager;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
            _sessionStartTime = Time.time;
        }

        private void OnEnable()
        {
            var root = _uiDocument.rootVisualElement;
            root.pickingMode = PickingMode.Ignore;
            if (root.parent != null)
                root.parent.pickingMode = PickingMode.Ignore;
            _root = root.Q("gameover-root");
            _waveLabel = root.Q<Label>("gameover-wave");
            _killsLabel = root.Q<Label>("gameover-kills");
            _timeLabel = root.Q<Label>("gameover-time");
            _logIdLabel = root.Q<Label>("gameover-log-id");
            _rebootButton = root.Q<Button>("gameover-reboot");
            _disconnectButton = root.Q<Button>("gameover-disconnect");

            if (_root != null)
            {
                var gridColor = new Color(0.56f, 0.96f, 1f, 0.03f);
                var grid = new GridBackground(40f, gridColor);
                _root.Insert(0, grid);
            }

            if (_rebootButton != null)
                _rebootButton.clicked += OnRebootClicked;
            if (_disconnectButton != null)
                _disconnectButton.clicked += OnDisconnectClicked;

            _waitCoroutine = StartCoroutine(WaitForGameManager());
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
                yield return null;

            GameManager.Instance.OnStateChanged += OnStateChanged;
            _subscribedToGameManager = true;
            _waitCoroutine = null;
        }

        private void OnDisable()
        {
            if (_waitCoroutine != null)
            {
                StopCoroutine(_waitCoroutine);
                _waitCoroutine = null;
            }

            if (_rebootButton != null)
                _rebootButton.clicked -= OnRebootClicked;
            if (_disconnectButton != null)
                _disconnectButton.clicked -= OnDisconnectClicked;

            if (_subscribedToGameManager)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                    gm.OnStateChanged -= OnStateChanged;
                _subscribedToGameManager = false;
            }
        }

        private void OnStateChanged(GameState previous, GameState current)
        {
            if (current == GameState.GameOver)
                Show();
            else
                Hide();
        }

        private void Show()
        {
            if (_root == null) return;

            var wm = FindAnyObjectByType<WaveManager>();
            if (_waveLabel != null)
                _waveLabel.text = $"{(wm != null ? wm.CurrentWave + 1 : 0):D2}";

            int totalKills = GameStats.Instance != null ? GameStats.Instance.TotalKills : 0;
            if (_killsLabel != null)
                _killsLabel.text = $"{totalKills:D2}";

            float elapsed = Time.time - _sessionStartTime;
            int minutes = (int)(elapsed / 60f);
            int seconds = (int)(elapsed % 60f);
            if (_timeLabel != null)
                _timeLabel.text = $"{minutes:D2}:{seconds:D2}";

            if (_logIdLabel != null)
                _logIdLabel.text = $"LOG_ID::VP_FAIL_{Random.Range(100, 999)}_{Random.Range(10, 99)}";

            _root.style.display = DisplayStyle.Flex;
            _root.pickingMode = PickingMode.Position;

            var hud = FindAnyObjectByType<GameHUD>();
            if (hud != null) hud.SetVisible(false);
        }

        private void Hide()
        {
            if (_root == null) return;
            _root.style.display = DisplayStyle.None;
            _root.pickingMode = PickingMode.Ignore;

            var hud = FindAnyObjectByType<GameHUD>();
            if (hud != null) hud.SetVisible(true);
        }

        private void OnRebootClicked()
        {
            GameManager.Instance?.RequestResetGame();
        }

        private void OnDisconnectClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
