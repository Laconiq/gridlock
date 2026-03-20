using AIWE.NodeEditor.Data;
using UnityEngine;
using UnityEngine.UIElements;

namespace AIWE.NodeEditor.UI
{
    public static class DocumentationContent
    {
        private const float NodeWidth = DesignConstants.NodeWidth;
        private const float SidePortY = DesignConstants.SidePortY;
        private const float NodeHeight = DesignConstants.NodeHeight;

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
                "WHERE / WHO", DesignConstants.HexZone, "ZONE_MODULES",
                "Zones select WHERE and WHO is affected. They filter targets from the game world based on spatial rules.\n\n" +
                "Zones can chain horizontally into other zones for filtering. They connect downward (vertical port) to effects.",
                new[] {
                    "\u25b6 Input + output horizontal ports",
                    "\u25b6 Vertical output to Effect modules",
                    "\u25b6 Can chain: Zone \u25b6 Zone for filtering"
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

            var title = new Label("MODULE_CHAIN_ARCHITECTURE");
            title.AddToClassList("doc-module-page__title");
            page.Add(title);

            var desc = new Label(
                "Modules chain together to create behaviors. Each chain follows:\n" +
                "TRIGGER (when) \u2500\u25b6 ZONE (where) \u2500\u25b6 EFFECT (what)\n" +
                "Zones connect vertically down to Effects. Effects chain to other Effects.");
            desc.AddToClassList("doc-module-page__desc");
            page.Add(desc);

            // Diagram
            float tX = 80f,  tY = 15f;
            float zX = 350f, zY = 15f;
            float eX = 350f, eY = 170f;

            var diagram = new VisualElement();
            diagram.AddToClassList("chain-diagram");

            var container = new VisualElement();
            container.AddToClassList("chain-diagram__container");
            container.style.height = 300;

            container.Add(PlaceNode("ON_ENEMY_ENTER", ModuleCategory.Trigger, tX, tY));
            container.Add(PlaceNode("NEAREST_ENEMY", ModuleCategory.Zone, zX, zY));
            container.Add(PlaceNode("PROJECTILE", ModuleCategory.Effect, eX, eY));

            var cables = new CableRenderer();
            // Trigger OUT → Zone IN (horizontal)
            cables.AddCable(
                new Vector2(tX + NodeWidth, tY + SidePortY),
                new Vector2(zX, zY + SidePortY),
                DesignConstants.PortChain, false);
            // Zone bottom → Effect top (vertical)
            cables.AddCable(
                new Vector2(zX + NodeWidth * 0.5f, zY + NodeHeight),
                new Vector2(eX + NodeWidth * 0.5f, eY),
                DesignConstants.PortEffect, true);
            container.Add(cables);

            // Labels
            var chainLabelColor = DesignConstants.PortChain;
            chainLabelColor.a = 0.4f;
            var effectLabelColor = DesignConstants.PortEffect;
            effectLabelColor.a = 0.4f;
            container.Add(MakeLabel("HORIZONTAL_CHAIN", tX + NodeWidth + 10, tY + SidePortY - 16, chainLabelColor));
            container.Add(MakeLabel("VERTICAL_CHAIN", zX + NodeWidth * 0.5f + 10, zY + NodeHeight + 15, effectLabelColor));

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

        private static Label MakeLabel(string text, float x, float y, Color color)
        {
            var lbl = new Label(text);
            lbl.style.position = Position.Absolute;
            lbl.style.left = x;
            lbl.style.top = y;
            lbl.style.fontSize = 8;
            lbl.style.color = new StyleColor(color);
            lbl.style.letterSpacing = 2;
            return lbl;
        }
    }
}
