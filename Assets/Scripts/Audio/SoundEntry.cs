using UnityEngine;

namespace Gridlock.Audio
{
    [System.Serializable]
    public class SoundEntry
    {
        public SoundType type;
        public AudioClip[] clips;

        [Range(0f, 1f)] public float volume = 0.8f;
        [Range(0.1f, 3f)] public float pitchMin = 0.95f;
        [Range(0.1f, 3f)] public float pitchMax = 1.05f;

        [Tooltip("Max concurrent instances of this sound type. 0 = unlimited.")]
        [Range(0, 10)] public int maxInstances = 3;

        [Tooltip("Minimum time (seconds) between plays of this sound type.")]
        [Range(0f, 1f)] public float cooldown = 0.05f;

        [Tooltip("AudioSource priority (0 = highest, 256 = lowest).")]
        [Range(0, 256)] public int priority = 128;

        [Tooltip("0 = 2D, 1 = full 3D spatialization.")]
        [Range(0f, 1f)] public float spatialBlend;

        public AudioClip GetRandomClip()
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }
    }
}
