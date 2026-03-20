using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class PortWidget
    {
        public VisualElement Element { get; }
        public NodeWidget ParentNode { get; }
        public int PortIndex { get; }
        public bool IsInput { get; }
        public Color PortColor { get; }

        private readonly NodeEditorCanvas _canvas;
        private static bool _isDragging;

        public static bool IsDraggingPort => _isDragging;

        public PortWidget(NodeWidget parentNode, int portIndex, bool isInput, Color color, NodeEditorCanvas canvas)
        {
            ParentNode = parentNode;
            PortIndex = portIndex;
            IsInput = isInput;
            PortColor = color;
            _canvas = canvas;

            Element = new VisualElement();
            Element.AddToClassList("port");
            Element.AddToClassList(isInput ? "port--input" : "port--output");
            Element.style.backgroundColor = new StyleColor(color);
            Element.userData = this;

            Element.RegisterCallback<PointerDownEvent>(OnPointerDown);
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0) return;
            _isDragging = true;
            _canvas?.StartPortDrag(this, evt);
            evt.StopPropagation();
        }

        public static void ResetDragState()
        {
            _isDragging = false;
        }
    }
}
