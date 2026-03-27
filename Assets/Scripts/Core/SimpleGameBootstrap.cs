using AIWE.Player;
using UnityEngine;

namespace AIWE.Core
{
    public class SimpleGameBootstrap : MonoBehaviour
    {
        [SerializeField] private GameObject playerPrefab;

        private void Start()
        {
            if (playerPrefab != null)
            {
                var player = Instantiate(playerPrefab);
                player.name = "Player";
                Debug.Log("[SimpleGameBootstrap] Player instantiated");
            }

            StartCoroutine(WaitAndStart());
        }

        private System.Collections.IEnumerator WaitAndStart()
        {
            yield return null;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Preparing);
                Debug.Log("[SimpleGameBootstrap] Game state set to Preparing");
            }
        }
    }
}
