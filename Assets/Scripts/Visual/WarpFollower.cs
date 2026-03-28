using Gridlock.Grid;
using UnityEngine;

namespace Gridlock.Visual
{
    /// <summary>
    /// Makes this object follow the grid warp surface.
    /// Add to any object that should bob with the grid deformation.
    /// </summary>
    [DefaultExecutionOrder(300)]
    public class WarpFollower : MonoBehaviour
    {
        [SerializeField] private float influence = 1f;

        private float _baseY;
        private bool _baseYSet;

        private void Start()
        {
            _baseY = transform.position.y;
            _baseYSet = true;
        }

        public void SetBaseY(float y)
        {
            _baseY = y;
            _baseYSet = true;
        }

        private void LateUpdate()
        {
            if (!_baseYSet) return;

            var warp = GridWarpManager.Instance;
            if (warp == null) return;

            var pos = transform.position;
            float offset = warp.GetWarpOffset(pos.x, pos.z);
            pos.y = _baseY + offset * influence;
            transform.position = pos;
        }
    }
}
