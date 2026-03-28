using System;
using Gridlock.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.RadialMenu
{
    public class RadialSegment : VisualElement
    {
        public string NodeId { get; }
        public event Action<string> OnClicked;

        public RadialSegment(string nodeId, string displayName, ModuleCategory category)
        {
            NodeId = nodeId;
            AddToClassList("radial-segment");

            var catClass = category switch
            {
                ModuleCategory.Trigger => "radial-segment--trigger",
                ModuleCategory.Zone => "radial-segment--zone",
                ModuleCategory.Effect => "radial-segment--effect",
                _ => ""
            };
            if (!string.IsNullOrEmpty(catClass))
                AddToClassList(catClass);

            var nameLabel = new Label(displayName.ToUpper());
            nameLabel.AddToClassList("radial-segment__name");
            Add(nameLabel);

            RegisterCallback<ClickEvent>(_ => OnClicked?.Invoke(NodeId));
        }

        public void SetPolarPosition(float centerX, float centerY, float radius, float angleDeg)
        {
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float halfW = resolvedStyle.width > 0 ? resolvedStyle.width * 0.5f : 55f;
            float halfH = resolvedStyle.height > 0 ? resolvedStyle.height * 0.5f : 35f;
            float x = centerX + radius * Mathf.Cos(angleRad) - halfW;
            float y = centerY + radius * Mathf.Sin(angleRad) - halfH;
            style.left = x;
            style.top = y;
        }
    }
}
