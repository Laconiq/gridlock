using System;
using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AIWE.NodeEditor.UI
{
    public class NodeEditorCanvas : MonoBehaviour, IPointerDownHandler, IDragHandler, IScrollHandler
    {
        public event Action<string> OnNodeAdded;
        public event Action<string> OnNodeRemoved;
        [Header("References")]
        [SerializeField] private RectTransform content;
        [SerializeField] private NodeWidget nodeWidgetPrefab;
        [SerializeField] private ConnectionRenderer connectionPrefab;
        [SerializeField] private ModuleRegistry moduleRegistry;

        [Header("Settings")]
        [SerializeField] private float zoomSpeed = 0.1f;
        [SerializeField] private float minZoom = 0.3f;
        [SerializeField] private float maxZoom = 2f;

        private NodeGraphData _graph;
        private int _maxTriggers;
        private readonly List<NodeWidget> _nodeWidgets = new();
        private readonly List<ConnectionRenderer> _connectionRenderers = new();
        private float _currentZoom = 1f;
        private Vector2 _panOffset;
        private bool _isPanning;

        public void LoadGraph(NodeGraphData graph, int maxTriggers)
        {
            _graph = graph ?? new NodeGraphData();
            _maxTriggers = maxTriggers;

            ClearCanvas();
            CreateNodeWidgets();
            CreateConnections();
        }

        public NodeGraphData GetCurrentGraph()
        {
            if (_graph == null) _graph = new NodeGraphData();

            foreach (var widget in _nodeWidgets)
            {
                if (widget == null) continue;
                var nodeData = _graph.nodes.Find(n => n.nodeId == widget.NodeId);
                if (nodeData != null)
                {
                    nodeData.editorPosition = widget.GetComponent<RectTransform>().anchoredPosition;
                }
            }

            return _graph;
        }

        private void ClearCanvas()
        {
            foreach (var widget in _nodeWidgets)
            {
                if (widget != null) Destroy(widget.gameObject);
            }
            _nodeWidgets.Clear();

            foreach (var conn in _connectionRenderers)
            {
                if (conn != null) Destroy(conn.gameObject);
            }
            _connectionRenderers.Clear();
        }

        private void CreateNodeWidgets()
        {
            if (_graph == null || nodeWidgetPrefab == null || content == null) return;

            foreach (var nodeData in _graph.nodes)
            {
                var widget = Instantiate(nodeWidgetPrefab, content);
                widget.Initialize(nodeData, this, moduleRegistry);
                widget.GetComponent<RectTransform>().anchoredPosition = nodeData.editorPosition;
                _nodeWidgets.Add(widget);
            }
        }

        private void CreateConnections()
        {
            if (_graph == null || connectionPrefab == null) return;

            foreach (var conn in _graph.connections)
            {
                var fromWidget = _nodeWidgets.Find(w => w.NodeId == conn.fromNodeId);
                var toWidget = _nodeWidgets.Find(w => w.NodeId == conn.toNodeId);

                if (fromWidget != null && toWidget != null)
                {
                    var renderer = Instantiate(connectionPrefab, content);
                    var lineColor = fromWidget.GetOutputPortColor(conn.fromPort);
                    renderer.SetEndpoints(
                        fromWidget.GetOutputPort(conn.fromPort),
                        toWidget.GetInputPort(conn.toPort),
                        lineColor
                    );
                    _connectionRenderers.Add(renderer);
                }
            }
        }

        public NodeWidget AddNode(NodeData nodeData)
        {
            if (_graph == null) _graph = new NodeGraphData();

            if (nodeData.category == ModuleCategory.Trigger)
            {
                int triggerCount = _graph.nodes.FindAll(n => n.category == ModuleCategory.Trigger).Count;
                if (triggerCount >= _maxTriggers)
                {
                    Debug.LogWarning($"[NodeEditor] Max triggers ({_maxTriggers}) reached");
                    return null;
                }
            }

            _graph.nodes.Add(nodeData);

            if (nodeWidgetPrefab != null && content != null)
            {
                var widget = Instantiate(nodeWidgetPrefab, content);
                widget.Initialize(nodeData, this, moduleRegistry);
                widget.GetComponent<RectTransform>().anchoredPosition = nodeData.editorPosition;
                _nodeWidgets.Add(widget);
                OnNodeAdded?.Invoke(nodeData.moduleDefId);
                return widget;
            }
            return null;
        }

        public void RemoveNode(string nodeId)
        {
            var nodeData = _graph?.nodes.Find(n => n.nodeId == nodeId);
            string moduleDefId = nodeData?.moduleDefId;

            _graph?.nodes.RemoveAll(n => n.nodeId == nodeId);
            _graph?.connections.RemoveAll(c => c.fromNodeId == nodeId || c.toNodeId == nodeId);

            var widget = _nodeWidgets.Find(w => w.NodeId == nodeId);
            if (widget != null)
            {
                _nodeWidgets.Remove(widget);
                Destroy(widget.gameObject);
            }

            if (moduleDefId != null)
                OnNodeRemoved?.Invoke(moduleDefId);

            RebuildConnections();
        }

        public void AddConnection(string fromNodeId, int fromPort, string toNodeId, int toPort)
        {
            if (_graph == null) return;

            _graph.connections.RemoveAll(c => c.toNodeId == toNodeId && c.toPort == toPort);

            var conn = new ConnectionData
            {
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                fromPort = fromPort,
                toPort = toPort
            };
            _graph.connections.Add(conn);
            RebuildConnections();
        }

        public void RemoveConnection(string fromNodeId, string toNodeId)
        {
            _graph?.connections.RemoveAll(c => c.fromNodeId == fromNodeId && c.toNodeId == toNodeId);
            RebuildConnections();
        }

        private void RebuildConnections()
        {
            foreach (var conn in _connectionRenderers)
            {
                if (conn != null) Destroy(conn.gameObject);
            }
            _connectionRenderers.Clear();
            CreateConnections();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right ||
                eventData.button == PointerEventData.InputButton.Middle)
            {
                _isPanning = true;
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_isPanning && content != null)
            {
                content.anchoredPosition += eventData.delta / _currentZoom;
            }
        }

        public void OnScroll(PointerEventData eventData)
        {
            if (content == null) return;

            var delta = eventData.scrollDelta.y * zoomSpeed;
            _currentZoom = Mathf.Clamp(_currentZoom + delta, minZoom, maxZoom);
            content.localScale = Vector3.one * _currentZoom;
        }

        private void LateUpdate()
        {
            if (!Mouse.current.rightButton.isPressed && !Mouse.current.middleButton.isPressed)
            {
                _isPanning = false;
            }
        }
    }
}
