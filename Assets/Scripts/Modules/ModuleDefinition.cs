using System;
using System.Collections.Generic;
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

        [Serializable]
        public class ParamDef
        {
            public string name;
            public float defaultValue;
            public float min;
            public float max;
        }

        public List<ParamDef> editableParams = new();
    }
}
