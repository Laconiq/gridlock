using AIWE.Core;
using AIWE.Interfaces;
using AIWE.Network;
using AIWE.NodeEditor.Data;
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

        public void Open(IChassis chassis)
        {
            _currentChassis = chassis;
            _isOpen = true;

            if (editorPanel != null) editorPanel.SetActive(true);

            var graph = chassis.GetNodeGraph();
            if (canvas != null) canvas.LoadGraph(graph, chassis.MaxTriggers);
            if (palette != null) palette.Initialize(canvas);

            _controls.Player.Disable();
            _controls.UI.Enable();

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
            // This is called when our lock request is granted
            // The interaction system should have set the chassis to open
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
