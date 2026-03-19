using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIWE.NodeEditor.UI
{
    public class PortWidget : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
    {
        [SerializeField] private Image portImage;

        private NodeWidget _parentNode;
        private int _portIndex;
        private bool _isInput;
        private NodeEditorCanvas _canvas;

        private static PortWidget _dragSource;
        private static GameObject _tempLine;

        public NodeWidget ParentNode => _parentNode;
        public int PortIndex => _portIndex;
        public bool IsInput => _isInput;

        public void Initialize(NodeWidget parentNode, int portIndex, bool isInput, NodeEditorCanvas canvas)
        {
            _parentNode = parentNode;
            _portIndex = portIndex;
            _isInput = isInput;
            _canvas = canvas;

            if (portImage != null)
            {
                portImage.color = isInput ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isInput) return; // Can only drag from output ports
            _dragSource = this;
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Visual feedback could be added here (temp line)
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            _dragSource = null;
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (!_isInput) return; // Can only drop on input ports
            if (_dragSource == null) return;
            if (_dragSource.ParentNode == _parentNode) return; // No self-connections

            // Validate connection types:
            // Trigger -> Zone, Zone -> Zone or Zone -> Effect
            var fromCategory = _dragSource.ParentNode.Data.category;
            var toCategory = _parentNode.Data.category;

            bool valid = false;
            if (fromCategory == Data.ModuleCategory.Trigger && toCategory == Data.ModuleCategory.Zone) valid = true;
            if (fromCategory == Data.ModuleCategory.Zone && toCategory == Data.ModuleCategory.Zone) valid = true;
            if (fromCategory == Data.ModuleCategory.Zone && toCategory == Data.ModuleCategory.Effect) valid = true;

            if (!valid)
            {
                Debug.LogWarning($"[NodeEditor] Invalid connection: {fromCategory} -> {toCategory}");
                return;
            }

            _canvas?.AddConnection(
                _dragSource.ParentNode.NodeId, _dragSource.PortIndex,
                _parentNode.NodeId, _portIndex
            );
        }
    }
}
