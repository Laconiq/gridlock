using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AIWE.Player
{
    public class InteractionHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private Image keyBackground;
        [SerializeField] private TextMeshProUGUI keyText;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private Image progressFill;

        [Header("Colors")]
        [SerializeField] private Color activeColor = new(0.9f, 0.9f, 0.9f);
        [SerializeField] private Color inactiveColor = new(0.4f, 0.4f, 0.4f);
        [SerializeField] private Color lockedColor = new(0.7f, 0.2f, 0.2f);

        private bool _isShowing;

        private void Awake()
        {
            if (promptRoot != null) promptRoot.SetActive(false);
        }

        public void Show(string text, bool canInteract)
        {
            if (promptRoot == null) return;

            promptRoot.SetActive(true);
            _isShowing = true;

            if (actionText != null)
                actionText.text = text;

            if (canInteract)
            {
                if (keyBackground != null) keyBackground.color = activeColor;
                if (keyText != null) keyText.color = Color.black;
                if (actionText != null) actionText.color = activeColor;
            }
            else
            {
                if (keyBackground != null) keyBackground.color = lockedColor;
                if (keyText != null) keyText.color = new Color(0.3f, 0.1f, 0.1f);
                if (actionText != null) actionText.color = inactiveColor;
            }
        }

        public void Hide()
        {
            if (promptRoot != null) promptRoot.SetActive(false);
            _isShowing = false;
        }
    }
}
