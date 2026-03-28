using Gridlock.Player;
using UnityEngine;

namespace Gridlock.Core
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
            }

            StartCoroutine(WaitAndStart());
        }

        private System.Collections.IEnumerator WaitAndStart()
        {
            yield return null;

            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Preparing);
        }
    }
}
