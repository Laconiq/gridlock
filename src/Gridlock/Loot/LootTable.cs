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
        private static readonly Dictionary<Rarity, ModType[]> _candidatesByRarity = BuildCandidateCache();

        private static Dictionary<Rarity, ModType[]> BuildCandidateCache()
        {
            var cache = new Dictionary<Rarity, ModType[]>();
            var allTypes = Enum.GetValues<ModType>();
            foreach (Rarity rarity in Enum.GetValues<Rarity>())
            {
                var list = new List<ModType>();
                foreach (var type in allTypes)
                {
                    if (ModRarity.GetRarity(type) == rarity)
                        list.Add(type);
                }
                cache[rarity] = list.ToArray();
            }
            return cache;
        }

        public ModType Roll()
        {
            var rarity = RollRarity();
            var candidates = _candidatesByRarity[rarity];
            return candidates.Length > 0 ? candidates[_rng.Next(candidates.Length)] : ModType.Heavy;
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
    }

    public struct RarityWeight
    {
        public Rarity Rarity;
        public float Weight;
    }
}
