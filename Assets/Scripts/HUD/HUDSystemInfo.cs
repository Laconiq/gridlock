using AIWE.Core;
using AIWE.NodeEditor.UI;
using AIWE.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.HUD
{
    public class HUDSystemInfo
    {
        private readonly VisualElement _sysDot;
        private readonly Label _sysVersion;
        private readonly Label _sysMeta;

        private PlayerController _player;

        public HUDSystemInfo(VisualElement sysDot, Label sysVersion, Label sysMeta)
        {
            _sysDot = sysDot;
            _sysVersion = sysVersion;
            _sysMeta = sysMeta;

            if (_sysVersion != null)
                _sysVersion.text = $"v{Application.version}";
        }

        public void Bind(PlayerController player)
        {
            _player = player;
        }

        public void Refresh()
        {
            if (_player == null) return;

            var pos = _player.transform.position;
            if (_sysMeta != null)
                _sysMeta.text = $"GRID::{pos.x:F0}.{pos.z:F0} | ALT::{pos.y:F0}M";

            UpdateStateDot();
        }

        private void UpdateStateDot()
        {
            if (_sysDot == null) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            Color dotColor = gm.CurrentState.Value switch
            {
                GameState.Wave => DesignConstants.Secondary,
                GameState.Preparing => DesignConstants.Primary,
                _ => DesignConstants.Outline
            };

            _sysDot.style.backgroundColor = dotColor;
        }
    }
}
