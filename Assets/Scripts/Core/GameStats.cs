using UnityEngine;

namespace Gridlock.Core
{
    public class GameStats : MonoBehaviour
    {
        public static GameStats Instance { get; private set; }

        public int TotalKills { get; private set; }
        public int WaveReached { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddKill()
        {
            TotalKills++;
        }

        public void SetWave(int wave)
        {
            if (wave > WaveReached)
                WaveReached = wave;
        }

        public void Reset()
        {
            TotalKills = 0;
            WaveReached = 0;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
