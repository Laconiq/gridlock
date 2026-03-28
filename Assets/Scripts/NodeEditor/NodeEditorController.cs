using Gridlock.Interfaces;
using Gridlock.NodeEditor.UI;
using Gridlock.Player;
using Gridlock.Towers;
using UnityEngine;

namespace Gridlock.NodeEditor
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
