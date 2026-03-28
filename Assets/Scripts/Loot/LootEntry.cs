using System;
using Gridlock.Modules;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gridlock.Loot
{
    [Serializable]
    public class LootEntry
    {
        [PreviewField(40, ObjectFieldAlignment.Left), TableColumnWidth(55, Resizable = false)]
        public ModuleDefinition module;

        [ShowInInspector, ReadOnly, TableColumnWidth(100)]
        [LabelWidth(1)]
        public string Name => module != null ? module.displayName : "\u2014";

        [TableColumnWidth(90)]
        [EnumToggleButtons]
        public LootRarity rarity = LootRarity.Common;
    }
}
