using System;
using System.Collections.Generic;
using Gridlock.Mods;

namespace Gridlock.Loot
{
    public sealed class LootTable
    {
        public float DropChance { get; set; } = 0.5f;

        public List<RarityWeight> RarityWeights { get; set; } = new()
        {
            new() { Rarity = Rarity.Common, Weight = 50f },
            new() { Rarity = Rarity.Uncommon, Weight = 30f },
            new() { Rarity = Rarity.Rare, Weight = 15f },
            new() { Rarity = Rarity.Epic, Weight = 5f },
        };

        private static readonly Random _rng = new();

        public ModType Roll()
        {
            var rarity = RollRarity();
            return PickModOfRarity(rarity);
        }

        private Rarity RollRarity()
        {
            float totalWeight = 0f;
            foreach (var rw in RarityWeights) totalWeight += rw.Weight;

            float roll = (float)(_rng.NextDouble() * totalWeight);
            float cumulative = 0f;
            foreach (var rw in RarityWeights)
            {
                cumulative += rw.Weight;
                if (roll <= cumulative) return rw.Rarity;
            }
            return Rarity.Common;
        }

        private static ModType PickModOfRarity(Rarity rarity)
        {
            var candidates = new List<ModType>();
            foreach (ModType type in Enum.GetValues(typeof(ModType)))
            {
                if (ModRarity.GetRarity(type) == rarity)
                    candidates.Add(type);
            }

            if (candidates.Count == 0) return ModType.Heavy;
            return candidates[_rng.Next(candidates.Count)];
        }
    }

    public struct RarityWeight
    {
        public Rarity Rarity;
        public float Weight;
    }
}
