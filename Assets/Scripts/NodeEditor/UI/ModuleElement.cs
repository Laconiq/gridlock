using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public class ModuleElement : VisualElement
    {
        protected static Color ChainColor => DesignConstants.PortChain;
        protected static Color EffectPortColor => DesignConstants.PortEffect;

        public VisualElement LeftPorts { get; }
        public VisualElement RightPorts { get; }

        protected ModuleElement(string displayName, string categoryName, string cssClass)
        {
            AddToClassList("node");

            var colorBar = new VisualElement();
            colorBar.AddToClassList("node__color-bar");
            colorBar.AddToClassList($"node__color-bar--{cssClass}");
            Add(colorBar);

            var header = new VisualElement();
            header.AddToClassList("node__header");

            var headerLeft = new VisualElement();
            headerLeft.AddToClassList("node__header-left");

            var headerIcon = new VisualElement();
            headerIcon.AddToClassList("node__header-icon");
            var iconPath = DesignConstants.GetIconPath(cssClass);
            if (iconPath != null)
            {
                var tex = Resources.Load<Texture2D>(iconPath);
                if (tex != null)
                    headerIcon.style.backgroundImage = new StyleBackground(tex);
            }
            headerLeft.Add(headerIcon);

            var catLabel = new Label(categoryName);
            catLabel.AddToClassList("node__category-label");
            catLabel.AddToClassList($"node__category-label--{cssClass}");
            headerLeft.Add(catLabel);
            header.Add(headerLeft);
            Add(header);

            var body = new VisualElement();
            body.AddToClassList("node__body");

            var title = new Label(displayName.ToUpper());
            title.AddToClassList("node__title");
            body.Add(title);

            var portsRow = new VisualElement();
            portsRow.AddToClassList("node__ports-row");

            LeftPorts = new VisualElement { name = "left-ports" };
            LeftPorts.AddToClassList("node__port-group");
            RightPorts = new VisualElement { name = "right-ports" };
            RightPorts.AddToClassList("node__port-group");

            portsRow.Add(LeftPorts);
            portsRow.Add(RightPorts);
            body.Add(portsRow);
            Add(body);
        }

        public virtual void AddDisplayPorts() { }

        protected void AddPort(VisualElement container, Color color, string label, bool isInput)
        {
            var group = new VisualElement();
            group.style.flexDirection = FlexDirection.Row;
            group.style.alignItems = Align.Center;

            var dot = new VisualElement();
            dot.AddToClassList("port");
            dot.style.backgroundColor = new StyleColor(color);

            var lbl = new Label(label);
            lbl.AddToClassList("port__label");

            if (isInput) { group.Add(dot); group.Add(lbl); }
            else { group.Add(lbl); group.Add(dot); }
            container.Add(group);
        }

        protected void AddVerticalPort(Color color, bool top)
        {
            var port = new VisualElement();
            port.AddToClassList("port");
            port.AddToClassList(top ? "node__port-vertical-top" : "node__port-vertical-bottom");
            port.style.backgroundColor = new StyleColor(color);
            Add(port);
        }

        public static ModuleElement Create(string displayName, ModuleCategory category)
        {
            ModuleElement el = category switch
            {
                ModuleCategory.Trigger => new TriggerModuleElement(displayName),
                ModuleCategory.Zone => new ZoneModuleElement(displayName),
                ModuleCategory.Effect => new EffectModuleElement(displayName),
                _ => null
            };
            return el;
        }

        public static ModuleElement CreateDisplay(string displayName, ModuleCategory category)
        {
            var el = Create(displayName, category);
            if (el != null)
            {
                el.AddDisplayPorts();
                el.AddToClassList("node--display");
            }
            return el;
        }
    }

    public class TriggerModuleElement : ModuleElement
    {
        public TriggerModuleElement(string displayName)
            : base(displayName, "TRIGGER", "trigger") { }

        public override void AddDisplayPorts()
        {
            AddPort(RightPorts, ChainColor, "OUT", false);
        }
    }

    public class ZoneModuleElement : ModuleElement
    {
        public ZoneModuleElement(string displayName)
            : base(displayName, "ZONE", "zone")
        {
        }

        public override void AddDisplayPorts()
        {
            AddPort(LeftPorts, ChainColor, "IN", true);
            AddPort(RightPorts, ChainColor, "OUT", false);
            AddVerticalPort(EffectPortColor, false);
        }
    }

    public class EffectModuleElement : ModuleElement
    {
        public EffectModuleElement(string displayName)
            : base(displayName, "EFFECT", "effect")
        {
            var statusBar = new VisualElement();
            statusBar.AddToClassList("node__status-bar");
            var statusText = new Label("ACTIVE");
            statusText.AddToClassList("node__status-text");
            statusBar.Add(statusText);
            Add(statusBar);
        }

        public override void AddDisplayPorts()
        {
            AddVerticalPort(EffectPortColor, true);
            AddVerticalPort(EffectPortColor, false);
        }
    }
}
