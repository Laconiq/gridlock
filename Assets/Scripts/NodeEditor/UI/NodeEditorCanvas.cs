using System;
using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using AIWE.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class NodeEditorCanvas
    {
        public event Action<string> OnNodeAdded;
        public event Action<string> OnNodeRemoved;

        private readonly VisualElement _viewport;
        private readonly VisualElement _content;
        private readonly ConnectionLayer _connectionLayer;
        private readonly GridBackground _gridBackground;
        private readonly ModuleRegistry _moduleRegistry;

        private NodeGraphData _graph;
        private int _maxTriggers;
        private readonly List<NodeWidget> _nodeWidgets = new();

        private Vector2 _panOffset;
        private float _zoomLevel = 1f;
        private const float ZoomMin = 0.15f;
        private const float ZoomMax = 3f;
        private const float ZoomFactor = 1.08f;
        private bool _isPanning;
        private int _panPointerId = -1;

        private PortWidget _dragSourcePort;
        private bool _isDraggingPort;
        private int _dragPortPointerId = -1;

        private NodeWidget _draggedNode;
        private bool _isDraggingNode;
        private int _nodeDragPointerId = -1;
        private Vector2 _nodeDragOffset;
        private VisualElement _dragOverlay;
        private VisualElement _sidebar;

        private readonly MinimapWidget _minimap;

        public VisualElement Content => _content;

        public NodeEditorCanvas(VisualElement canvasArea, ModuleRegistry registry)
        {
            _moduleRegistry = registry;
            _viewport = canvasArea;

            _content = canvasArea.Q("canvas-content");
            if (_content == null)
            {
                _content = new VisualElement { name = "canvas-content" };
                _content.AddToClassList("canvas-content");
                canvasArea.Add(_content);
            }

            var gridColor = DesignConstants.Primary;
            gridColor.a = 0.05f;
            _gridBackground = new GridBackground(color: gridColor);
            canvasArea.Insert(0, _gridBackground);

            _connectionLayer = new ConnectionLayer();
            _content.Add(_connectionLayer);

            _viewport.RegisterCallback<PointerDownEvent>(OnViewportPointerDown);
            _viewport.RegisterCallback<PointerMoveEvent>(OnViewportPointerMove);
            _viewport.RegisterCallback<PointerUpEvent>(OnViewportPointerUp);
            _viewport.RegisterCallback<WheelEvent>(OnWheel);

            _sidebar = canvasArea.parent?.Q("sidebar");

            var editorRoot = canvasArea.parent?.parent;
            if (editorRoot != null)
            {
                _dragOverlay = new VisualElement { name = "drag-overlay" };
                _dragOverlay.style.position = Position.Absolute;
                _dragOverlay.style.left = 0;
                _dragOverlay.style.top = 0;
                _dragOverlay.style.right = 0;
                _dragOverlay.style.bottom = 0;
                _dragOverlay.pickingMode = PickingMode.Ignore;
                editorRoot.Add(_dragOverlay);
            }

            _minimap = new MinimapWidget(canvasArea);
        }

        public void LoadGraph(NodeGraphData graph, int maxTriggers)
        {
            _graph = graph ?? new NodeGraphData();
            _maxTriggers = maxTriggers;

            _panOffset = Vector2.zero;
            _zoomLevel = 1f;
            ApplyContentTransform();
            _gridBackground.UpdateTransform(_panOffset, _zoomLevel);

            ClearCanvas();
            CreateNodeWidgets();
            RefreshConnections();
            RefreshMinimap();
        }

        public NodeGraphData GetCurrentGraph()
        {
            if (_graph == null) _graph = new NodeGraphData();

            foreach (var widget in _nodeWidgets)
            {
                if (widget == null) continue;
                var nodeData = _graph.nodes.Find(n => n.nodeId == widget.NodeId);
                if (nodeData != null)
                    nodeData.editorPosition = widget.Position;
            }

            return _graph;
        }

        private void ClearCanvas()
        {
            foreach (var widget in _nodeWidgets)
                widget.RemoveFromCanvas();
            _nodeWidgets.Clear();
            _connectionLayer.ClearConnections();
        }

        private void CreateNodeWidgets()
        {
            if (_graph == null) return;

            foreach (var nodeData in _graph.nodes)
            {
                var widget = new NodeWidget(nodeData, this, _moduleRegistry);
                _content.Add(widget.Root);
                widget.SetPosition(nodeData.editorPosition);
                _nodeWidgets.Add(widget);
            }
        }

        public NodeWidget AddNode(NodeData nodeData, bool animated = false)
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

            var widget = new NodeWidget(nodeData, this, _moduleRegistry);
            _content.Add(widget.Root);
            widget.SetPosition(nodeData.editorPosition);
            _nodeWidgets.Add(widget);

            if (animated)
                widget.PlaySpawnAnimation();

            OnNodeAdded?.Invoke(nodeData.moduleDefId);
            RefreshMinimap();
            return widget;
        }

        public void BeginNodeDrag(NodeWidget widget, PointerDownEvent evt)
        {
            if (_dragOverlay == null || widget == null) return;

            _draggedNode = widget;
            _isDraggingNode = true;
            _nodeDragPointerId = evt.pointerId;

            var cursorPanel = (Vector2)evt.position;

            var bounds = widget.Root.worldBound;
            bool hasLayout = bounds.width > 1f && bounds.height > 1f;

            if (hasLayout)
            {
                _nodeDragOffset = new Vector2(bounds.position.x, bounds.position.y) - cursorPanel;
            }
            else
            {
                _nodeDragOffset = new Vector2(-96f, -20f);
            }

            var targetPanel = cursorPanel + _nodeDragOffset;

            widget.Root.RemoveFromHierarchy();
            _dragOverlay.Add(widget.Root);

            widget.Root.style.transformOrigin = new StyleTransformOrigin(new TransformOrigin(0, 0, 0));
            float dragScale = _zoomLevel * 1.04f;
            widget.Root.style.scale = new StyleScale(new Scale(new Vector3(dragScale, dragScale, 1f)));

            var overlayPos = _dragOverlay.WorldToLocal(targetPanel);
            widget.Root.style.left = overlayPos.x;
            widget.Root.style.top = overlayPos.y;

            widget.Root.AddToClassList("node--dragging");
            widget.Root.BringToFront();

            _viewport.CapturePointer(evt.pointerId);
            _connectionLayer.MarkDirtyRepaint();
        }

        private void UpdateNodeDrag(PointerMoveEvent evt)
        {
            if (!_isDraggingNode || _draggedNode == null) return;

            var cursorPanel = (Vector2)evt.position;
            var targetPanel = cursorPanel + _nodeDragOffset;
            var overlayPos = _dragOverlay.WorldToLocal(targetPanel);

            _draggedNode.Root.style.left = overlayPos.x;
            _draggedNode.Root.style.top = overlayPos.y;

            SyncDraggedNodeToContent();
            _connectionLayer.MarkDirtyRepaint();
        }

        private void SyncDraggedNodeToContent()
        {
            if (_draggedNode == null) return;
            var nodeWorldPos = _draggedNode.Root.worldBound.position;
            var contentPos = _content.WorldToLocal(nodeWorldPos);
            _draggedNode.UpdatePositionSilent(contentPos);
        }

        private void EndNodeDrag(PointerUpEvent evt)
        {
            if (!_isDraggingNode || _draggedNode == null) return;

            _viewport.ReleasePointer(evt.pointerId);
            _draggedNode.Root.RemoveFromClassList("node--dragging");

            var cursorPanel = (Vector2)evt.position;

            bool overSidebar = _sidebar != null && _sidebar.worldBound.Contains(cursorPanel);

            if (overSidebar)
            {
                var nodeData = _graph?.nodes.Find(n => n.nodeId == _draggedNode.NodeId);
                if (nodeData != null && !nodeData.isFixed)
                {
                    var nodeToRemove = _draggedNode;
                    nodeToRemove.PlayDespawnAnimation(() =>
                    {
                        nodeToRemove.Root.RemoveFromHierarchy();
                    });

                    string moduleDefId = nodeData.moduleDefId;
                    _graph.nodes.RemoveAll(n => n.nodeId == nodeToRemove.NodeId);
                    _graph.connections.RemoveAll(c =>
                        c.fromNodeId == nodeToRemove.NodeId || c.toNodeId == nodeToRemove.NodeId);
                    _nodeWidgets.Remove(nodeToRemove);
                    if (moduleDefId != null) OnNodeRemoved?.Invoke(moduleDefId);
                    RefreshConnections();
                }
                else
                {
                    ReturnNodeToContent(cursorPanel);
                }
            }
            else
            {
                ReturnNodeToContent(cursorPanel);
            }

            _draggedNode = null;
            _isDraggingNode = false;
            _nodeDragPointerId = -1;
            _connectionLayer.MarkDirtyRepaint();
            RefreshConnections();
            RefreshMinimap();
        }

        private void ReturnNodeToContent(Vector2 cursorPanel)
        {
            if (_draggedNode == null) return;
            var nodeWorldPos = cursorPanel + _nodeDragOffset;
            _draggedNode.Root.RemoveFromHierarchy();
            _draggedNode.Root.style.scale = new StyleScale(new Scale(Vector3.one));
            _draggedNode.Root.style.transformOrigin = StyleKeyword.Null;
            _content.Add(_draggedNode.Root);
            var contentPos = _content.WorldToLocal(nodeWorldPos);
            _draggedNode.SetPosition(contentPos);
            _draggedNode.Data.editorPosition = contentPos;
        }

        public void RemoveNode(string nodeId)
        {
            var nodeData = _graph?.nodes.Find(n => n.nodeId == nodeId);
            if (nodeData != null && nodeData.isFixed) return;
            string moduleDefId = nodeData?.moduleDefId;

            _graph?.nodes.RemoveAll(n => n.nodeId == nodeId);
            _graph?.connections.RemoveAll(c => c.fromNodeId == nodeId || c.toNodeId == nodeId);

            var widget = _nodeWidgets.Find(w => w.NodeId == nodeId);
            if (widget != null)
            {
                widget.RemoveFromCanvas();
                _nodeWidgets.Remove(widget);
            }

            if (moduleDefId != null)
                OnNodeRemoved?.Invoke(moduleDefId);

            RefreshConnections();
        }

        public void AddConnection(string fromNodeId, int fromPort, string toNodeId, int toPort)
        {
            if (_graph == null) return;

            _graph.connections.RemoveAll(c => c.toNodeId == toNodeId && c.toPort == toPort);

            _graph.connections.Add(new ConnectionData
            {
                fromNodeId = fromNodeId,
                toNodeId = toNodeId,
                fromPort = fromPort,
                toPort = toPort
            });

            RefreshConnections();
        }

        public void RefreshConnections()
        {
            if (_graph == null) return;

            var connectionInfos = new List<(VisualElement from, VisualElement to, Color color)>();

            foreach (var conn in _graph.connections)
            {
                var fromWidget = _nodeWidgets.Find(w => w.NodeId == conn.fromNodeId);
                var toWidget = _nodeWidgets.Find(w => w.NodeId == conn.toNodeId);

                if (fromWidget != null && toWidget != null)
                {
                    var fromPort = fromWidget.GetOutputPort(conn.fromPort);
                    var toPort = toWidget.GetInputPort(conn.toPort);

                    if (fromPort != null && toPort != null)
                    {
                        connectionInfos.Add((fromPort.Element, toPort.Element, fromPort.PortColor));
                    }
                }
            }

            _connectionLayer.SetConnections(connectionInfos);
        }

        public void OnNodeMoved()
        {
            _connectionLayer.MarkDirtyRepaint();
            RefreshMinimap();
        }

        // === Port Drag ===

        public void StartPortDrag(PortWidget port, PointerDownEvent evt)
        {
            _dragSourcePort = port;
            _isDraggingPort = true;
            _dragPortPointerId = evt.pointerId;
            _viewport.CapturePointer(evt.pointerId);
        }

        private void UpdatePortDrag(PointerMoveEvent evt)
        {
            if (!_isDraggingPort || _dragSourcePort == null) return;

            var connLocal = _connectionLayer.WorldToLocal(evt.position);

            _connectionLayer.SetTempConnection(_dragSourcePort.Element, connLocal, _dragSourcePort.PortColor);
        }

        private void EndPortDrag(PointerUpEvent evt)
        {
            if (!_isDraggingPort) return;

            _viewport.ReleasePointer(evt.pointerId);
            _connectionLayer.ClearTempConnection();

            var targetPort = FindPortAtPanelPosition(evt.position);

            if (targetPort != null && targetPort != _dragSourcePort &&
                targetPort.ParentNode != _dragSourcePort.ParentNode)
            {
                PortWidget output, input;
                if (_dragSourcePort.IsInput)
                {
                    if (!targetPort.IsInput) { output = targetPort; input = _dragSourcePort; }
                    else { ResetPortDrag(); return; }
                }
                else
                {
                    if (targetPort.IsInput) { output = _dragSourcePort; input = targetPort; }
                    else { ResetPortDrag(); return; }
                }

                TryConnect(output, input);
            }

            ResetPortDrag();
        }

        private void ResetPortDrag()
        {
            _dragSourcePort = null;
            _isDraggingPort = false;
            _dragPortPointerId = -1;
            PortWidget.ResetDragState();
        }

        private PortWidget FindPortAtPanelPosition(Vector2 panelPos)
        {
            foreach (var node in _nodeWidgets)
            {
                foreach (var port in node.AllPorts)
                {
                    if (port.Element.worldBound.Contains(panelPos))
                        return port;
                }
            }
            return null;
        }

        private void TryConnect(PortWidget from, PortWidget to)
        {
            var fromCategory = from.ParentNode.Data.category;
            var toCategory = to.ParentNode.Data.category;

            bool valid = (fromCategory, toCategory) switch
            {
                (ModuleCategory.Trigger, ModuleCategory.Zone) => true,
                (ModuleCategory.Zone, ModuleCategory.Zone) => true,
                (ModuleCategory.Zone, ModuleCategory.Effect) => true,
                (ModuleCategory.Effect, ModuleCategory.Effect) => true,
                _ => false
            };

            if (!valid)
            {
                Debug.LogWarning($"[NodeEditor] Invalid connection: {fromCategory} -> {toCategory}");
                return;
            }

            if (from.PortColor != to.PortColor)
            {
                Debug.LogWarning("[NodeEditor] Port color mismatch");
                return;
            }

            AddConnection(from.ParentNode.NodeId, from.PortIndex, to.ParentNode.NodeId, to.PortIndex);
        }

        // === Pan / Zoom ===

        private void OnViewportPointerDown(PointerDownEvent evt)
        {
            if (_isDraggingPort) return;

            if (evt.button == 1 || evt.button == 2)
            {
                _isPanning = true;
                _panPointerId = evt.pointerId;
                _viewport.CapturePointer(evt.pointerId);
                evt.StopPropagation();
            }
        }

        private void OnViewportPointerMove(PointerMoveEvent evt)
        {
            if (_isDraggingNode && evt.pointerId == _nodeDragPointerId)
            {
                UpdateNodeDrag(evt);
                return;
            }

            if (_isDraggingPort && evt.pointerId == _dragPortPointerId)
            {
                UpdatePortDrag(evt);
                return;
            }

            if (_isPanning && evt.pointerId == _panPointerId)
            {
                var delta = new Vector2(evt.deltaPosition.x, evt.deltaPosition.y);
                _panOffset += delta;
                ApplyContentTransform();
                _gridBackground.UpdateTransform(_panOffset, _zoomLevel);
                RefreshMinimap();
                evt.StopPropagation();
            }
        }

        private void OnViewportPointerUp(PointerUpEvent evt)
        {
            if (_isDraggingNode && evt.pointerId == _nodeDragPointerId)
            {
                EndNodeDrag(evt);
                return;
            }

            if (_isDraggingPort && evt.pointerId == _dragPortPointerId)
            {
                EndPortDrag(evt);
                return;
            }

            if (_isPanning && evt.pointerId == _panPointerId)
            {
                _isPanning = false;
                _viewport.ReleasePointer(evt.pointerId);
                _panPointerId = -1;
                evt.StopPropagation();
            }
        }

        private void OnWheel(WheelEvent evt)
        {
            float oldZoom = _zoomLevel;
            float factor = evt.delta.y > 0 ? 1f / ZoomFactor : ZoomFactor;
            _zoomLevel = Mathf.Clamp(_zoomLevel * factor, ZoomMin, ZoomMax);

            if (Mathf.Approximately(oldZoom, _zoomLevel))
            {
                evt.StopPropagation();
                return;
            }

            var cursorLocal = _viewport.WorldToLocal(evt.mousePosition);
            var contentPoint = (cursorLocal - _panOffset) / oldZoom;
            _panOffset = cursorLocal - contentPoint * _zoomLevel;

            ApplyContentTransform();
            _gridBackground.UpdateTransform(_panOffset, _zoomLevel);
            RefreshMinimap();
            evt.StopPropagation();
        }

        private void ApplyContentTransform()
        {
            _content.style.translate = new StyleTranslate(new Translate(_panOffset.x, _panOffset.y, 0));
            _content.style.scale = new StyleScale(new Scale(new Vector3(_zoomLevel, _zoomLevel, 1f)));
        }

        public Vector2 PanelToCanvasPosition(Vector2 panelPos)
        {
            return _content.WorldToLocal(panelPos);
        }

        public void RefreshMinimap()
        {
            if (_graph == null) return;
            _minimap?.Refresh(_nodeWidgets, _panOffset, _viewport.contentRect, _zoomLevel);
        }

    }
}
