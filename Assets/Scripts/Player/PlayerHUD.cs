using TMPro;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerHUD : NetworkBehaviour
    {
        [SerializeField] private GameObject interactionPrompt;
        [SerializeField] private TextMeshProUGUI interactionText;
        [SerializeField] private GameObject crosshair;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                if (interactionPrompt != null) interactionPrompt.SetActive(false);
                if (crosshair != null) crosshair.SetActive(false);
                enabled = false;
                return;
            }
        }

        public void ShowInteractionPrompt(string text, bool canInteract)
        {
            if (interactionPrompt != null)
            {
                interactionPrompt.SetActive(true);
            }
            UpdateInteractionPrompt(text, canInteract);
        }

        public void UpdateInteractionPrompt(string text, bool canInteract)
        {
            if (interactionText != null)
            {
                interactionText.text = text;
                interactionText.color = canInteract ? Color.white : Color.gray;
            }
        }

        public void HideInteractionPrompt()
        {
            if (interactionPrompt != null)
                interactionPrompt.SetActive(false);
        }
    }
}
