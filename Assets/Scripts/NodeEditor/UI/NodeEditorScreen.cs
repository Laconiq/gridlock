using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Modules;
using AIWE.Network;
using AIWE.NodeEditor.Data;
using AIWE.Player;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class NodeEditorScreen : MonoBehaviour
    {
        private const string LeftClickModuleId = "trigger_onleftclick";
        private const string RightClickModuleId = "trigger_onrightclick";

        [SerializeField] private ModuleRegistry moduleRegistry;

        private UIDocument _uiDocument;
        private VisualElement _root;
        private Controls _controls;
        private IChassis _currentChassis;
        private PlayerInventory _playerInventory;
        private bool _isOpen;
        private bool _isWeaponMode;

        private NodeEditorCanvas _canvas;
        private ModulePalette _palette;

        public bool IsOpen => _isOpen;
        public bool IsWeaponMode => _isWeaponMode;

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

        private void Start()
        {
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
                lockManager.OnLockGranted += OnLockGranted;
        }

        public void Open(IChassis chassis, PlayerInventory inventory = null)
        {
            _currentChassis = chassis;
            _playerInventory = inventory;
            _isOpen = true;
            _isWeaponMode = chassis is PlayerWeaponChassis;
            _canvas?.ResumeAnimations();

            if (_root != null)
                _root.style.display = DisplayStyle.Flex;

            var graph = chassis.GetNodeGraph();

            if (_isWeaponMode)
                EnsureFixedTriggerNodes(graph);

            _canvas?.LoadGraph(graph, chassis.MaxTriggers);
            _palette?.Initialize(_playerInventory);

            _controls.Player.Disable();
            _controls.UI.Enable();

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                var cam = localPlayer.GetComponentInChildren<PlayerCamera>();
                if (cam != null) cam.InputEnabled = false;
            }

            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;

            _controls.UI.Cancel.performed -= OnCancelPerformed;
            _controls.UI.Cancel.performed += OnCancelPerformed;
        }

        private void EnsureFixedTriggerNodes(NodeGraphData graph)
        {
            bool hasLeft = false;
            bool hasRight = false;

            foreach (var node in graph.nodes)
            {
                if (node.isFixed && node.moduleDefId == LeftClickModuleId) hasLeft = true;
                if (node.isFixed && node.moduleDefId == RightClickModuleId) hasRight = true;
            }

            if (!hasLeft)
            {
                graph.nodes.Insert(0, new NodeData
                {
                    moduleDefId = LeftClickModuleId,
                    category = ModuleCategory.Trigger,
                    editorPosition = new Vector2(-300, -80),
                    isFixed = true
                });
            }

            if (!hasRight)
            {
                graph.nodes.Insert(1, new NodeData
                {
                    moduleDefId = RightClickModuleId,
                    category = ModuleCategory.Trigger,
                    editorPosition = new Vector2(-300, 80),
                    isFixed = true
                });
            }
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;
            _canvas?.PauseAnimations();

            if (_root != null)
                _root.style.display = DisplayStyle.None;

            _controls.UI.Cancel.performed -= OnCancelPerformed;
            _controls.UI.Disable();
            _controls.Player.Enable();

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                var cam = localPlayer.GetComponentInChildren<PlayerCamera>();
                if (cam != null) cam.InputEnabled = true;
            }

            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;

            if (!_isWeaponMode)
            {
                var lockManager = ServiceLocator.Get<EditorLockManager>();
                if (lockManager != null && NetworkManager.Singleton != null)
                    lockManager.ReleaseLockRpc();
            }

            _currentChassis = null;
            _isWeaponMode = false;
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

        private void OnLockGranted() { }

        private void OnDestroy()
        {
            _controls?.Dispose();
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
                lockManager.OnLockGranted -= OnLockGranted;
        }
    }
}
