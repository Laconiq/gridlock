using System;
using System.Collections.Generic;
using Gridlock.Audio;
using Gridlock.Mods;
using Gridlock.Mods.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.Loot
{
    [RequireComponent(typeof(UIDocument))]
    public class InventoryPanel : MonoBehaviour
    {
        public static InventoryPanel Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _container;
        private VisualElement _tab;
        private VisualElement _grid;
        private Controls _controls;
        private bool _isOpen;

        private readonly Dictionary<ModType, VisualElement> _tiles = new();
        private readonly Dictionary<ModType, Label> _badges = new();
        private EventCallback<ClickEvent> _tabClickCallback;

        public bool IsOpen => _isOpen;

        private void Awake()
        {
            Instance = this;
            _controls = new Controls();
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            _uiDocument.rootVisualElement.pickingMode = PickingMode.Ignore;
            if (_uiDocument.rootVisualElement.parent != null)
                _uiDocument.rootVisualElement.parent.pickingMode = PickingMode.Ignore;

            _root = _uiDocument.rootVisualElement.Q("inventory-root");
            if (_root == null) return;

            _container = _root.Q("inventory-container");
            _tab = _root.Q("inventory-tab");
            _grid = _root.Q("inventory-grid");

            _tabClickCallback = _ => Toggle();
            _tab?.RegisterCallback(_tabClickCallback);

            _controls.Player.ToggleInventory.performed += OnToggleInput;
            _controls.Player.Enable();

            EnsureSubscribed();
            RebuildGrid();
        }

        private void OnDisable()
        {
            _controls.Player.ToggleInventory.performed -= OnToggleInput;

            if (_tabClickCallback != null)
            {
                _tab?.UnregisterCallback(_tabClickCallback);
                _tabClickCallback = null;
            }

            if (_subscribed && PlayerInventory.Instance != null)
            {
                PlayerInventory.Instance.OnModChanged -= OnModChanged;
                _subscribed = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            _controls?.Dispose();
        }

        private void OnToggleInput(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            if (ModSlotPanel.Instance != null && ModSlotPanel.Instance.IsOpen) return;
            Toggle();
        }

        public void Toggle()
        {
            if (_isOpen) Close();
            else Open();
        }

        public void Open()
        {
            _isOpen = true;
            _container?.RemoveFromClassList("inventory-panel__root--closed");
            _container?.AddToClassList("inventory-panel__root--open");
            EnsureSubscribed();
            RebuildGrid();
            SoundManager.Instance?.PlayUI(SoundType.InventoryOpen);
        }

        private bool _subscribed;

        private void EnsureSubscribed()
        {
            if (_subscribed) return;
            if (PlayerInventory.Instance == null) return;
            PlayerInventory.Instance.OnModChanged += OnModChanged;
            _subscribed = true;
        }

        public void Close()
        {
            _isOpen = false;
            _container?.RemoveFromClassList("inventory-panel__root--open");
            _container?.AddToClassList("inventory-panel__root--closed");
            SoundManager.Instance?.PlayUI(SoundType.InventoryClose);
        }

        public void OpenForEditing()
        {
            Open();
        }

        public Vector2 GetTabScreenPosition()
        {
            if (_tab == null) return new Vector2(Screen.width - 20f, Screen.height * 0.5f);

            var bound = _tab.worldBound;
            var panel = _tab.panel;
            if (panel == null) return new Vector2(Screen.width - 20f, Screen.height * 0.5f);

            float x = bound.center.x;
            float y = Screen.height - bound.center.y;
            return new Vector2(x, y);
        }

        public bool IsScreenPointOver(Vector2 screenPos)
        {
            if (!_isOpen || _container == null) return false;
            var panel = _uiDocument.rootVisualElement.panel;
            if (panel == null) return false;

            var flipped = new Vector2(screenPos.x, Screen.height - screenPos.y);
            var panelPos = RuntimePanelUtils.ScreenToPanel(panel, flipped);
            return _container.worldBound.Contains(panelPos);
        }

        public void OnPickupCollected(ModType type)
        {
            if (_tab == null) return;
            _tab.AddToClassList("inventory-panel__tab-pulse");
            _tab.schedule.Execute(() => _tab.RemoveFromClassList("inventory-panel__tab-pulse")).ExecuteLater(200);

            if (_badges.TryGetValue(type, out var badge))
            {
                badge.AddToClassList("inventory-tile__badge--bump");
                badge.schedule.Execute(() => badge.RemoveFromClassList("inventory-tile__badge--bump")).ExecuteLater(150);
            }
        }

        private void OnModChanged(ModType type, int newQty)
        {
            if (!_tiles.TryGetValue(type, out var tile))
            {
                RebuildGrid();
                return;
            }

            UpdateTileQuantity(tile, type, newQty);
        }

        private void RebuildGrid()
        {
            if (_grid == null) return;
            _grid.Clear();
            _tiles.Clear();
            _badges.Clear();

            var inventory = PlayerInventory.Instance;
            if (inventory == null) return;

            foreach (var kvp in inventory.All)
            {
                if (kvp.Value <= 0) continue;
                CreateTile(kvp.Key, kvp.Value);
            }
        }

        private void CreateTile(ModType type, int quantity)
        {
            var tile = ModTileFactory.Create(type);
            tile.pickingMode = PickingMode.Position;
            var badge = ModTileFactory.AddBadge(tile, quantity);

            tile.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (PlayerInventory.Instance == null || PlayerInventory.Instance.GetCount(type) <= 0) return;
                var panel = ModSlotPanel.Instance;
                if (panel != null && panel.IsOpen)
                    panel.StartDragFromInventory(type);
            });

            tile.RegisterCallback<PointerEnterEvent>(_ =>
            {
                var panel = ModSlotPanel.Instance;
                if (panel != null && panel.IsOpen)
                    panel.ShowModInfoExternal(type);
            });

            if (quantity <= 0)
                tile.AddToClassList("inventory-tile--empty");

            _tiles[type] = tile;
            _badges[type] = badge;
            _grid.Add(tile);
        }

        private void UpdateTileQuantity(VisualElement tile, ModType type, int newQty)
        {
            if (_badges.TryGetValue(type, out var badge))
                badge.text = $"x{newQty}";

            if (newQty <= 0)
                tile.AddToClassList("inventory-tile--empty");
            else
                tile.RemoveFromClassList("inventory-tile--empty");
        }
    }
}
