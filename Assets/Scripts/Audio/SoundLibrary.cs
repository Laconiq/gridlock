using System.Collections.Generic;
using UnityEngine;

namespace Gridlock.Audio
{
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Gridlock/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> entries = new();

        private Dictionary<SoundType, SoundEntry> _lookup;

        public SoundEntry Get(SoundType type)
        {
            BuildLookup();
            _lookup.TryGetValue(type, out var entry);
            return entry;
        }

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<SoundType, SoundEntry>();
            foreach (var entry in entries)
                _lookup.TryAdd(entry.type, entry);
        }

        private void OnEnable() => _lookup = null;
    }
}
