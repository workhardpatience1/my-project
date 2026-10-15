using System;
using System.Collections.Generic;
using UnityEngine;

namespace ObstacleDodge
{
    /// <summary>The phone version's sound effects, made from the same simple waves (no sound files).</summary>
    public sealed class Sfx
    {
        const int Rate = 22050;
        readonly AudioSource source;
        readonly Dictionary<SoundKind, AudioClip> clips = new Dictionary<SoundKind, AudioClip>();
        readonly System.Random random = new System.Random(3);
        public bool Enabled = true;

        public Sfx(GameObject host)
        {
            source = host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            try
            {
                clips[SoundKind.Hit] = Make("hit", 0.28f, t =>
                    Mathf.Sin(2f * Mathf.PI * (150f - 140f * t) * t) * Mathf.Exp(-t * 14f) + Noise() * 0.45f * Mathf.Exp(-t * 45f));
                clips[SoundKind.Land] = Make("land", 0.3f, t => Noise() * Mathf.Exp(-t * 16f) * 0.6f +
                    Mathf.Sin(2f * Mathf.PI * 70f * t) * Mathf.Exp(-t * 10f) * 0.6f);
                clips[SoundKind.Launch] = Make("launch", 0.22f, t => Mathf.Sin(2f * Mathf.PI * (900f - 1500f * t) * t) * Mathf.Exp(-t * 9f) * 0.5f);
                clips[SoundKind.Click] = Make("click", 0.06f, t => Square(1200f, t) * Mathf.Exp(-t * 60f) * 0.35f);
                clips[SoundKind.Finish] = Make("finish", 0.75f, t =>
                {
                    float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                    int i = Math.Min(3, (int)(t / 0.12f));
                    float local = t - i * 0.12f;
                    float env = i < 3 ? Mathf.Exp(-local * 10f) : Mathf.Exp(-local * 3.5f);
                    return (Mathf.Sin(2f * Mathf.PI * notes[i] * t) * 0.6f + Square(notes[i], t) * 0.15f) * env;
                });
                clips[SoundKind.Lose] = Make("lose", 0.8f, t =>
                {
                    float f = t < 0.25f ? 392f : t < 0.5f ? 330f : 262f;
                    return (Mathf.Sin(2f * Mathf.PI * f * t) * 0.6f + Square(f, t) * 0.12f) * Mathf.Exp(-(t % 0.25f) * 5f) * (1f - t / 0.8f);
                });
            }
            catch (Exception e)
            {
                Debug.Log("[Sound] disabled: " + e.Message);
                clips.Clear();
            }
        }

        public void Play(SoundKind kind)
        {
            if (!Enabled || !clips.TryGetValue(kind, out var clip)) return;
            source.PlayOneShot(clip, 0.8f);
        }

        float Noise() => (float)random.NextDouble() * 2f - 1f;

        static float Square(float freq, float t) => Mathf.Sin(2f * Mathf.PI * freq * t) >= 0 ? 1f : -1f;

        static AudioClip Make(string name, float seconds, Func<float, float> wave)
        {
            int samples = (int)(seconds * Rate);
            var data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                float fade = Mathf.Min(1f, (samples - i) / (Rate * 0.01f));
                data[i] = Mathf.Clamp(wave(t) * fade, -1f, 1f) * 0.8f;
            }
            var clip = AudioClip.Create(name, samples, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
