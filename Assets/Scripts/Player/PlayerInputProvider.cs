using UnityEngine;

namespace AIWE.Player
{
    public class PlayerInputProvider : MonoBehaviour
    {
        public Controls Controls { get; private set; }

        private void Awake()
        {
            Controls = new Controls();
        }

        public void SetPlayerMapEnabled(bool enabled)
        {
            if (enabled)
                Controls.Player.Enable();
            else
                Controls.Player.Disable();
        }

        private void OnDestroy()
        {
            Controls?.Dispose();
        }
    }
}
