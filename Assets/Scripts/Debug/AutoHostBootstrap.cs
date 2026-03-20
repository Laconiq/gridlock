using System.Collections;
using AIWE.Core;
using Unity.Netcode;
using UnityEngine;

namespace AIWE.Testing
{
    public class AutoHostBootstrap : MonoBehaviour
    {
        [SerializeField] private float startDelay = 0.5f;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(startDelay);

            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                UnityEngine.Debug.LogError("[AutoHost] No NetworkManager found");
                yield break;
            }

            nm.StartHost();
            UnityEngine.Debug.Log("[AutoHost] Host started (local, no Relay)");

            yield return new WaitUntil(() => GameManager.Instance != null && GameManager.Instance.IsSpawned);

            GameManager.Instance.SetState(GameState.Preparing);
            UnityEngine.Debug.Log("[AutoHost] State set to Preparing — player controls active");
        }
    }
}
