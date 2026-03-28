using System;
using System.Collections.Generic;
using Gridlock.Modules;
using UnityEngine;

namespace Gridlock.Player
{
    [CreateAssetMenu(menuName = "Gridlock/Default Loadout")]
    public class DefaultLoadout : ScriptableObject
    {
        [Serializable]
        public struct LoadoutEntry
        {
            public ModuleDefinition module;
            public int count;
        }

        public List<LoadoutEntry> entries = new();
    }
}
