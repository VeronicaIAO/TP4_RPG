using System.Collections.Generic;
using UnityEngine;

namespace TPRPG
{
    [System.Serializable]
    public class ActionVoiceLine
    {
        public string label = "Attack";
        public ActionData action;
        public AudioClip[] clips = new AudioClip[0];
    }

    [System.Serializable]
    public class ReactionVoiceLines
    {
        public AudioClip[] hurt = new AudioClip[0];
        public AudioClip[] pain = new AudioClip[0];
        public AudioClip[] surprise = new AudioClip[0];
        public AudioClip[] outrage = new AudioClip[0];
        public AudioClip[] disgust = new AudioClip[0];
    }

    [System.Serializable]
    public class OtherVoiceLines
    {
        public AudioClip[] guard = new AudioClip[0];
        public AudioClip[] itemUse = new AudioClip[0];
        public AudioClip[] turnReady = new AudioClip[0];
        public AudioClip[] knockedOut = new AudioClip[0];
        public AudioClip[] victory = new AudioClip[0];
    }

    [CreateAssetMenu(menuName = "TP RPG/Character Voice", fileName = "NewVoice")]
    public class CharacterVoice : ScriptableObject
    {
        [Range(0f, 1f)] public float volume = 0.9f;
        [Range(0f, 0.3f)] public float pitchVariation = 0.06f;

        public List<ActionVoiceLine> attackGrunts = new List<ActionVoiceLine>();
        public AudioClip[] fallbackAttackGrunts = new AudioClip[0];
        public ReactionVoiceLines reactions = new ReactionVoiceLines();
        public OtherVoiceLines other = new OtherVoiceLines();

        public AudioClip[] ForAction(ActionData action)
        {
            if (action != null)
            {
                foreach (var line in attackGrunts)
                    if (line != null && line.action == action && HasAny(line.clips)) return line.clips;
            }
            return fallbackAttackGrunts;
        }

        public AudioClip[] ForReaction(HitReaction reaction)
        {
            switch (reaction)
            {
                case HitReaction.Hurt: return reactions.hurt;
                case HitReaction.Pain: return HasAny(reactions.pain) ? reactions.pain : reactions.hurt;
                case HitReaction.Surprise: return reactions.surprise;
                case HitReaction.Outrage: return reactions.outrage;
                case HitReaction.Disgust: return reactions.disgust;
                default: return null;
            }
        }

        public static bool HasAny(AudioClip[] clips)
        {
            if (clips == null) return false;
            foreach (var c in clips) if (c != null) return true;
            return false;
        }
    }
}
