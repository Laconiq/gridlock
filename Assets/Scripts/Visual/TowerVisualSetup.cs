using AmazingAssets.WireframeShader;
using Gridlock.Enemies;
using Gridlock.Interfaces;
using Gridlock.Mods;
using Gridlock.Mods.Pipeline;
using Gridlock.Towers;
using UnityEngine;

namespace Gridlock.Visual
{
    public class TowerVisualSetup : MonoBehaviour
    {
        private static Material _wireframeMat;
        private static Mesh _diamondWireMesh;

        private Transform _turret;
        private TowerChassis _chassis;
        private ModSlotExecutor _executor;

        [SerializeField] private float turretHeight = 1.2f;
        [SerializeField] private float turretSize = 0.35f;
        [SerializeField] private float turretRotSpeed = 8f;
        [SerializeField] private float bobAmplitude = 0.06f;
        [SerializeField] private float bobSpeed = 2f;
        [SerializeField] private float idleSpinSpeed = 30f;

        private float _bobPhase;

        public static void Apply(GameObject tower)
        {
            if (tower.GetComponent<TowerVisualSetup>() != null) return;
            tower.AddComponent<TowerVisualSetup>();
        }

        private void Start()
        {
            _chassis = GetComponent<TowerChassis>();
            _executor = GetComponent<ModSlotExecutor>();

            EnsureResources();
            BuildVisuals();

            _bobPhase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void BuildVisuals()
        {
            var baseMf = GetComponentInChildren<MeshFilter>();
            if (baseMf != null)
            {
                var wireCube = baseMf.sharedMesh.WireframeShader().GenerateWireframeMesh(true, true);
                if (wireCube != null)
                    baseMf.sharedMesh = wireCube;
            }

            var baseRenderer = GetComponentInChildren<MeshRenderer>();
            if (baseRenderer != null)
                baseRenderer.sharedMaterial = _wireframeMat;

            transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);

            _turret = new GameObject("Turret").transform;
            _turret.SetParent(transform, false);
            _turret.localPosition = new Vector3(0f, turretHeight / transform.localScale.y, 0f);
            float sx = turretSize / transform.localScale.x;
            float sy = turretSize * 1.8f / transform.localScale.y;
            _turret.localScale = new Vector3(sx, sy, sx);

            var mf = _turret.gameObject.AddComponent<MeshFilter>();
            mf.sharedMesh = _diamondWireMesh;

            var mr = _turret.gameObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _wireframeMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            var firePointGO = new GameObject("FirePoint");
            firePointGO.transform.SetParent(_turret, false);
            firePointGO.transform.localPosition = new Vector3(0f, 0f, 1f);

            if (_chassis != null)
            {
                var fpField = typeof(TowerChassis).GetField("firePoint",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fpField?.SetValue(_chassis, firePointGO.transform);
            }
        }

        private void Update()
        {
            if (_turret == null) return;

            float bob = Mathf.Sin(Time.time * bobSpeed + _bobPhase) * bobAmplitude;
            float baseY = turretHeight / transform.localScale.y;
            _turret.localPosition = new Vector3(0f, baseY + bob / transform.localScale.y, 0f);

            var target = FindCurrentTarget();
            if (target != null && target.IsAlive)
            {
                var dir = target.Position - _turret.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                {
                    var targetRot = Quaternion.LookRotation(dir) * Quaternion.Euler(0f, 0f, 45f);
                    _turret.rotation = Quaternion.Slerp(_turret.rotation, targetRot, turretRotSpeed * Time.deltaTime);
                }
            }
            else
            {
                _turret.Rotate(Vector3.up, idleSpinSpeed * Time.deltaTime, Space.World);
            }
        }

        private ITargetable FindCurrentTarget()
        {
            if (_executor == null || _chassis == null) return null;

            float rangeSq = _chassis.BaseRange * _chassis.BaseRange;
            float bestDist = float.MaxValue;
            ITargetable best = null;

            var entries = EnemyRegistry.All;
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.Controller == null || !e.Controller.IsAlive) continue;
                float d = (e.Controller.Position - transform.position).sqrMagnitude;
                if (d < rangeSq && d < bestDist)
                {
                    bestDist = d;
                    best = e.Controller;
                }
            }
            return best;
        }

        private static void EnsureResources()
        {
            if (_wireframeMat == null)
                _wireframeMat = Resources.Load<Material>("WireframeTower");

            if (_diamondWireMesh == null)
            {
                var rawMesh = CreateOctahedronMesh();
                _diamondWireMesh = rawMesh.WireframeShader().GenerateWireframeMesh(true, false);
                if (_diamondWireMesh == null)
                    _diamondWireMesh = rawMesh;
            }
        }

        private static Mesh CreateOctahedronMesh()
        {
            var mesh = new Mesh { name = "Diamond" };

            float s = 0.5f;
            var verts = new[]
            {
                new Vector3(0, s, 0),
                new Vector3(s, 0, 0),
                new Vector3(0, 0, s),
                new Vector3(-s, 0, 0),
                new Vector3(0, 0, -s),
                new Vector3(0, -s, 0),
            };

            mesh.vertices = verts;
            mesh.triangles = new[]
            {
                0,1,2, 0,2,3, 0,3,4, 0,4,1,
                5,2,1, 5,3,2, 5,4,3, 5,1,4
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
