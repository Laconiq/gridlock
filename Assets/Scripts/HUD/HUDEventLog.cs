using System;
using Gridlock.Core;
using UnityEngine.UIElements;

namespace Gridlock.HUD
{
    public class HUDEventLog
    {
        private const int MaxEntries = 5;
        private const float OpacityFalloff = 0.7f;

        private readonly VisualElement _container;
        private bool _gameEventsBound;

        public static event Action<string> OnLogMessage;

        public HUDEventLog(VisualElement logContainer)
        {
            _container = logContainer;
            OnLogMessage += HandleLogMessage;
        }

        public static void Log(string message)
        {
            OnLogMessage?.Invoke(message);
        }

        public void BindGameEvents()
        {
            if (_gameEventsBound) return;
            _gameEventsBound = true;

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged += HandleStateChanged;
        }

        public void UnbindGameEvents()
        {
            OnLogMessage -= HandleLogMessage;

            if (!_gameEventsBound) return;
            _gameEventsBound = false;

            var gm = GameManager.Instance;
            if (gm != null)
                gm.OnStateChanged -= HandleStateChanged;
        }

        public void Refresh()
        {
        }

        private void HandleLogMessage(string message)
        {
            if (_container == null) return;

            var entry = new Label { text = $"> {message}" };
            entry.AddToClassList("hud__log-entry");
            _container.Add(entry);

            while (_container.childCount > MaxEntries)
                _container.RemoveAt(0);

            UpdateOpacities();
        }

        private void UpdateOpacities()
        {
            int count = _container.childCount;
            for (int i = 0; i < count; i++)
            {
                int age = count - 1 - i;
                float opacity = 1f;
                for (int j = 0; j < age; j++)
                    opacity *= OpacityFalloff;

                _container[i].style.opacity = opacity;
            }
        }

        private void HandleStateChanged(GameState previous, GameState current)
        {
            Log($"{current.ToString().ToUpperInvariant()}_ACTIVE");
        }
    }
}
