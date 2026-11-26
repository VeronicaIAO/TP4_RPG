using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace TPRPG
{
    public class CharacterAnimator : MonoBehaviour
    {
        public Transform modelRoot;
        public AnimationClip[] clips = new AnimationClip[0];
        public float defaultFade = 0.12f;

        readonly Dictionary<string, AnimationClip> map = new Dictionary<string, AnimationClip>();
        PlayableGraph graph;
        AnimationMixerPlayable mixer;
        AnimationClipPlayable current;
        AnimationClipPlayable previous;
        AnimationClip currentClip;
        AnimationClip previousClip;
        bool currentLoop;
        bool previousLoop;
        float fadeTime;
        float fadeDuration;
        Animation legacy;
        bool useLegacy;
        bool ready;

        public string CurrentClip { get; private set; }
        public bool HasClips => map.Count > 0;

        void Awake()
        {
            if (modelRoot == null) modelRoot = transform;
            foreach (var c in clips)
                if (c != null && !map.ContainsKey(c.name)) map[c.name] = c;
            if (map.Count == 0) return;

            foreach (var c in map.Values)
                if (c.legacy) { useLegacy = true; break; }

            if (useLegacy)
            {
                legacy = modelRoot.GetComponent<Animation>();
                if (legacy == null) legacy = modelRoot.gameObject.AddComponent<Animation>();
                legacy.playAutomatically = false;
                foreach (var kv in map)
                    if (kv.Value.legacy && legacy.GetClip(kv.Key) == null) legacy.AddClip(kv.Value, kv.Key);
            }
            else
            {
                var animator = modelRoot.GetComponent<Animator>();
                if (animator == null) animator = modelRoot.gameObject.AddComponent<Animator>();
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                graph = PlayableGraph.Create(name + "_Animation");
                graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
                var output = AnimationPlayableOutput.Create(graph, "Output", animator);
                mixer = AnimationMixerPlayable.Create(graph, 2);
                output.SetSourcePlayable(mixer);
                graph.Play();
            }
            ready = true;
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }

        public bool Has(string clipName) => clipName != null && map.ContainsKey(clipName);

        public float Length(string clipName) => clipName != null && map.TryGetValue(clipName, out var c) ? c.length : 0f;

        public IEnumerable<string> ClipNames => map.Keys;

        public string FindClip(string wanted)
        {
            if (string.IsNullOrEmpty(wanted) || map.Count == 0) return null;
            if (map.ContainsKey(wanted)) return wanted;
            foreach (var k in map.Keys)
                if (k.EndsWith(wanted, System.StringComparison.OrdinalIgnoreCase)) return k;
            foreach (var k in map.Keys)
                if (k.IndexOf(wanted, System.StringComparison.OrdinalIgnoreCase) >= 0) return k;
            return null;
        }

        public void Play(string clipName, bool loop, float fade = -1f, float speed = 1f)
        {
            if (!ready || clipName == null || !map.TryGetValue(clipName, out var clip)) return;
            if (fade < 0f) fade = defaultFade;
            CurrentClip = clipName;

            if (useLegacy)
            {
                var state = legacy[clipName];
                if (state == null) return;
                state.wrapMode = loop ? WrapMode.Loop : WrapMode.ClampForever;
                state.speed = speed;
                legacy.CrossFade(clipName, fade);
                state.time = 0f;
                return;
            }

            if (previous.IsValid())
            {
                mixer.DisconnectInput(1);
                previous.Destroy();
            }
            if (current.IsValid())
            {
                mixer.DisconnectInput(0);
                previous = current;
                previousClip = currentClip;
                previousLoop = currentLoop;
                mixer.ConnectInput(1, previous, 0);
            }

            current = AnimationClipPlayable.Create(graph, clip);
            current.SetApplyFootIK(false);
            current.SetSpeed(speed);
            mixer.ConnectInput(0, current, 0);
            currentClip = clip;
            currentLoop = loop;
            fadeDuration = fade;
            fadeTime = 0f;
            bool hasPrevious = previous.IsValid();
            mixer.SetInputWeight(0, fade > 0f && hasPrevious ? 0f : 1f);
            mixer.SetInputWeight(1, fade > 0f && hasPrevious ? 1f : 0f);
        }

        void Update()
        {
            if (!ready || useLegacy || !current.IsValid()) return;
            Wrap(current, currentClip, currentLoop);
            if (previous.IsValid()) Wrap(previous, previousClip, previousLoop);

            if (fadeTime < fadeDuration)
            {
                fadeTime += Time.deltaTime;
                float w = Mathf.Clamp01(fadeTime / fadeDuration);
                mixer.SetInputWeight(0, w);
                mixer.SetInputWeight(1, 1f - w);
            }
            else
            {
                mixer.SetInputWeight(0, 1f);
                mixer.SetInputWeight(1, 0f);
            }
        }

        static void Wrap(AnimationClipPlayable p, AnimationClip clip, bool loop)
        {
            if (clip == null) return;
            double length = clip.length;
            if (length <= 0.0) return;
            double t = p.GetTime();
            if (t <= length) return;
            p.SetTime(loop ? t % length : length);
        }
    }
}