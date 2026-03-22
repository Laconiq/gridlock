using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.Player
{
    [RequireComponent(typeof(UIDocument))]
    public class InteractionHUD : MonoBehaviour
    {
        private UIDocument _uiDocument;
        private VisualElement _prompt;
        private VisualElement _keyContainer;
        private VisualElement _keyFill;
        private Label _keyLabel;
        private Label _actionText;
        private Label _holdHint;

        private bool _canInteract;

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;
            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _prompt = root.Q("prompt");
            _keyContainer = root.Q("key-container");
            _keyFill = root.Q("key-fill");
            _keyLabel = root.Q<Label>("key-label");
            _actionText = root.Q<Label>("action-text");
            _holdHint = root.Q<Label>("hold-hint");

            Hide();
        }

        public void Show(string text, bool canInteract)
        {
            _canInteract = canInteract;

            _prompt?.AddToClassList("interaction__prompt--visible");

            if (_actionText != null)
                _actionText.text = text;

            _keyContainer?.RemoveFromClassList("interaction__key--active");
            _keyContainer?.RemoveFromClassList("interaction__key--locked");
            _actionText?.RemoveFromClassList("interaction__action--active");
            _actionText?.RemoveFromClassList("interaction__action--locked");

            if (canInteract)
            {
                _keyContainer?.AddToClassList("interaction__key--active");
                _actionText?.AddToClassList("interaction__action--active");
                _holdHint?.AddToClassList("interaction__hold-hint--visible");
            }
            else
            {
                _keyContainer?.AddToClassList("interaction__key--locked");
                _actionText?.AddToClassList("interaction__action--locked");
                _holdHint?.RemoveFromClassList("interaction__hold-hint--visible");
            }

            SetProgress(0f);
        }

        public void Hide()
        {
            _prompt?.RemoveFromClassList("interaction__prompt--visible");
            _holdHint?.RemoveFromClassList("interaction__hold-hint--visible");
            SetProgress(0f);
        }

        public void SetProgress(float progress)
        {
            if (_keyFill == null) return;
            float clamped = Mathf.Clamp01(progress);
            _keyFill.style.height = Length.Percent(clamped * 100f);
        }
    }
}
