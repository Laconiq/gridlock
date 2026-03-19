using System.Collections.Generic;
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

        private NodeData _nodeData;
        private NodeEditorCanvas _canvas;
        private RectTransform _rectTransform;
        private readonly List<PortWidget> _inputPorts = new();
        private readonly List<PortWidget> _outputPorts = new();

        public string NodeId => _nodeData?.nodeId;
        public NodeData Data => _nodeData;

        public void Initialize(NodeData nodeData, NodeEditorCanvas canvas)
        {
            _nodeData = nodeData;
            _canvas = canvas;
            _rectTransform = GetComponent<RectTransform>();

            if (titleText != null)
                titleText.text = nodeData.moduleDefId;

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

            // Input port (all except Triggers)
            if (_nodeData.category != ModuleCategory.Trigger && inputPortsContainer != null)
            {
                var port = Instantiate(portPrefab, inputPortsContainer);
                port.Initialize(this, 0, true, _canvas);
                _inputPorts.Add(port);
            }

            // Output port (all except Effects)
            if (_nodeData.category != ModuleCategory.Effect && outputPortsContainer != null)
            {
                var port = Instantiate(portPrefab, outputPortsContainer);
                port.Initialize(this, 0, false, _canvas);
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

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            transform.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;

            var parentScale = _rectTransform.parent != null
                ? _rectTransform.parent.lossyScale.x
                : 1f;

            _rectTransform.anchoredPosition += eventData.delta / parentScale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_nodeData != null)
            {
                _nodeData.editorPosition = _rectTransform.anchoredPosition;
            }
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
