using System.Collections.Generic;
using UnityEngine;

namespace AIWE.Modules
{
    [CreateAssetMenu(menuName = "AIWE/Module Registry")]
    public class ModuleRegistry : ScriptableObject
    {
        [SerializeField] private List<ModuleDefinition> allModules = new();

        private Dictionary<string, ModuleDefinition> _lookup;

        public IReadOnlyList<ModuleDefinition> AllModules => allModules;

        public ModuleDefinition GetById(string moduleId)
        {
            if (_lookup == null) BuildLookup();
            return _lookup.TryGetValue(moduleId, out var def) ? def : null;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, ModuleDefinition>();
            foreach (var mod in allModules)
            {
                if (mod != null && !string.IsNullOrEmpty(mod.moduleId))
                {
                    _lookup[mod.moduleId] = mod;
                }
            }
        }

        private void OnEnable()
        {
            _lookup = null;
        }
    }
}
