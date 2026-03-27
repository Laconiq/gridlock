using AIWE.Interfaces;
using AIWE.NodeEditor.UI;
using AIWE.Player;
using AIWE.Towers;
using UnityEngine;

namespace AIWE.NodeEditor
{
    public class NodeEditorController : MonoBehaviour
    {
        [SerializeField] private NodeEditorScreen screen;

        public void OpenEditor(IChassis chassis, PlayerInventory inventory)
        {
            screen?.Open(chassis, inventory);
        }
    }
}
