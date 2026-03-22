using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.AI
{
    [RequireComponent(typeof(NavMeshSurface))]
    public class RuntimeNavMeshBaker : MonoBehaviour
    {
        [SerializeField] private bool bakeOnAwake = true;

        private NavMeshSurface _surface;

        private void Awake()
        {
            _surface = GetComponent<NavMeshSurface>();
        }

        private void Start()
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && !nm.IsServer) return;

            if (bakeOnAwake)
                Bake();
        }

        public void Bake()
        {
            if (_surface == null) return;
            _surface.BuildNavMesh();
        }
    }
}
