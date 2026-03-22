using AIWE.Interfaces;
using UnityEngine;

namespace AIWE.Player
{
    public class PlayerTargetable : MonoBehaviour, ITargetable
    {
        private PlayerHealth _health;

        public Vector3 Position => transform.position;
        public bool IsAlive => _health != null && _health.IsAlive;
        public Transform Transform => transform;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
        }
    }
}
