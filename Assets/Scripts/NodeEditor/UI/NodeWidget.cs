using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class NodeWidget
    {
        public VisualElement Root { get; }
        public string NodeId => _nodeData?.nodeId;
        public NodeData Data => _nodeData;
        public Vector2 Position => _position;
        public IReadOnlyList<PortWidget> AllPorts => _ports;

        private readonly NodeData _nodeData;
        private readonly NodeEditorCanvas _canvas;
        private readonly List<PortWidget> _ports = new();
        private readonly List<PortWidget> _inputPorts = new();
        private readonly List<PortWidget> _outputPorts = new();

        private Vector2 _position;

        private static Color ChainColor => DesignConstants.PortChain;
        private static Color EffectPortColor => DesignConstants.PortEffect;

        public NodeWidget(NodeData nodeData, NodeEditorCanvas canvas, ModuleRegistry registry)
        {
            _nodeData = nodeData;
            _canvas = canvas;

            var moduleDef = registry?.GetById(nodeData.moduleDefId);
            var displayName = moduleDef != null ? moduleDef.displayName : nodeData.moduleDefId;

            Root = ModuleElement.Create(displayName, nodeData.category);
            if (Root == null)
            {
                Root = new VisualElement();
                Root.AddToClassList("node");
                return;
            }

            if (nodeData.isFixed) Root.AddToClassList("node--fixed");

            AddCooldownLabel(moduleDef);
            SetupInteractivePorts();
            Root.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        private void SetupInteractivePorts()
        {
            var moduleEl = Root as ModuleElement;
            if (moduleEl == null) return;

            switch (_nodeData.category)
            {
                case ModuleCategory.Trigger:
                    CreatePort(moduleEl.RightPorts, 0, false, ChainColor, "OUT");
                    break;
                case ModuleCategory.Zone:
                    CreatePort(moduleEl.LeftPorts, 0, true, ChainColor, "IN");
                    CreatePort(moduleEl.RightPorts, 0, false, ChainColor, "OUT");
                    CreateVerticalPort(1, false, EffectPortColor);
                    break;
                case ModuleCategory.Effect:
                    CreateVerticalPort(0, true, EffectPortColor);
                    CreateVerticalPort(0, false, EffectPortColor);
                    break;
            }
        }

        private void CreatePort(VisualElement container, int index, bool isInput, Color color, string label)
        {
            var portGroup = new VisualElement();
            portGroup.style.flexDirection = FlexDirection.Row;
            portGroup.style.alignItems = Align.Center;

            var port = new PortWidget(this, index, isInput, color, _canvas);
            _ports.Add(port);
            if (isInput) _inputPorts.Add(port);
            else _outputPorts.Add(port);

            if (isInput)
            {
                portGroup.Add(port.Element);
                var lbl = new Label(label);
                lbl.AddToClassList("port__label");
                portGroup.Add(lbl);
            }
            else
            {
                var lbl = new Label(label);
                lbl.AddToClassList("port__label");
                portGroup.Add(lbl);
                portGroup.Add(port.Element);
            }

            container.Add(portGroup);
        }

        private void AddCooldownLabel(ModuleDefinition moduleDef)
        {
            if (moduleDef == null) return;
            float cd = moduleDef.GetCooldown();
            if (cd <= 0f) return;

            var body = Root.Q(className: "node__body");
            if (body == null) return;

            var label = new Label($"CD: {cd:F1}s");
            label.AddToClassList("node__cooldown-label");
            body.Add(label);
        }

        private void CreateVerticalPort(int index, bool isInput, Color color)
        {
            var port = new PortWidget(this, index, isInput, color, _canvas);
            _ports.Add(port);
            if (isInput) _inputPorts.Add(port);
            else _outputPorts.Add(port);

            port.Element.AddToClassList(isInput ? "node__port-vertical-top" : "node__port-vertical-bottom");
            Root.Add(port.Element);
        }

        public PortWidget GetInputPort(int index)
        {
            return index < _inputPorts.Count ? _inputPorts[index] : null;
        }

        public PortWidget GetOutputPort(int index)
        {
            return index < _outputPorts.Count ? _outputPorts[index] : null;
        }

        public void SetPosition(Vector2 pos)
        {
            _position = pos;
            Root.style.left = pos.x;
            Root.style.top = pos.y;
        }

        public void UpdatePositionSilent(Vector2 contentPos)
        {
            _position = contentPos;
            if (_nodeData != null)
                _nodeData.editorPosition = contentPos;
        }

        public void RemoveFromCanvas()
        {
            Root.RemoveFromHierarchy();
        }

        public void PlaySpawnAnimation()
        {
            Root.AddToClassList("node--spawn-line");
            Root.schedule.Execute(() =>
            {
                Root.RemoveFromClassList("node--spawn-line");
            }).ExecuteLater(16);
        }

        public void PlayDespawnAnimation(System.Action onComplete)
        {
            Root.AddToClassList("node--despawning");
            Root.schedule.Execute(() =>
            {
                onComplete?.Invoke();
            }).ExecuteLater(200);
        }

        public void EndSpawnDrag()
        {
            Root.RemoveFromClassList("node--dragging");
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            if (PortWidget.IsDraggingPort) return;

            _canvas.BeginNodeDrag(this, evt);
            evt.StopPropagation();
        }

        public static VisualElement CreateDisplayNode(string displayName, ModuleCategory category)
        {
            return ModuleElement.CreateDisplay(displayName, category);
        }
    }
}
