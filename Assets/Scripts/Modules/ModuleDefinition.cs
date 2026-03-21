using AIWE.NodeEditor.Data;
using UnityEngine;

namespace AIWE.Modules
{
    public abstract class ModuleDefinition : ScriptableObject
    {
        public string moduleId;
        public string displayName;
        public Sprite icon;
        public ModuleCategory category;
        public Color nodeColor = Color.gray;
        [TextArea] public string description;

        public virtual float GetCooldown() => 0f;
    }
}
