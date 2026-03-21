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
                _root.Q<Label>("self-hp-pct")
            );

            _squadFeed = new HUDSquadFeed(_root.Q("squad-feed"));

            _eventLog = new HUDEventLog(_root.Q("event-log"));

            _systemInfo = new HUDSystemInfo(
                _root.Q("sys-dot"),
                _root.Q<Label>("sys-version"),
                _root.Q<Label>("sys-meta")
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
            int phase = _frameCounter % 3;

            switch (phase)
            {
                case 0: _playerStatus?.Refresh(); break;
                case 1: _squadFeed?.Refresh(); break;
                case 2: _systemInfo?.Refresh(); break;
            }

            _frameCounter++;
        }

        public void SetVisible(bool visible)
        {
            if (_root != null)
                _root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
