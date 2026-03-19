using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Network;
using AIWE.NodeEditor.Data;
using AIWE.Player;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.NodeEditor.UI
{
    public class NodeEditorScreen : MonoBehaviour
    {
        [SerializeField] private GameObject editorPanel;
        [SerializeField] private NodeEditorCanvas canvas;
        [SerializeField] private ModulePalette palette;

        private Controls _controls;
        private IChassis _currentChassis;
        private PlayerInventory _playerInventory;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private static NodeEditorScreen _instance;
        public static NodeEditorScreen Instance => _instance;

        private void Awake()
        {
            _instance = this;
            _controls = new Controls();
            if (editorPanel != null) editorPanel.SetActive(false);
        }

        private void Start()
        {
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
            {
                lockManager.OnLockGranted += OnLockGranted;
            }
        }

        public void Open(IChassis chassis, PlayerInventory inventory = null)
        {
            _currentChassis = chassis;
            _playerInventory = inventory;
            _isOpen = true;

            if (editorPanel != null) editorPanel.SetActive(true);

            var graph = chassis.GetNodeGraph();
            if (canvas != null) canvas.LoadGraph(graph, chassis.MaxTriggers);
            if (palette != null) palette.Initialize(canvas, _playerInventory);

            _controls.Player.Disable();
            _controls.UI.Enable();

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                var cam = localPlayer.GetComponentInChildren<PlayerCamera>();
                if (cam != null) cam.InputEnabled = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            _controls.UI.Cancel.performed += _ => Close();
        }

        public void Close()
        {
            if (!_isOpen) return;
            _isOpen = false;

            if (editorPanel != null) editorPanel.SetActive(false);

            _controls.UI.Disable();
            _controls.Player.Enable();

            var localPlayer = NetworkManager.Singleton?.LocalClient?.PlayerObject;
            if (localPlayer != null)
            {
                var cam = localPlayer.GetComponentInChildren<PlayerCamera>();
                if (cam != null) cam.InputEnabled = true;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null && NetworkManager.Singleton != null)
            {
                lockManager.ReleaseLockRpc(NetworkManager.Singleton.LocalClientId);
            }

            _currentChassis = null;
        }

        public void SaveGraph()
        {
            if (_currentChassis == null || canvas == null) return;

            var graph = canvas.GetCurrentGraph();

            if (_playerInventory != null)
            {
                var oldGraph = _currentChassis.GetNodeGraph();
                var oldCounts = new System.Collections.Generic.Dictionary<string, int>();
                var newCounts = new System.Collections.Generic.Dictionary<string, int>();

                if (oldGraph?.nodes != null)
                {
                    foreach (var n in oldGraph.nodes)
                    {
                        oldCounts.TryGetValue(n.moduleDefId, out int c);
                        oldCounts[n.moduleDefId] = c + 1;
                    }
                }
                foreach (var n in graph.nodes)
                {
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

        private void OnLockGranted()
        {
        }

        private void OnDestroy()
        {
            _controls?.Dispose();
            var lockManager = ServiceLocator.Get<EditorLockManager>();
            if (lockManager != null)
            {
                lockManager.OnLockGranted -= OnLockGranted;
            }
        }
    }
}
