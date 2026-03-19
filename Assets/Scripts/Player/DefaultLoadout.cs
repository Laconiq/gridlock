using System;
using System.Collections.Generic;
using AIWE.Modules;
using UnityEngine;

namespace AIWE.Player
{
    [CreateAssetMenu(menuName = "AIWE/Default Loadout")]
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
