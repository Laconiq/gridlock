using Gridlock.Loot;
using Gridlock.UI;
using UnityEngine;
using UnityEngine.UIElements;

namespace Gridlock.Mods.UI
{
    public static class ModTileFactory
    {
        public static VisualElement Create(ModType type, string baseClass = "inventory-tile", bool blur = false)
        {
            VisualElement tile = blur ? new BlurPanel() : new VisualElement();
            tile.AddToClassList(baseClass);
            tile.pickingMode = PickingMode.Ignore;

            var modColor = ModSlotColors.GetModColor(type);
            var rarity = ModRarity.GetRarity(type);
            var rarityColor = ModRarity.GetRarityColor(rarity);

            tile.style.borderLeftColor = rarityColor;
            tile.style.borderTopColor = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
            tile.style.borderRightColor = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);
            tile.style.borderBottomColor = new Color(rarityColor.r, rarityColor.g, rarityColor.b, 0.3f);

            var nameLabel = new Label(ModSlotColors.GetModDisplayName(type));
            nameLabel.AddToClassList(baseClass + "__name");
            nameLabel.style.color = modColor;
            nameLabel.pickingMode = PickingMode.Ignore;
            tile.Add(nameLabel);

            var rarityLabel = new Label(rarity.ToString().ToUpperInvariant());
            rarityLabel.AddToClassList(baseClass + "__rarity");
            rarityLabel.style.color = rarityColor;
            rarityLabel.pickingMode = PickingMode.Ignore;
            tile.Add(rarityLabel);

            return tile;
        }

        public static Label AddBadge(VisualElement tile, int quantity, string baseClass = "inventory-tile")
        {
            var badge = new Label($"x{quantity}");
            badge.AddToClassList(baseClass + "__badge");
            badge.pickingMode = PickingMode.Ignore;
            tile.Add(badge);
            return badge;
        }
    }
}
