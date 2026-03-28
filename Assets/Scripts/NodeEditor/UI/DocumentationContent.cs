using Gridlock.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.NodeEditor.UI
{
    public static class DocumentationContent
    {
        public static PopupPanel Build()
        {
            var popup = new PopupPanel("SYSTEM_DOCUMENTATION");

            popup.AddPage(BuildOverviewPage());
            popup.AddPage(BuildModulePage(
                "WHEN", DesignConstants.HexTrigger, "TRIGGER_MODULES",
                "Triggers are the entry point of every module chain. They define WHEN the chain activates.\n\n" +
                "A trigger fires based on a condition: a timer interval, an enemy entering range, or a player input like left/right click.",
                new[] {
                    "\u25b6 Only output ports (no inputs)",
                    "\u25b6 Connects horizontally to Zone modules",
                    "\u25b6 Multiple triggers can exist per chassis"
                },
                "ON_TIMER", ModuleCategory.Trigger));
            popup.AddPage(BuildModulePage(
                "WHERE / WHO", DesignConstants.HexTarget, "TARGET_MODULES",
                "Targets select WHERE and WHO is affected. They filter targets from the game world based on spatial rules.\n\n" +
                "Targets can chain horizontally into other targets for filtering. They connect downward (vertical port) to effects.",
                new[] {
                    "\u25b6 Input + output horizontal ports",
                    "\u25b6 Vertical output to Effect modules",
                    "\u25b6 Can chain: Target \u25b6 Target for filtering"
                },
                "ALL_IN_RANGE", ModuleCategory.Zone));
            popup.AddPage(BuildModulePage(
                "WHAT", DesignConstants.HexEffect, "EFFECT_MODULES",
                "Effects define WHAT happens to the selected targets. They execute concrete actions like firing projectiles, dealing damage, or applying debuffs.\n\n" +
                "Effects chain vertically: one effect can trigger another below it.",
                new[] {
                    "\u25b6 Vertical input + output ports",
                    "\u25b6 Can chain: Effect \u25b6 Effect vertically",
                    "\u25b6 Always at the end of a chain"
                },
                "PROJECTILE", ModuleCategory.Effect));

            return popup;
        }

        private static VisualElement BuildOverviewPage()
        {
            var page = new VisualElement();

            var tag = new Label("OVERVIEW");
            tag.AddToClassList("doc-module-page__tag");
            tag.style.color = new StyleColor(DesignConstants.PrimaryDim);
            page.Add(tag);

            var title = new Label("DEFAULT_TOWER_LOADOUT");
            title.AddToClassList("doc-module-page__title");
            page.Add(title);

            var desc = new Label(
                "Every tower starts with this chain.\n" +
                "Blue cables = horizontal flow. Green cables = vertical flow.");
            desc.AddToClassList("doc-module-page__desc");
            page.Add(desc);

            var diagram = new VisualElement();
            diagram.AddToClassList("chain-diagram");

            var container = new VisualElement();
            container.AddToClassList("chain-diagram__container");
            container.style.height = 280;

            float tX = 100f, tY = -20f;
            float zX = 360f, zY = -20f;
            float eX = 368f, eY = 150f;

            var triggerNode = PlaceNode("ON_TIMER", ModuleCategory.Trigger, tX, tY);
            var zoneNode = PlaceNode("NEAREST_ENEMY", ModuleCategory.Zone, zX, zY);
            var effectNode = PlaceNode("PROJECTILE", ModuleCategory.Effect, eX, eY);

            container.Add(triggerNode);
            container.Add(zoneNode);
            container.Add(effectNode);

            // Find port dots after layout to draw cables at their real positions
            var cables = new CableRenderer(animated: true);
            container.Add(cables);

            container.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                cables.Clear();

                // Trigger OUT port (right-side dot)
                var triggerOut = FindPortDot(triggerNode, "right-ports");
                // Zone IN port (left-side dot) and vertical bottom port
                var zoneIn = FindPortDot(zoneNode, "left-ports");
                var zoneBottom = FindVerticalPort(zoneNode, false);
                // Effect top vertical port
                var effectTop = FindVerticalPort(effectNode, true);

                if (triggerOut != null && zoneIn != null)
                {
                    var from = CenterInParent(triggerOut, container);
                    var to = CenterInParent(zoneIn, container);
                    cables.AddCable(from, to, DesignConstants.PortChain, false);
                }

                if (zoneBottom != null && effectTop != null)
                {
                    var from = CenterInParent(zoneBottom, container);
                    var to = CenterInParent(effectTop, container);
                    cables.AddCable(from, to, DesignConstants.PortEffect, true);
                }
            });

            diagram.Add(container);
            page.Add(diagram);
            return page;
        }

        private static VisualElement BuildModulePage(string role, string hexColor,
            string titleText, string desc, string[] rules,
            string exampleName, ModuleCategory category)
        {
            var page = new VisualElement();

            var tag = new Label(role);
            tag.AddToClassList("doc-module-page__tag");
            if (ColorUtility.TryParseHtmlString(hexColor, out var tagColor))
                tag.style.color = new StyleColor(tagColor);
            page.Add(tag);

            var title = new Label(titleText);
            title.AddToClassList("doc-module-page__title");
            page.Add(title);

            var descLabel = new Label(desc);
            descLabel.AddToClassList("doc-module-page__desc");
            page.Add(descLabel);

            // Side-by-side: node + rules
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            row.style.marginTop = 16;

            var preview = new VisualElement();
            preview.style.marginRight = 24;
            preview.style.flexShrink = 0;
            preview.Add(ModuleElement.CreateDisplay(exampleName, category));
            row.Add(preview);

            var rulesBox = new VisualElement();
            rulesBox.AddToClassList("doc-module-page__rules");
            rulesBox.style.flexGrow = 1;
            if (ColorUtility.TryParseHtmlString(hexColor, out var ruleColor))
                rulesBox.style.borderLeftColor = new StyleColor(ruleColor);

            foreach (var rule in rules)
            {
                var lbl = new Label(rule);
                lbl.AddToClassList("doc-module-page__rule");
                rulesBox.Add(lbl);
            }
            row.Add(rulesBox);

            page.Add(row);
            return page;
        }

        private static VisualElement PlaceNode(string name, ModuleCategory cat, float x, float y)
        {
            var node = ModuleElement.CreateDisplay(name, cat);
            node.style.position = Position.Absolute;
            node.style.left = x;
            node.style.top = y;
            return node;
        }

        private static VisualElement FindPortDot(VisualElement node, string portGroupName)
        {
            var group = node.Q(portGroupName);
            return group?.Q(className: "port");
        }

        private static VisualElement FindVerticalPort(VisualElement node, bool top)
        {
            var className = top ? "node__port-vertical-top" : "node__port-vertical-bottom";
            return node.Q(className: className);
        }

        private static Vector2 CenterInParent(VisualElement element, VisualElement parent)
        {
            var elBound = element.worldBound;
            var parentBound = parent.worldBound;
            return new Vector2(
                elBound.center.x - parentBound.x,
                elBound.center.y - parentBound.y
            );
        }
    }
}
