using System.Collections.Generic;
using Gridlock.Interfaces;
using Gridlock.Modules;
using Gridlock.NodeEditor.Data;
using Gridlock.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.RadialMenu
{
    [AddComponentMenu("Gridlock/UI/Radial Menu")]
    [RequireComponent(typeof(UIDocument))]
    public class RadialMenuScreen : MonoBehaviour
    {
        private static RadialMenuScreen _instance;
        public static RadialMenuScreen Instance => _instance;

        [SerializeField] private ModuleRegistry moduleRegistry;
        [SerializeField] private float segmentRadius = 140f;

        private UIDocument _uiDocument;
        private VisualElement _overlay;
        private VisualElement _segmentsContainer;
        private Label _layerLabel;
        private Button _backBtn;
        private Button _addBtn;

        private RadialMenuCanvas _canvas;
        private AddModulePopup _addPopup;
        private IChassis _chassis;
        private PlayerInventory _inventory;

        private bool _isOpen;
        public bool IsOpen => _isOpen;

        private int _currentLayer;
        private string _selectedTriggerNodeId;
        private string _selectedZoneNodeId;

        private void Awake()
        {
            _instance = this;
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            var root = _uiDocument.rootVisualElement;
            if (root == null) return;

            _overlay = root.Q("radial-overlay");
            _segmentsContainer = root.Q("segments-container");
            _layerLabel = root.Q<Label>("layer-label");
            _backBtn = root.Q<Button>("back-btn");
            _addBtn = root.Q<Button>("add-btn");

            if (_overlay != null)
                _overlay.style.display = DisplayStyle.None;

            var addPopupElement = root.Q("add-popup");
            if (addPopupElement != null)
            {
                _addPopup = new AddModulePopup(addPopupElement, moduleRegistry);
                _addPopup.OnModuleSelected += OnModuleSelected;
            }

            if (_backBtn != null)
                _backBtn.clicked += OnBackClicked;

            if (_addBtn != null)
                _addBtn.clicked += OnAddClicked;
        }

        public void Open(IChassis chassis, PlayerInventory inventory)
        {
            _chassis = chassis;
            _inventory = inventory;
            _canvas = new RadialMenuCanvas(chassis.GetNodeGraph());
            _isOpen = true;

            if (_overlay != null)
                _overlay.style.display = DisplayStyle.Flex;

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            _selectedTriggerNodeId = null;
            _selectedZoneNodeId = null;
            ShowLayer(0);
        }

        public void Close()
        {
            if (_canvas != null && _chassis != null)
                _chassis.SetNodeGraph(_canvas.Graph);

            if (_overlay != null)
                _overlay.style.display = DisplayStyle.None;

            _addPopup?.Hide();

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            _isOpen = false;
            _chassis = null;
            _inventory = null;
            _canvas = null;
        }

        private void ShowLayer(int layer)
        {
            _currentLayer = layer;

            if (_layerLabel != null)
            {
                _layerLabel.text = layer switch
                {
                    0 => "TRIGGERS",
                    1 => "TARGETS",
                    2 => "EFFECTS",
                    _ => ""
                };
            }

            if (_backBtn != null)
                _backBtn.style.display = layer == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            if (_segmentsContainer != null)
                _segmentsContainer.Clear();

            _addPopup?.Hide();

            if (_canvas == null) return;

            var nodes = GetNodesForCurrentLayer();
            var segments = new List<RadialSegment>();

            foreach (var node in nodes)
            {
                var def = moduleRegistry.GetById(node.moduleDefId);
                string displayName = def != null ? def.displayName : node.moduleDefId;
                var segment = new RadialSegment(node.nodeId, displayName, node.category);
                segment.OnClicked += OnSegmentClicked;
                segments.Add(segment);
                _segmentsContainer?.Add(segment);
            }

            float containerCenter = 200f;
            int count = segments.Count;
            for (int i = 0; i < count; i++)
            {
                float angle = -90f + (360f / (count + 1)) * (i + 1);
                segments[i].SetPolarPosition(containerCenter, containerCenter, segmentRadius, angle);
            }

            UpdateAddButtonColor();
        }

        private List<NodeData> GetNodesForCurrentLayer()
        {
            return _currentLayer switch
            {
                0 => _canvas.GetTriggers(),
                1 => _selectedTriggerNodeId != null
                    ? _canvas.GetZonesForTrigger(_selectedTriggerNodeId)
                    : new List<NodeData>(),
                2 => _selectedZoneNodeId != null
                    ? _canvas.GetEffectsForZone(_selectedZoneNodeId)
                    : new List<NodeData>(),
                _ => new List<NodeData>()
            };
        }

        private void OnSegmentClicked(string nodeId)
        {
            switch (_currentLayer)
            {
                case 0:
                    _selectedTriggerNodeId = nodeId;
                    ShowLayer(1);
                    break;
                case 1:
                    _selectedZoneNodeId = nodeId;
                    ShowLayer(2);
                    break;
            }
        }

        private void OnBackClicked()
        {
            if (_currentLayer == 2)
                ShowLayer(1);
            else if (_currentLayer == 1)
                ShowLayer(0);
        }

        private void OnAddClicked()
        {
            var category = _currentLayer switch
            {
                0 => ModuleCategory.Trigger,
                1 => ModuleCategory.Zone,
                2 => ModuleCategory.Effect,
                _ => ModuleCategory.Trigger
            };

            if (_currentLayer == 0 && _chassis != null)
            {
                var triggers = _canvas.GetTriggers();
                if (triggers.Count >= _chassis.MaxTriggers)
                    return;
            }

            _addPopup?.Show(category, _inventory);
        }

        private void OnModuleSelected(string moduleDefId)
        {
            if (_canvas == null || _inventory == null) return;

            switch (_currentLayer)
            {
                case 0:
                    _canvas.AddTrigger(moduleDefId);
                    _inventory.RemoveModule(moduleDefId);
                    break;
                case 1:
                    if (_selectedTriggerNodeId != null)
                    {
                        _canvas.AddZone(moduleDefId, _selectedTriggerNodeId);
                        _inventory.RemoveModule(moduleDefId);
                    }
                    break;
                case 2:
                    if (_selectedZoneNodeId != null)
                    {
                        _canvas.AddEffect(moduleDefId, _selectedZoneNodeId);
                        _inventory.RemoveModule(moduleDefId);
                    }
                    break;
            }

            ShowLayer(_currentLayer);
        }

        private void UpdateAddButtonColor()
        {
            if (_addBtn == null) return;

            var borderColor = _currentLayer switch
            {
                0 => new Color(1f, 0.47f, 0.28f),
                1 => new Color(0.56f, 0.96f, 1f),
                2 => new Color(0.18f, 0.97f, 0f),
                _ => Color.white
            };

            _addBtn.style.borderLeftColor = new StyleColor(borderColor);
            _addBtn.style.borderRightColor = new StyleColor(borderColor);
            _addBtn.style.borderTopColor = new StyleColor(borderColor);
            _addBtn.style.borderBottomColor = new StyleColor(borderColor);
        }
    }
}
