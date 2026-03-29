using Gridlock.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class WaveStartUI : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _root;
        private Button _startButton;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _root.pickingMode = PickingMode.Ignore;
            if (_root.parent != null)
                _root.parent.pickingMode = PickingMode.Ignore;

            _startButton = _root.Q<Button>("start-wave-btn");
            if (_startButton != null)
                _startButton.clicked += OnStartWaveClicked;

            StartCoroutine(WaitForGameManager());
        }

        private System.Collections.IEnumerator WaitForGameManager()
        {
            while (GameManager.Instance == null)
                yield return null;

            GameManager.Instance.OnStateChanged += OnStateChanged;
            OnStateChanged(GameState.Preparing, GameManager.Instance.CurrentState);
        }

        private void OnDisable()
        {
            if (_startButton != null)
                _startButton.clicked -= OnStartWaveClicked;

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= OnStateChanged;
        }

        private void OnStateChanged(GameState prev, GameState current)
        {
            if (_startButton == null) return;

            bool show = current == GameState.Preparing;
            _startButton.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void OnStartWaveClicked()
        {
            GameManager.Instance?.SetState(GameState.Wave);
        }
    }
}
