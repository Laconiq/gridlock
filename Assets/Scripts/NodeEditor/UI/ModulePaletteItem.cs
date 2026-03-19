using System;
using AIWE.Modules;
using AIWE.NodeEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AIWE.NodeEditor.UI
{
    public class ModulePaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image colorBar;

        private ModuleDefinition _definition;
        private NodeEditorCanvas _canvas;
        private GameObject _ghost;

        public void Initialize(ModuleDefinition definition, NodeEditorCanvas canvas)
        {
            _definition = definition;
            _canvas = canvas;

            if (nameText != null)
                nameText.text = definition.displayName;

            if (colorBar != null)
                colorBar.color = definition.nodeColor;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _ghost = new GameObject("DragGhost");
            _ghost.transform.SetParent(transform.root, false);
            var rt = _ghost.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(140, 40);
            var img = _ghost.AddComponent<Image>();
            img.color = new Color(_definition.nodeColor.r, _definition.nodeColor.g, _definition.nodeColor.b, 0.5f);
            img.raycastTarget = false;

            var text = new GameObject("Text").AddComponent<TextMeshProUGUI>();
            text.transform.SetParent(_ghost.transform, false);
            text.text = _definition.displayName;
            text.fontSize = 12;
            text.alignment = TextAlignmentOptions.Center;
            text.raycastTarget = false;
            var textRt = text.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_ghost != null)
            {
                _ghost.transform.position = eventData.position;
            }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_ghost != null)
            {
                Destroy(_ghost);
            }

            // Check if dropped on the canvas area
            if (_canvas == null || _definition == null) return;

            // Create a new node at the drop position
            var nodeData = new NodeData
            {
                moduleDefId = _definition.moduleId,
                category = _definition.category,
                editorPosition = eventData.position
            };

            // Convert screen position to canvas local position
            var canvasRt = _canvas.GetComponent<RectTransform>();
            if (canvasRt != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRt, eventData.position, eventData.pressEventCamera, out var localPos);
                nodeData.editorPosition = localPos;
            }

            _canvas.AddNode(nodeData);
        }
    }
}
