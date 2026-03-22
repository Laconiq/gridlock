using System.Collections;
using AIWE.Core;
using AIWE.Network;
using AIWE.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.HUD
{
    [AddComponentMenu("AIWE/UI/Game HUD")]
    [RequireComponent(typeof(UIDocument))]
    public class GameHUD : MonoBehaviour
    {
        public static GameHUD Instance { get; private set; }

        [SerializeField] private float playerPollInterval = 0.25f;

        private UIDocument _uiDocument;
        private VisualElement _root;

        private HUDPlayerStatus _playerStatus;
        private HUDSquadFeed _squadFeed;
        private HUDEventLog _eventLog;
        private HUDSystemInfo _systemInfo;
        private HUDWaveInfo _waveInfo;

        private Label _readyPrompt;
        private Label _announcementLabel;
        private Coroutine _announcementHideCoroutine;
        private VisualElement _spectateOverlay;
        private Label _spectateTarget;
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

            _readyPrompt = _root.Q<Label>("ready-prompt");
            if (_readyPrompt != null)
                _readyPrompt.style.display = DisplayStyle.None;

            _announcementLabel = _root.Q<Label>("announcement-label");
            if (_announcementLabel != null)
                _announcementLabel.style.display = DisplayStyle.None;

            _squadFeed = new HUDSquadFeed(_root.Q("squad-feed"));

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

            _spectateOverlay = _root.Q("spectate-overlay");
            _spectateTarget = _root.Q<Label>("spectate-target");

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
                var nm = NetworkManager.Singleton;
                if (nm != null && nm.LocalClient != null && nm.LocalClient.PlayerObject != null)
                {
                    var playerObj = nm.LocalClient.PlayerObject;
                    var health = playerObj.GetComponent<PlayerHealth>();
                    var data = playerObj.GetComponent<PlayerData>();
                    var controller = playerObj.GetComponent<PlayerController>();

                    if (health != null && data != null && controller != null)
                    {
                        _playerStatus.Bind(health, data);
                        _systemInfo.Bind(controller);
                        _eventLog.BindGameEvents();
                        break;
                    }
                }

                yield return wait;
            }
        }

        private void Update()
        {
            int phase = _frameCounter % 4;

            switch (phase)
            {
                case 0: _playerStatus?.Refresh(); break;
                case 1: _squadFeed?.Refresh(); break;
                case 2: _systemInfo?.Refresh(); break;
                case 3: _waveInfo?.Refresh(); break;
            }

            UpdateReadyPrompt();
            _frameCounter++;
        }

        private void UpdateReadyPrompt()
        {
            if (_readyPrompt == null) return;

            var gm = GameManager.Instance;
            bool preparing = gm != null && gm.CurrentState.Value == GameState.Preparing;

            var radial = RadialMenu.RadialMenuScreen.Instance;
            var rm = ReadyManager.Instance;
            bool counting = rm != null && rm.IsCountingDown;

            if (!preparing || counting || (radial != null && radial.IsOpen))
            {
                _readyPrompt.style.display = DisplayStyle.None;
                return;
            }

            var nm = NetworkManager.Singleton;
            bool selfReady = rm != null && nm != null && rm.IsPlayerReady(nm.LocalClientId);

            _readyPrompt.style.display = DisplayStyle.Flex;
            _readyPrompt.text = selfReady ? "WAITING FOR SQUAD..." : "PRESS [F] TO READY UP";
        }

        public void ShowAnnouncement(string text, float autoHideDuration = 0f)
        {
            if (_announcementLabel == null) return;

            if (_announcementHideCoroutine != null)
                StopCoroutine(_announcementHideCoroutine);

            _announcementLabel.text = text;
            _announcementLabel.style.display = DisplayStyle.Flex;

            if (_readyPrompt != null)
                _readyPrompt.style.display = DisplayStyle.None;

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

        public void ShowSpectateOverlay(string playerName)
        {
            if (_spectateOverlay == null) return;
            _spectateOverlay.style.display = DisplayStyle.Flex;
            UpdateSpectateTarget(playerName);
        }

        public void HideSpectateOverlay()
        {
            if (_spectateOverlay != null)
                _spectateOverlay.style.display = DisplayStyle.None;
        }

        public void UpdateSpectateTarget(string name)
        {
            if (_spectateTarget != null)
                _spectateTarget.text = $"SPECTATING: {name}";
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
