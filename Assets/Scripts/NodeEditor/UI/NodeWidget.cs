using System.Collections.Generic;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIWE.NodeEditor.UI
{
    public class NodeWidget : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerClickHandler
    {
        [Header("UI References")]
        [SerializeField] private Image headerImage;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private Image iconImage;
        [SerializeField] private RectTransform inputPortsContainer;
        [SerializeField] private RectTransform outputPortsContainer;
        [SerializeField] private PortWidget portPrefab;

        [Header("Colors")]
        [SerializeField] private Color triggerColor = new(1f, 0.6f, 0.2f);
        [SerializeField] private Color zoneColor = new(0.3f, 0.5f, 1f);
        [SerializeField] private Color effectColor = new(0.3f, 0.85f, 0.4f);

        private static readonly Color ChainColor = new(0.3f, 0.5f, 1f);
        private static readonly Color EffectPortColor = new(0.3f, 0.85f, 0.4f);

        private NodeData _nodeData;
        private NodeEditorCanvas _canvas;
        private RectTransform _rectTransform;
        private readonly List<PortWidget> _inputPorts = new();
        private readonly List<PortWidget> _outputPorts = new();
        private bool _isDragging;

        public string NodeId => _nodeData?.nodeId;
        public NodeData Data => _nodeData;

        public void Initialize(NodeData nodeData, NodeEditorCanvas canvas, ModuleRegistry registry = null)
        {
            _nodeData = nodeData;
            _canvas = canvas;
            _rectTransform = GetComponent<RectTransform>();

            var moduleDef = registry != null ? registry.GetById(nodeData.moduleDefId) : null;

            if (titleText != null)
                titleText.text = moduleDef != null ? moduleDef.displayName : nodeData.moduleDefId;

            if (iconImage != null && moduleDef != null && moduleDef.icon != null)
                iconImage.sprite = moduleDef.icon;

            var color = nodeData.category switch
            {
                ModuleCategory.Trigger => triggerColor,
                ModuleCategory.Zone => zoneColor,
                ModuleCategory.Effect => effectColor,
                _ => Color.gray
            };

            if (headerImage != null)
                headerImage.color = color;

            SetupPorts();
        }

        private void SetupPorts()
        {
            if (portPrefab == null) return;

            switch (_nodeData.category)
            {
                case ModuleCategory.Trigger:
                    CreateSidePort(outputPortsContainer, 0, false, ChainColor);
                    break;

                case ModuleCategory.Zone:
                    CreateSidePort(inputPortsContainer, 0, true, ChainColor);
                    CreateSidePort(outputPortsContainer, 0, false, ChainColor);
                    CreateVerticalPort(1, false, EffectPortColor);
                    break;

                case ModuleCategory.Effect:
                    CreateVerticalPort(0, true, EffectPortColor);
                    CreateVerticalPort(0, false, EffectPortColor);
                    break;
            }
        }

        private void CreateSidePort(RectTransform container, int index, bool isInput, Color color)
        {
            if (container == null) return;
            var port = Instantiate(portPrefab, container);
            port.Initialize(this, index, isInput, _canvas, color);
            if (isInput) _inputPorts.Add(port);
            else _outputPorts.Add(port);
        }

        private void CreateVerticalPort(int index, bool isInput, Color color)
        {
            var port = Instantiate(portPrefab, _rectTransform);
            port.Initialize(this, index, isInput, _canvas, color);

            var portRt = port.GetComponent<RectTransform>();
            if (isInput)
            {
                portRt.anchorMin = new Vector2(0.5f, 1f);
                portRt.anchorMax = new Vector2(0.5f, 1f);
                portRt.pivot = new Vector2(0.5f, 0.5f);
                portRt.anchoredPosition = new Vector2(0, 10);
                _inputPorts.Add(port);
            }
            else
            {
                portRt.anchorMin = new Vector2(0.5f, 0f);
                portRt.anchorMax = new Vector2(0.5f, 0f);
                portRt.pivot = new Vector2(0.5f, 0.5f);
                portRt.anchoredPosition = new Vector2(0, -10);
                _outputPorts.Add(port);
            }
        }

        public RectTransform GetInputPort(int index)
        {
            if (index < _inputPorts.Count)
                return _inputPorts[index].GetComponent<RectTransform>();
            return _rectTransform;
        }

        public RectTransform GetOutputPort(int index)
        {
            if (index < _outputPorts.Count)
                return _outputPorts[index].GetComponent<RectTransform>();
            return _rectTransform;
        }

        public Color GetOutputPortColor(int index)
        {
            if (index < _outputPorts.Count)
                return _outputPorts[index].PortColor;
            return Color.white;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (PortWidget.IsDraggingPort) return;
            transform.SetAsLastSibling();
            _isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (!_isDragging) return;

            var parentScale = _rectTransform.parent != null
                ? _rectTransform.parent.lossyScale.x
                : 1f;

            _rectTransform.anchoredPosition += eventData.delta / parentScale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_isDragging && _nodeData != null)
            {
                _nodeData.editorPosition = _rectTransform.anchoredPosition;
            }
            _isDragging = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Right)
            {
                _canvas?.RemoveNode(NodeId);
            }
        }
    }
}
