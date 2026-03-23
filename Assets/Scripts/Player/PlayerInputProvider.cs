using Unity.Netcode;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerInputProvider : NetworkBehaviour
    {
        public Controls Controls { get; private set; }

        private void Awake()
        {
            Controls = new Controls();
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
                return;
            }

            Controls.Player.Enable();
        }

        public override void OnNetworkDespawn()
        {
            if (!IsOwner) return;
            Controls.Player.Disable();
        }

        public void SetPlayerMapEnabled(bool enabled)
        {
            if (enabled)
                Controls.Player.Enable();
            else
                Controls.Player.Disable();
        }

        public override void OnDestroy()
        {
            Controls?.Dispose();
            base.OnDestroy();
        }
    }
}
