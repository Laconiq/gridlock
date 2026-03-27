using System.Collections;
using AIWE.Core;
using UnityEngine;

namespace AIWE.LevelDesign
{
    public class LDtkLevelLinker : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject towerPrefab;

        private bool _built;

        private void Start()
        {
            StartCoroutine(WaitAndBuild());
        }

        private IEnumerator WaitAndBuild()
        {
            while (GameManager.Instance == null)
                yield return null;

            GameManager.Instance.OnStateChanged += OnStateChanged;
            CheckState(GameManager.Instance.CurrentState);
        }

        private void OnStateChanged(GameState prev, GameState current)
        {
            CheckState(current);
        }

        private void CheckState(GameState state)
        {
            if (_built) return;
            if (state == GameState.Preparing || state == GameState.Wave)
            {
                BuildLevel();
                _built = true;
            }
        }

        public void ResetBuiltState()
        {
            _built = false;
        }

        private void BuildLevel()
        {
            SpawnTowers();
            Debug.Log("[LDtkLevelLinker] Level built from LDtk markers");
        }

        private void SpawnTowers()
        {
            if (towerPrefab == null)
            {
                Debug.LogWarning("[LDtkLevelLinker] No tower prefab assigned");
                return;
            }

            var markers = FindObjectsByType<TowerSlotMarker>(FindObjectsSortMode.None);
            foreach (var marker in markers)
            {
                var pos = marker.transform.position;
                var tower = Instantiate(towerPrefab, pos, Quaternion.identity);
                tower.name = $"Tower_{marker.TowerId}";

                Debug.Log($"[LDtkLevelLinker] Spawned tower '{marker.TowerId}' at {pos}");
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= OnStateChanged;
        }
    }
}
