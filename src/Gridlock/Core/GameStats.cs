namespace Gridlock.Core
{
    public sealed class GameStats
    {
        public static GameStats? Instance { get; private set; }

        public int TotalKills { get; private set; }
        public int WaveReached { get; private set; }

        public void Init()
        {
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

        public void Shutdown()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
