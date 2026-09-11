using System.Collections.Generic;
using UnityEngine;

namespace ChaosCrew
{
    public enum Sfx
    {
        Tap,
        Confirm,
        TaskDone,
        Error,
        Slip,
        Sabotage,
        Alarm,
        Whoosh,
        Vote,
        Win,
        Lose
    }

    /// <summary>
    /// Procedural audio so the prototype needs no .wav assets. Each clip is synthesised once
    /// and cached; replace <see cref="Build"/> with authored clips later without touching callers.
    /// </summary>
    public sealed class SfxSynth : MonoBehaviour
    {
        private const int SampleRate = 44100;

        private readonly Dictionary<Sfx, AudioClip> _clips = new Dictionary<Sfx, AudioClip>();
        private AudioSource _source;

        public bool Muted { get; set; }
        public float Volume { get; set; } = 0.7f;

        private void Awake()
        {
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f;
        }

        public void Play(Sfx sfx, float pitch = 1f)
        {
            if (Muted || _source == null) return;
            if (!_clips.TryGetValue(sfx, out var clip))
            {
                clip = Build(sfx);
                _clips[sfx] = clip;
            }
            _source.pitch = Mathf.Clamp(pitch, 0.4f, 2.5f);
            _source.PlayOneShot(clip, Volume);
        }

        private static AudioClip Build(Sfx sfx)
        {
            switch (sfx)
            {
                case Sfx.Tap:      return Tone("sfx_tap", 0.06f, t => Square(t, 880f) * Decay(t, 0.06f) * 0.25f);
                case Sfx.Confirm:  return Tone("sfx_confirm", 0.18f, t => Sine(t, Mathf.Lerp(520f, 900f, t / 0.18f)) * Decay(t, 0.18f) * 0.35f);
                case Sfx.TaskDone: return Arpeggio("sfx_taskdone", new[] { 523f, 659f, 784f, 1046f }, 0.075f);
                case Sfx.Error:    return Tone("sfx_error", 0.22f, t => Square(t, Mathf.Lerp(300f, 140f, t / 0.22f)) * Decay(t, 0.22f) * 0.3f);
                case Sfx.Slip:     return Tone("sfx_slip", 0.35f, t => Sine(t, Mathf.Lerp(900f, 180f, Mathf.Sqrt(t / 0.35f))) * Decay(t, 0.35f) * 0.35f);
                case Sfx.Sabotage: return Tone("sfx_sabotage", 0.4f, t => (Saw(t, 110f) * 0.6f + Noise(t) * 0.4f) * Decay(t, 0.4f) * 0.32f);
                case Sfx.Alarm:    return Tone("sfx_alarm", 0.7f, t => Square(t, 440f + 220f * Mathf.Sin(t * 26f)) * Decay(t, 0.7f) * 0.25f);
                case Sfx.Whoosh:   return Tone("sfx_whoosh", 0.3f, t => Noise(t) * Mathf.Sin(Mathf.PI * Mathf.Clamp01(t / 0.3f)) * 0.22f);
                case Sfx.Vote:     return Tone("sfx_vote", 0.14f, t => Sine(t, 660f) * Decay(t, 0.14f) * 0.3f);
                case Sfx.Win:      return Arpeggio("sfx_win", new[] { 523f, 659f, 784f, 1046f, 1318f }, 0.11f);
                case Sfx.Lose:     return Arpeggio("sfx_lose", new[] { 440f, 370f, 294f, 220f }, 0.14f);
                default:           return Tone("sfx_default", 0.1f, t => Sine(t, 440f) * Decay(t, 0.1f) * 0.3f);
            }
        }

        private static AudioClip Tone(string name, float seconds, System.Func<float, float> fn)
        {
            int count = Mathf.Max(1, Mathf.CeilToInt(seconds * SampleRate));
            var data = new float[count];
            for (int i = 0; i < count; i++) data[i] = Mathf.Clamp(fn(i / (float)SampleRate), -1f, 1f);
            var clip = AudioClip.Create(name, count, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip Arpeggio(string name, float[] notes, float noteSeconds)
        {
            int perNote = Mathf.CeilToInt(noteSeconds * SampleRate);
            var data = new float[perNote * notes.Length];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote; i++)
                {
                    float t = i / (float)SampleRate;
                    data[n * perNote + i] = Mathf.Clamp(Sine(t, notes[n]) * Decay(t, noteSeconds) * 0.32f, -1f, 1f);
                }
            }
            var clip = AudioClip.Create(name, data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static float Sine(float t, float hz) => Mathf.Sin(2f * Mathf.PI * hz * t);
        private static float Square(float t, float hz) => Mathf.Sign(Sine(t, hz)) * 0.7f;
        private static float Saw(float t, float hz) => (t * hz % 1f) * 2f - 1f;
        private static float Noise(float t) => Mathf.PerlinNoise(t * 4000f, 0.5f) * 2f - 1f;
        private static float Decay(float t, float len) => Mathf.Exp(-4f * t / Mathf.Max(0.001f, len));
    }
}
