using UnityEngine;

namespace AIWE.Player.CameraEffects
{
    [AddComponentMenu("AIWE/Audio/Placeholder Audio Generator")]
    public class PlaceholderAudioGenerator : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private void Start()
        {
            var movementAudio = GetComponent<MovementAudio>();
            if (movementAudio == null) return;

            AssignPlaceholderAudio(movementAudio);
        }

        private void AssignPlaceholderAudio(MovementAudio audio)
        {
            var footstepField = typeof(MovementAudio).GetField("footstepClips",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var windSourceField = typeof(MovementAudio).GetField("windSource",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var landingClipField = typeof(MovementAudio).GetField("landingClip",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (footstepField != null)
            {
                var clips = footstepField.GetValue(audio) as AudioClip[];
                if (clips == null || clips.Length == 0)
                {
                    footstepField.SetValue(audio, new[]
                    {
                        GenerateClick(0.08f, 220f, "Footstep1"),
                        GenerateClick(0.08f, 200f, "Footstep2"),
                        GenerateClick(0.08f, 240f, "Footstep3"),
                        GenerateClick(0.08f, 210f, "Footstep4")
                    });
                }
            }

            if (windSourceField != null)
            {
                var windSource = windSourceField.GetValue(audio) as AudioSource;
                if (windSource != null && windSource.clip == null)
                    windSource.clip = GenerateNoise(2f, "Wind");
            }

            if (landingClipField != null)
            {
                var clip = landingClipField.GetValue(audio) as AudioClip;
                if (clip == null)
                    landingClipField.SetValue(audio, GenerateThud(0.2f, 80f, "Landing"));
            }

            var hitFeedback = FindAnyObjectByType<HitFeedback>();
            if (hitFeedback != null)
            {
                var hitSoundField = typeof(HitFeedback).GetField("hitSound",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var killSoundField = typeof(HitFeedback).GetField("killSound",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (hitSoundField != null && hitSoundField.GetValue(hitFeedback) == null)
                    hitSoundField.SetValue(hitFeedback, GenerateClick(0.05f, 1200f, "HitTick"));
                if (killSoundField != null && killSoundField.GetValue(hitFeedback) == null)
                    killSoundField.SetValue(hitFeedback, GenerateSweep(0.15f, 800f, 1600f, "KillConfirm"));
            }
        }

        private static AudioClip GenerateClick(float duration, float freq, string name)
        {
            int samples = Mathf.CeilToInt(duration * SampleRate);
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = 1f - (t / duration);
                envelope *= envelope;
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.5f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateThud(float duration, float freq, string name)
        {
            int samples = Mathf.CeilToInt(duration * SampleRate);
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = Mathf.Exp(-t * 15f);
                float currentFreq = freq * (1f - t / duration * 0.5f);
                data[i] = Mathf.Sin(2f * Mathf.PI * currentFreq * t) * envelope * 0.7f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateNoise(float duration, string name)
        {
            int samples = Mathf.CeilToInt(duration * SampleRate);
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            var data = new float[samples];
            float prev = 0f;
            for (int i = 0; i < samples; i++)
            {
                float noise = Random.Range(-1f, 1f);
                prev = prev * 0.95f + noise * 0.05f;
                data[i] = prev * 0.3f;
            }
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip GenerateSweep(float duration, float startFreq, float endFreq, string name)
        {
            int samples = Mathf.CeilToInt(duration * SampleRate);
            var clip = AudioClip.Create(name, samples, 1, SampleRate, false);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / SampleRate;
                float envelope = 1f - (t / duration);
                float freq = Mathf.Lerp(startFreq, endFreq, t / duration);
                data[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
            }
            clip.SetData(data, 0);
            return clip;
        }
    }
}
