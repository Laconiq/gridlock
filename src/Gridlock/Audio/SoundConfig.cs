namespace Gridlock.Audio
{
    public sealed class SoundConfig
    {
        public string[] FilePaths { get; set; } = System.Array.Empty<string>();
        public float Volume { get; set; } = 1f;
        public float PitchVariance { get; set; } = 0.1f;
        public float Cooldown { get; set; } = 0.05f;
        public int MaxInstances { get; set; } = 3;
    }
}
