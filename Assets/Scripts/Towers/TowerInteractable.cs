using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Towers
{
    [RequireComponent(typeof(TowerChassis))]
    public class TowerInteractable : MonoBehaviour, IInteractable
    {
        private TowerChassis _chassis;

        public TowerChassis Chassis => _chassis;

        private void Awake()
        {
            _chassis = GetComponent<TowerChassis>();
        }

        public string GetPromptText()
        {
            return "Click - Edit Tower";
        }

        public bool CanInteract()
        {
            return true;
        }

        public void Interact()
        {
        }
    }
}
