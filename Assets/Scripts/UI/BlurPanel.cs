using Gridlock.Visual;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.UI
{
    [UxmlElement]
    public partial class BlurPanel : VisualElement
    {
        private IVisualElementScheduledItem _updateTask;

        public BlurPanel()
        {
            AddToClassList("blur-panel");
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        private void OnAttach(AttachToPanelEvent evt)
        {
            _updateTask = schedule.Execute(UpdateBlur).Every(16);
        }

        private void OnDetach(DetachFromPanelEvent evt)
        {
            _updateTask?.Pause();
            _updateTask = null;
            style.backgroundImage = StyleKeyword.None;
        }

        private void UpdateBlur()
        {
            var blurTex = UIBlurCapture.BlurTexture;
            if (blurTex == null)
            {
                style.backgroundImage = StyleKeyword.None;
                return;
            }

            style.backgroundImage = Background.FromRenderTexture(blurTex);

            var panelRoot = panel?.visualTree;
            if (panelRoot == null) return;

            float panelW = panelRoot.worldBound.width;
            float panelH = panelRoot.worldBound.height;
            if (panelW <= 0f || panelH <= 0f) return;

            var rect = worldBound;

            style.backgroundSize = new BackgroundSize(
                new Length(panelW, LengthUnit.Pixel),
                new Length(panelH, LengthUnit.Pixel)
            );

            style.backgroundPositionX = new BackgroundPosition(
                BackgroundPositionKeyword.Left,
                new Length(-rect.x, LengthUnit.Pixel)
            );
            style.backgroundPositionY = new BackgroundPosition(
                BackgroundPositionKeyword.Top,
                new Length(-rect.y, LengthUnit.Pixel)
            );
        }
    }
}
