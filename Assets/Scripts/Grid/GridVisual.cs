using AIWE.Core;
using UnityEngine;

namespace AIWE.Grid
{
    public class GridVisual : MonoBehaviour
    {
        [SerializeField] private Material gridMaterial;

        private void Start()
        {
            var gridManager = ServiceLocator.Get<GridManager>();
            if (gridManager == null || gridManager.Definition == null) return;

            var def = gridManager.Definition;
            float w = def.Width * def.CellSize;
            float h = def.Height * def.CellSize;

            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "GridPlane";
            quad.transform.SetParent(transform);
            quad.transform.position = new Vector3(0f, -0.01f, 0f);
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(w, h, 1f);

            Destroy(quad.GetComponent<Collider>());

            if (gridMaterial != null)
            {
                var renderer = quad.GetComponent<MeshRenderer>();
                renderer.material = gridMaterial;
                renderer.material.SetFloat("_GridSize", def.CellSize);
                renderer.material.SetVector("_GridDimensions", new Vector4(w, h, 0, 0));
            }
        }
    }
}
