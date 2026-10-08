using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;

namespace ObstacleDodge
{
    /// <summary>
    /// Sound effects made in code from simple waves (no sound files needed),
    /// the "AudioSource.PlayOneShot" idea from the guide's ideas list.
    /// </summary>
    public sealed class Sfx
    {
        const int Rate = 22050;
        readonly Dictionary<SoundKind, SoundEffect> effects = new Dictionary<SoundKind, SoundEffect>();
        readonly Random random = new Random(3);
        public bool Enabled = true;

        public Sfx()
        {
            try
            {
                effects[SoundKind.Hit] = Make(0.28f, t =>
                    MathF.Sin(2f * MathF.PI * (150f - 140f * t) * t) * MathF.Exp(-t * 14f) +
                    Noise() * 0.45f * MathF.Exp(-t * 45f));
                effects[SoundKind.Land] = Make(0.3f, t => Noise() * MathF.Exp(-t * 16f) * 0.6f +
                    MathF.Sin(2f * MathF.PI * 70f * t) * MathF.Exp(-t * 10f) * 0.6f);
                effects[SoundKind.Launch] = Make(0.22f, t => MathF.Sin(2f * MathF.PI * (900f - 1500f * t) * t) * MathF.Exp(-t * 9f) * 0.5f);
                effects[SoundKind.Click] = Make(0.06f, t => Square(1200f, t) * MathF.Exp(-t * 60f) * 0.35f);
                effects[SoundKind.Finish] = Make(0.75f, t =>
                {
                    float[] notes = { 523.25f, 659.25f, 783.99f, 1046.5f };
                    int i = Math.Min(3, (int)(t / 0.12f));
                    float local = t - i * 0.12f;
                    float env = i < 3 ? MathF.Exp(-local * 10f) : MathF.Exp(-local * 3.5f);
                    return (MathF.Sin(2f * MathF.PI * notes[i] * t) * 0.6f + Square(notes[i], t) * 0.15f) * env;
                });
                effects[SoundKind.Lose] = Make(0.8f, t =>
                {
                    float f = t < 0.25f ? 392f : t < 0.5f ? 330f : 262f;
                    return (MathF.Sin(2f * MathF.PI * f * t) * 0.6f + Square(f, t) * 0.12f) * MathF.Exp(-(t % 0.25f) * 5f) * (1f - t / 0.8f);
                });
            }
            catch (Exception e)
            {
                // no sound card (for example in a test machine): the game simply stays silent
                Console.WriteLine("[Sound] disabled: " + e.Message);
                effects.Clear();
            }
        }

        public void Play(SoundKind kind)
        {
            if (!Enabled || !effects.TryGetValue(kind, out var effect)) return;
            try { effect.Play(0.8f, 0f, 0f); } catch { }
        }

        float Noise() => (float)random.NextDouble() * 2f - 1f;

        static float Square(float freq, float t) => MathF.Sin(2f * MathF.PI * freq * t) >= 0 ? 1f : -1f;

        static SoundEffect Make(float seconds, Func<float, float> wave)
        {
            int samples = (int)(seconds * Rate);
            var data = new byte[samples * 2];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                float fade = MathF.Min(1f, (samples - i) / (Rate * 0.01f)); // no click at the end
                short v = (short)(Math.Clamp(wave(t) * fade, -1f, 1f) * short.MaxValue * 0.8f);
                data[i * 2] = (byte)(v & 0xff);
                data[i * 2 + 1] = (byte)((v >> 8) & 0xff);
            }
            return new SoundEffect(data, Rate, AudioChannels.Mono);
        }
    }
}
