using System.Collections.Generic;
using AIWE.Network;
using AIWE.Player;
using Unity.Netcode;
using UnityEngine.UIElements;

namespace AIWE.HUD
{
    public class HUDSquadFeed
    {
        private const string LowClass = "hud__hp-bar-fill--low";
        private const string CriticalClass = "hud__hp-bar-fill--critical";
        private const string DisconnectedClass = "hud__squad-row--disconnected";

        private readonly VisualElement _container;
        private readonly Dictionary<ulong, SquadRow> _rows = new();
        private readonly HashSet<ulong> _activeIds = new();
        private readonly List<ulong> _toRemove = new();

        public HUDSquadFeed(VisualElement squadContainer)
        {
            _container = squadContainer;
        }

        public void Refresh()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null || _container == null) return;

            var localId = nm.LocalClientId;
            _activeIds.Clear();

            foreach (var client in nm.ConnectedClientsList)
            {
                if (client.ClientId == localId) continue;

                _activeIds.Add(client.ClientId);

                var playerObj = client.PlayerObject;
                if (playerObj == null) continue;

                var health = playerObj.GetComponent<PlayerHealth>();
                var data = playerObj.GetComponent<PlayerData>();

                if (!_rows.TryGetValue(client.ClientId, out var row))
                {
                    row = CreateRow(client.ClientId);
                    _rows[client.ClientId] = row;
                    _container.Add(row.Root);
                }

                UpdateRow(row, health, data);
            }

            // Remove disconnected clients
            _toRemove.Clear();
            foreach (var kvp in _rows)
            {
                if (!_activeIds.Contains(kvp.Key))
                    _toRemove.Add(kvp.Key);
            }

            foreach (var id in _toRemove)
            {
                _container.Remove(_rows[id].Root);
                _rows.Remove(id);
            }
        }

        private SquadRow CreateRow(ulong clientId)
        {
            var root = new VisualElement();
            root.AddToClassList("hud__squad-row");

            var header = new VisualElement();
            header.AddToClassList("hud__squad-row-header");

            var nameLabel = new Label { text = $"OPERATOR_{clientId + 1:D2}" };
            nameLabel.AddToClassList("hud__squad-name");

            header.Add(nameLabel);

            var hpTrack = new VisualElement();
            hpTrack.AddToClassList("hud__squad-hp-track");

            var hpFill = new VisualElement();
            hpFill.AddToClassList("hud__squad-hp-fill");
            hpTrack.Add(hpFill);

            root.Add(header);
            root.Add(hpTrack);

            return new SquadRow
            {
                Root = root,
                Name = nameLabel,
                HpFill = hpFill
            };
        }

        private void UpdateRow(SquadRow row, PlayerHealth health, PlayerData data)
        {
            if (data != null)
                row.Name.text = data.DisplayName;

            if (health != null)
            {
                float normalized = health.HPNormalized;
                row.HpFill.style.width = Length.Percent(normalized * 100f);

                row.Root.RemoveFromClassList(DisconnectedClass);

                if (normalized < 0.15f)
                {
                    row.HpFill.AddToClassList(CriticalClass);
                    row.HpFill.AddToClassList(LowClass);
                }
                else if (normalized < 0.30f)
                {
                    row.HpFill.RemoveFromClassList(CriticalClass);
                    row.HpFill.AddToClassList(LowClass);
                }
                else
                {
                    row.HpFill.RemoveFromClassList(CriticalClass);
                    row.HpFill.RemoveFromClassList(LowClass);
                }
            }
            else
            {
                row.Root.AddToClassList(DisconnectedClass);
            }
        }

        private class SquadRow
        {
            public VisualElement Root;
            public Label Name;
            public VisualElement HpFill;
        }
    }
}
