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
        private int _count;
        private bool _hasInventory;
        private CanvasGroup _canvasGroup;

        public void Initialize(ModuleDefinition definition, NodeEditorCanvas canvas, int count = -1)
        {
            _definition = definition;
            _canvas = canvas;
            _hasInventory = count >= 0;
            _count = count;

            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            UpdateDisplay();
        }

        public void AdjustCount(int delta)
        {
            if (!_hasInventory) return;
            _count = Mathf.Max(0, _count + delta);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            if (nameText != null)
            {
                nameText.text = _hasInventory
                    ? $"{_definition.displayName} (x{_count})"
                    : _definition.displayName;
            }

            if (colorBar != null)
                colorBar.color = _definition.nodeColor;

            if (_canvasGroup != null)
                _canvasGroup.alpha = (_hasInventory && _count <= 0) ? 0.4f : 1f;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_hasInventory && _count <= 0) return;

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

            if (_canvas == null || _definition == null) return;
            if (_hasInventory && _count <= 0) return;

            var nodeData = new NodeData
            {
                moduleDefId = _definition.moduleId,
                category = _definition.category,
                editorPosition = eventData.position
            };

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
