using System.Collections.Generic;
using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIWE.NodeEditor.UI
{
    public class PortWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private Image portImage;

        private NodeWidget _parentNode;
        private int _portIndex;
        private bool _isInput;
        private NodeEditorCanvas _canvas;
        private Color _portColor;

        private static PortWidget _dragSource;
        private static bool _dragFromInput;
        private static GameObject _dragLine;
        private static RectTransform _dragLineParentRt;

        public NodeWidget ParentNode => _parentNode;
        public int PortIndex => _portIndex;
        public bool IsInput => _isInput;
        public Color PortColor => _portColor;
        public static bool IsDraggingPort => _dragSource != null;

        public void Initialize(NodeWidget parentNode, int portIndex, bool isInput, NodeEditorCanvas canvas, Color color)
        {
            _parentNode = parentNode;
            _portIndex = portIndex;
            _isInput = isInput;
            _canvas = canvas;
            _portColor = color;

            if (portImage != null)
                portImage.color = color;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragSource = this;
            _dragFromInput = _isInput;

            var rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
            _dragLineParentRt = rootCanvas.GetComponent<RectTransform>();

            _dragLine = new GameObject("DragLine");
            _dragLine.transform.SetParent(rootCanvas.transform, false);
            var lineImg = _dragLine.AddComponent<Image>();
            lineImg.color = _portColor;
            lineImg.raycastTarget = false;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_dragSource != this || _dragLine == null) return;

            var cam = eventData.pressEventCamera;

            var portScreenPos = RectTransformUtility.WorldToScreenPoint(cam, transform.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragLineParentRt, portScreenPos, cam, out var localStart);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragLineParentRt, eventData.position, cam, out var localEnd);

            var diff = localEnd - localStart;
            float distance = diff.magnitude;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg - 90f;

            var lineRt = _dragLine.GetComponent<RectTransform>();
            lineRt.anchoredPosition = localStart;
            lineRt.sizeDelta = new Vector2(3, distance);
            lineRt.pivot = new Vector2(0.5f, 0);
            lineRt.localRotation = Quaternion.Euler(0, 0, angle);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_dragSource != this) return;

            if (_dragLine != null)
            {
                Destroy(_dragLine);
                _dragLine = null;
            }

            var target = FindPortUnderCursor(eventData);
            if (target != null && target != this && target._parentNode != _parentNode)
            {
                PortWidget output, input;
                if (_dragFromInput)
                {
                    if (!target._isInput) { output = target; input = this; }
                    else { _dragSource = null; return; }
                }
                else
                {
                    if (target._isInput) { output = this; input = target; }
                    else { _dragSource = null; return; }
                }

                TryConnect(output, input);
            }

            _dragSource = null;
        }

        private PortWidget FindPortUnderCursor(PointerEventData eventData)
        {
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);

            foreach (var result in results)
            {
                var port = result.gameObject.GetComponent<PortWidget>();
                if (port != null && port != this) return port;
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

            _canvas?.AddConnection(
                from.ParentNode.NodeId, from.PortIndex,
                to.ParentNode.NodeId, to.PortIndex
            );
        }
    }
}
