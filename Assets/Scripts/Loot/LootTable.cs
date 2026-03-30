using System;
using System.Collections.Generic;
using Gridlock.Mods;
using UnityEngine;

namespace Gridlock.Loot
{
    [CreateAssetMenu(menuName = "Gridlock/Loot Table")]
    public class LootTable : ScriptableObject
    {
        [Serializable]
        public struct RarityWeight
        {
            public Rarity rarity;
            public float weight;
        }

        [SerializeField] private float dropChance = 0.5f;
        [SerializeField] private List<RarityWeight> rarityWeights = new()
        {
            new() { rarity = Rarity.Common, weight = 50f },
            new() { rarity = Rarity.Uncommon, weight = 30f },
            new() { rarity = Rarity.Rare, weight = 15f },
            new() { rarity = Rarity.Epic, weight = 5f }
        };

        public float DropChance => dropChance;

        public ModType Roll()
        {
            var rarity = RollRarity();
            return PickModOfRarity(rarity);
        }

        private Rarity RollRarity()
        {
            float totalWeight = 0f;
            foreach (var rw in rarityWeights) totalWeight += rw.weight;

            float roll = UnityEngine.Random.Range(0f, totalWeight);
            float cumulative = 0f;
            foreach (var rw in rarityWeights)
            {
                cumulative += rw.weight;
                if (roll <= cumulative) return rw.rarity;
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
            return candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }
    }
}
