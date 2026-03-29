using Gridlock.Audio;
using Gridlock.CameraSystem;
using Gridlock.Interfaces;
using Gridlock.Modules;
using Gridlock.NodeEditor.Data;
using Gridlock.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.NodeEditor.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class NodeEditorScreen : MonoBehaviour
    {
        [SerializeField] private ModuleRegistry moduleRegistry;
        [SerializeField] private float focusOrthoSize = 10f;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _panelLeft;
        private VisualElement _panelRight;
        private VisualElement _panelCenter;
        private Controls _controls;
        private IChassis _currentChassis;
        private PlayerInventory _playerInventory;
        private Transform _currentTowerTransform;
        private bool _isOpen;

        private NodeEditorCanvas _canvas;
        private ModulePalette _palette;

        public bool IsOpen => _isOpen;

        private static NodeEditorScreen _instance;
        public static NodeEditorScreen Instance => _instance;

        private void Awake()
        {
            _instance = this;
            _controls = new Controls();
            _uiDocument = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_uiDocument == null) return;

            _root = _uiDocument.rootVisualElement;
            if (_root == null) return;

            _root.style.display = DisplayStyle.None;

            _panelLeft = _root.Q("panel-left");
            _panelRight = _root.Q("panel-right");
            _panelCenter = _root.Q("panel-center");

            var canvasArea = _root.Q("canvas-area");
            if (canvasArea != null)
            {
                _canvas = new NodeEditorCanvas(canvasArea, moduleRegistry);
                _canvas.OnNodeAdded += OnCanvasNodeAdded;
                _canvas.OnNodeRemoved += OnCanvasNodeRemoved;
            }

            _palette = new ModulePalette(_root, moduleRegistry, _canvas);

            var saveBtn = _root.Q<Button>("save-btn");
            if (saveBtn != null)
                saveBtn.clicked += OnSaveButtonClicked;

            var docsBtn = _root.Q<Button>("btn-docs");
            if (docsBtn != null)
                docsBtn.clicked += ShowDocumentation;
        }

        public void Open(IChassis chassis, PlayerInventory inventory = null, Transform towerTransform = null)
        {
            _currentChassis = chassis;
            _playerInventory = inventory;
            _currentTowerTransform = towerTransform;
            _isOpen = true;
            _canvas?.ResumeAnimations();

            if (_root != null)
                _root.style.display = DisplayStyle.Flex;

            if (_currentTowerTransform != null)
                TopDownCamera.Instance?.FocusOn(_currentTowerTransform.position, focusOrthoSize);

            TopDownCamera.Instance?.SetInputEnabled(false);

            SoundManager.Instance?.PlayUI(SoundType.EditorOpen);

            _root?.schedule.Execute(() =>
            {
                _panelLeft?.AddToClassList("panel-left--open");
                _panelRight?.AddToClassList("panel-right--open");
            });

            var graph = chassis.GetNodeGraph();
            _canvas?.LoadGraph(graph, chassis.MaxTriggers);
            _palette?.Initialize(_playerInventory);

            _controls.Player.Disable();
            _controls.UI.Enable();

            _controls.UI.Cancel.performed -= OnCancelPerformed;
            _controls.UI.Cancel.performed += OnCancelPerformed;
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _canvas?.PauseAnimations();
            SoundManager.Instance?.PlayUI(SoundType.EditorClose);

            _panelLeft?.RemoveFromClassList("panel-left--open");
            _panelRight?.RemoveFromClassList("panel-right--open");

            TopDownCamera.Instance?.RestoreFocus();
            TopDownCamera.Instance?.SetInputEnabled(true);

            _root?.schedule.Execute(() =>
            {
                if (!_isOpen && _root != null)
                    _root.style.display = DisplayStyle.None;
            }).ExecuteLater(400);

            _controls.UI.Cancel.performed -= OnCancelPerformed;
            _controls.UI.Disable();
            _controls.Player.Enable();

            _currentChassis = null;
            _currentTowerTransform = null;
        }

        public void SaveGraph()
        {
            if (_currentChassis == null || _canvas == null) return;

            var graph = _canvas.GetCurrentGraph();

            if (_playerInventory != null)
            {
                var oldGraph = _currentChassis.GetNodeGraph();
                var oldCounts = new System.Collections.Generic.Dictionary<string, int>();
                var newCounts = new System.Collections.Generic.Dictionary<string, int>();

                if (oldGraph?.nodes != null)
                {
                    foreach (var n in oldGraph.nodes)
                    {
                        if (n.isFixed) continue;
                        oldCounts.TryGetValue(n.moduleDefId, out int c);
                        oldCounts[n.moduleDefId] = c + 1;
                    }
                }

                foreach (var n in graph.nodes)
                {
                    if (n.isFixed) continue;
                    newCounts.TryGetValue(n.moduleDefId, out int c);
                    newCounts[n.moduleDefId] = c + 1;
                }

                var allIds = new System.Collections.Generic.HashSet<string>();
                foreach (var k in oldCounts.Keys) allIds.Add(k);
                foreach (var k in newCounts.Keys) allIds.Add(k);

                foreach (var id in allIds)
                {
                    oldCounts.TryGetValue(id, out int oldC);
                    newCounts.TryGetValue(id, out int newC);
                    int delta = newC - oldC;
                    if (delta > 0)
                        _playerInventory.RemoveModule(id, delta);
                    else if (delta < 0)
                        _playerInventory.AddModule(id, -delta);
                }
            }

            _currentChassis.SetNodeGraph(graph);
            Debug.Log("[NodeEditor] Graph saved");
        }

        public void OnSaveButtonClicked()
        {
            SaveGraph();
            Close();
        }

        private void OnCancelPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx)
        {
            Close();
        }

        private void OnCanvasNodeAdded(string moduleDefId)
        {
            _palette?.OnNodeAdded(moduleDefId);
        }

        private void OnCanvasNodeRemoved(string moduleDefId)
        {
            _palette?.OnNodeRemoved(moduleDefId);
        }

        private void ShowDocumentation()
        {
            if (_root == null) return;

            var existing = _root.Q("doc-overlay");
            if (existing != null) { existing.RemoveFromHierarchy(); return; }

            var popup = DocumentationContent.Build();
            popup.Show(_root);
        }

        private void OnDisable()
        {
            if (_canvas != null)
            {
                _canvas.OnNodeAdded -= OnCanvasNodeAdded;
                _canvas.OnNodeRemoved -= OnCanvasNodeRemoved;
            }
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            _controls?.Dispose();
        }
    }
}
