using System.Collections;
using Gridlock.Core;
using Gridlock.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.HUD
{
    [AddComponentMenu("Gridlock/UI/Game HUD")]
    [RequireComponent(typeof(UIDocument))]
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [SerializeField] private float playerPollInterval = 0.25f;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private HUDPlayerStatus _playerStatus;
        private HUDEventLog _eventLog;
        private HUDSystemInfo _systemInfo;
        private HUDWaveInfo _waveInfo;

        private Label _announcementLabel;
        private Coroutine _announcementHideCoroutine;
        private int _frameCounter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _playerStatus = new HUDPlayerStatus(
                _root.Q<Label>("self-name"),
                _root.Q("self-hp-fill"),
                _root.Q<Label>("self-signal"),
                _root.Q<Label>("self-hp-pct"),
                _root.Q<Label>("self-ready")
            );

            _announcementLabel = _root.Q<Label>("announcement-label");
            if (_announcementLabel != null)
                _announcementLabel.style.display = DisplayStyle.None;

            _eventLog = new HUDEventLog(_root.Q("event-log"));

            _systemInfo = new HUDSystemInfo(
                _root.Q("sys-dot"),
                _root.Q<Label>("sys-version"),
                _root.Q<Label>("sys-meta")
            );

            _waveInfo = new HUDWaveInfo(
                _root.Q<Label>("wave-label"),
                _root.Q<Label>("wave-enemies"),
                _root.Q("objective-hp-fill"),
                _root.Q<Label>("objective-hp-pct")
            );

            StartCoroutine(WaitForLocalPlayer());
        }

        private void OnDisable()
        {
            _playerStatus?.Unbind();
            _eventLog?.UnbindGameEvents();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private IEnumerator WaitForLocalPlayer()
        {
            var wait = new WaitForSeconds(playerPollInterval);

            while (true)
            {
                var controller = FindAnyObjectByType<PlayerController>();
                if (controller != null)
                {
                    _playerStatus.Bind();
                    _systemInfo.Bind(controller);
                    _eventLog.BindGameEvents();
                    break;
                }

                yield return wait;
            }
        }

        private void Update()
        {
            int phase = _frameCounter % 3;

            switch (phase)
            {
                case 0: _playerStatus?.Refresh(); break;
                case 1: _systemInfo?.Refresh(); break;
                case 2: _waveInfo?.Refresh(); break;
            }

            _frameCounter++;
        }

        public void ShowAnnouncement(string text, float autoHideDuration = 0f)
        {
            if (_announcementLabel == null) return;

            if (_announcementHideCoroutine != null)
                StopCoroutine(_announcementHideCoroutine);

            _announcementLabel.text = text;
            _announcementLabel.style.display = DisplayStyle.Flex;

            if (autoHideDuration > 0f)
                _announcementHideCoroutine = StartCoroutine(AutoHideAnnouncement(autoHideDuration));
        }

        public void HideAnnouncement()
        {
            if (_announcementLabel == null) return;

            if (_announcementHideCoroutine != null)
            {
                StopCoroutine(_announcementHideCoroutine);
                _announcementHideCoroutine = null;
            }

            _announcementLabel.style.display = DisplayStyle.None;
        }

        private IEnumerator AutoHideAnnouncement(float duration)
        {
            yield return new WaitForSeconds(duration);
            HideAnnouncement();
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
