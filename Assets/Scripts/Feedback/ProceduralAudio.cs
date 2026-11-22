using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    [RequireComponent(typeof(AudioSource))]
    public class ProceduralAudio : MonoBehaviour
    {
        [System.Serializable]
        public class CustomSound
        {
            public FeedbackSound type;
            public AudioClip clip;
        }

        [Range(0f, 1f)] public float volume = 0.35f;
        public bool useGeneratedSounds = true;
        public List<CustomSound> customSounds = new List<CustomSound>();

        const int SampleRate = 44100;
        AudioSource source;
        readonly Dictionary<FeedbackSound, AudioClip> clips = new Dictionary<FeedbackSound, AudioClip>();
        readonly System.Random rng = new System.Random(1234);

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            if (useGeneratedSounds) Generate();
        }

        public void Play(FeedbackSound s)
        {
            if (s == FeedbackSound.None || source == null) return;
            AudioClip c = null;
            foreach (var p in customSounds) if (p != null && p.type == s && p.clip != null) c = p.clip;
            if (c == null) clips.TryGetValue(s, out c);
            if (c != null) source.PlayOneShot(c, volume);
        }

        delegate float Wave(float t);

        AudioClip Create(string clipName, float duration, Wave f)
        {
            int n = Mathf.CeilToInt(SampleRate * duration);
            var samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = Mathf.Clamp(f(i / (float)SampleRate), -1f, 1f);
            var clip = AudioClip.Create(clipName, n, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static float Envelope(float t, float duration)
        {
            float attack = Mathf.Clamp01(t / 0.005f);
            float decay = Mathf.Clamp01(1f - t / duration);
            return attack * decay * decay;
        }

        static float Sine(float f, float t) => Mathf.Sin(2f * Mathf.PI * f * t);
        static float Square(float f, float t) => Sine(f, t) >= 0f ? 0.6f : -0.6f;

        static float Sweep(float f0, float f1, float t, float duration)
        {
            float ph = 2f * Mathf.PI * (f0 * t + (f1 - f0) * t * t / (2f * duration));
            return Mathf.Sin(ph);
        }

        float Noise() => (float)(rng.NextDouble() * 2.0 - 1.0);

        AudioClip Notes(string clipName, float[] freqs, float noteDuration, bool square)
        {
            float total = freqs.Length * noteDuration;
            return Create(clipName, total, t =>
            {
                int idx = Mathf.Min(freqs.Length - 1, (int)(t / noteDuration));
                float lt = t - idx * noteDuration;
                float w = square ? Square(freqs[idx], lt) * 0.5f : Sine(freqs[idx], lt);
                return w * Envelope(lt, noteDuration) * 0.6f;
            });
        }

        void Generate()
        {
            clips[FeedbackSound.Hit] = Create("Hit", 0.15f, t => (Noise() * 0.6f + Sine(110f, t) * 0.6f) * Envelope(t, 0.15f));
            clips[FeedbackSound.Critical] = Create("Critical", 0.3f, t => (Noise() * 0.7f + Sine(80f, t) * 0.7f + Sine(880f, t) * 0.2f) * Envelope(t, 0.3f));
            clips[FeedbackSound.Verbal] = Create("Verbal", 0.4f, t => Square(220f + Mathf.Sin(t * 40f) * 60f, t) * Envelope(t, 0.4f) * 0.25f);
            clips[FeedbackSound.Shutter] = Create("Shutter", 0.12f, t => Noise() * (t < 0.03f || (t > 0.06f && t < 0.09f) ? 0.8f : 0f) * Envelope(t, 0.12f));
            clips[FeedbackSound.Splat] = Create("Splat", 0.22f, t => (Noise() * 0.5f + Sweep(500f, 120f, t, 0.22f) * 0.4f) * Envelope(t, 0.22f));
            clips[FeedbackSound.Whoosh] = Create("Whoosh", 0.25f, t => Noise() * Mathf.Sin(Mathf.PI * t / 0.25f) * 0.35f);
            clips[FeedbackSound.Heal] = Notes("Heal", new[] { 523.25f, 659.25f, 783.99f }, 0.1f, false);
            clips[FeedbackSound.Buff] = Create("Buff", 0.3f, t => Square(Mathf.Lerp(300f, 700f, t / 0.3f), t) * Envelope(t, 0.3f) * 0.3f);
            clips[FeedbackSound.Debuff] = Create("Debuff", 0.35f, t => Square(Mathf.Lerp(500f, 180f, t / 0.35f), t) * Envelope(t, 0.35f) * 0.3f);
            clips[FeedbackSound.KO] = Create("KO", 0.6f, t => Sweep(320f, 60f, t, 0.6f) * Envelope(t, 0.6f) * 0.6f);
            clips[FeedbackSound.Victory] = Notes("Victory", new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1046.5f }, 0.14f, true);
            clips[FeedbackSound.Defeat] = Notes("Defeat", new[] { 392f, 349.23f, 311.13f, 261.63f }, 0.25f, false);
            clips[FeedbackSound.Select] = Create("Select", 0.05f, t => Sine(900f, t) * Envelope(t, 0.05f) * 0.4f);
            clips[FeedbackSound.Turn] = Notes("Turn", new[] { 880f, 1318.5f }, 0.06f, false);
        }
    }
}
