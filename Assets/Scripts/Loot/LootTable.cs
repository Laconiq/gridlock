using System;
using System.Collections.Generic;
using System.Linq;
using Gridlock.Modules;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gridlock.Loot
{
    [CreateAssetMenu(menuName = "Gridlock/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public class RarityWeight
        {
            [HorizontalGroup("Row"), LabelWidth(70)]
            [ReadOnly]
            public LootRarity rarity;

            [HorizontalGroup("Row"), LabelWidth(50)]
            [MinValue(0)]
            public int weight = 10;

            [HorizontalGroup("Row"), LabelWidth(1)]
            [ShowInInspector, ReadOnly]
            [ProgressBar(0, 100, ColorGetter = nameof(GetBarColor))]
            public float Percent { get; set; }

            [HorizontalGroup("Row"), LabelWidth(1)]
            [ShowInInspector, ReadOnly]
            [ProgressBar(0, 100, r: 0.3f, g: 0.6f, b: 1f)]
            public float SimPercent { get; set; }

            [HideInInspector]
            private Color GetBarColor()
            {
                return rarity switch
                {
                    LootRarity.Common => new Color(0.6f, 0.6f, 0.6f),
                    LootRarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
                    LootRarity.Rare => new Color(0.3f, 0.5f, 1f),
                    LootRarity.Epic => new Color(0.7f, 0.2f, 0.9f),
                    _ => Color.white
                };
            }
        }

        [Title("Rarity Weights")]
        [InfoBox("Roll 1: pick a rarity tier. Roll 2: pick a random module from that tier.")]
        [ListDrawerSettings(DraggableItems = false, HideAddButton = true, HideRemoveButton = true)]
        [OnValueChanged(nameof(RecalculateProbabilities), IncludeChildren = true)]
        [SerializeField]
        private List<RarityWeight> rarityWeights = new()
        {
            new() { rarity = LootRarity.Common, weight = 50 },
            new() { rarity = LootRarity.Uncommon, weight = 30 },
            new() { rarity = LootRarity.Rare, weight = 15 },
            new() { rarity = LootRarity.Epic, weight = 5 },
        };

        [Title("Module Pool")]
        [TableList(AlwaysExpanded = true, DrawScrollView = true, MaxScrollViewHeight = 400)]
        [SerializeField]
        private List<LootEntry> entries = new();

        [Title("Settings")]
        [SerializeField, Range(0f, 1f), PropertyOrder(10)]
        private float dropChance = 0.5f;

        [Title("Pool Preview"), PropertyOrder(12)]
        [ShowInInspector, ReadOnly]
        private string CommonPool => GetPoolPreview(LootRarity.Common);
        [ShowInInspector, ReadOnly]
        private string UncommonPool => GetPoolPreview(LootRarity.Uncommon);
        [ShowInInspector, ReadOnly]
        private string RarePool => GetPoolPreview(LootRarity.Rare);
        [ShowInInspector, ReadOnly]
        private string EpicPool => GetPoolPreview(LootRarity.Epic);

        [Title("Simulation"), PropertyOrder(20)]
        [SerializeField, Range(100, 100000)]
        private int simulationRolls = 10000;

        public List<LootEntry> Entries => entries;

        [Button("Run Simulation", ButtonSizes.Large), GUIColor(0.4f, 0.8f, 1f), PropertyOrder(21)]
        private void RunSimulation()
        {
            var counts = new Dictionary<LootRarity, int>();
            foreach (LootRarity r in Enum.GetValues(typeof(LootRarity)))
                counts[r] = 0;

            for (int i = 0; i < simulationRolls; i++)
            {
                var rarity = RollRarity();
                if (rarity.HasValue)
                    counts[rarity.Value]++;
            }

            foreach (var rw in rarityWeights)
                rw.SimPercent = (counts[rw.rarity] / (float)simulationRolls) * 100f;
        }

        [Button("Clear Simulation"), PropertyOrder(22)]
        private void ClearSimulation()
        {
            foreach (var rw in rarityWeights)
                rw.SimPercent = 0f;
        }

        private void RecalculateProbabilities()
        {
            float total = rarityWeights.Sum(r => r.weight);
            foreach (var rw in rarityWeights)
                rw.Percent = total > 0f ? (rw.weight / total) * 100f : 0f;
        }

        private void OnValidate() => RecalculateProbabilities();
        private void OnEnable() => RecalculateProbabilities();

        private string GetPoolPreview(LootRarity rarity)
        {
            var modules = entries.Where(e => e.rarity == rarity && e.module != null).ToList();
            if (modules.Count == 0) return "(empty)";
            return string.Join(", ", modules.Select(e => e.module.displayName));
        }

        private LootRarity? RollRarity()
        {
            float total = rarityWeights.Sum(r => r.weight);
            if (total <= 0f) return null;

            float roll = UnityEngine.Random.value * total;
            float cumulative = 0f;

            foreach (var rw in rarityWeights)
            {
                cumulative += rw.weight;
                if (roll < cumulative) return rw.rarity;
            }

            return rarityWeights[^1].rarity;
        }

        public ModuleDefinition Roll()
        {
            if (UnityEngine.Random.value > dropChance) return null;
            if (entries.Count == 0) return null;

            var rarity = RollRarity();
            if (!rarity.HasValue) return null;

            var pool = entries.Where(e => e.rarity == rarity.Value && e.module != null).ToList();
            if (pool.Count == 0) return null;

            return pool[UnityEngine.Random.Range(0, pool.Count)].module;
        }
    }
}
