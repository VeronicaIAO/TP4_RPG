using UnityEngine;

namespace TPRPG
{
    [RequireComponent(typeof(AudioSource))]
    public class UnitVoice : MonoBehaviour
    {
        public CharacterVoice voice;

        AudioSource source;
        AudioClip lastClip;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
        }

        public void PlayAttack(ActionData action)
        {
            if (voice == null || action == null) return;
            if (action.actionAnimation == ActionAnimation.Guard && CharacterVoice.HasAny(voice.other.guard)) { Play(voice.other.guard); return; }
            if (action is ItemData && CharacterVoice.HasAny(voice.other.itemUse)) { Play(voice.other.itemUse); return; }
            Play(voice.ForAction(action));
        }

        public void PlayReaction(HitReaction reaction)
        {
            if (voice == null) return;
            Play(voice.ForReaction(reaction));
        }

        public void PlayTurnReady() { if (voice != null) Play(voice.other.turnReady); }
        public void PlayKnockedOut() { if (voice != null) Play(voice.other.knockedOut); }
        public void PlayVictory() { if (voice != null) Play(voice.other.victory); }

        public void Play(AudioClip[] options)
        {
            if (source == null || !CharacterVoice.HasAny(options)) return;
            AudioClip pick = null;
            for (int tries = 0; tries < 6; tries++)
            {
                var c = options[Random.Range(0, options.Length)];
                if (c == null) continue;
                pick = c;
                if (c != lastClip) break;
            }
            if (pick == null) return;
            lastClip = pick;
            source.Stop();
            float variation = voice != null ? voice.pitchVariation : 0f;
            source.pitch = 1f + Random.Range(-variation, variation);
            source.clip = pick;
            source.volume = voice != null ? voice.volume : 1f;
            source.Play();
        }
    }
}
