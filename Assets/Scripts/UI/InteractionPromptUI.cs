using TMPro;
using UnityEngine;

namespace AIWE.UI
{
    public class InteractionPromptUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI promptText;
        [SerializeField] private CanvasGroup canvasGroup;

        public void Show(string text, bool interactable)
        {
            gameObject.SetActive(true);
            if (promptText != null)
                promptText.text = text;
            if (canvasGroup != null)
                canvasGroup.alpha = interactable ? 1f : 0.5f;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
